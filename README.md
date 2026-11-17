# Đề tài 04: Hệ thống đặt món và xử lý tại bếp

> **Học phần:** Phát triển ứng dụng phân tán  
> **Sinh viên thực hiện:** Lương Việt Anh  
> **Mã sinh viên:** DTC245200063  
> **Email:** dtc245200063@ictu.edu.vn  
> **Nền tảng:** .NET 10 (C#) - ASP.NET Core Minimal API & SignalR  

---

## 1. Tổng quan đề tài

Xây dựng hệ thống phân tán phục vụ việc đặt món nhà hàng và đồng bộ quy trình chế biến tại bếp theo thời gian thực:
- Khách hàng xem thực đơn, gửi yêu cầu đặt món và theo dõi trạng thái đơn hàng.
- Đơn hàng được đưa vào hàng đợi thông điệp (RabbitMQ) để chuyển tiếp bất đồng bộ tới bộ phận bếp.
- Bộ phận bếp (**Kitchen Worker**) tiêu thụ (consume) đơn hàng từ hàng đợi, cập nhật tiến độ chế biến (`Pending` -> `Preparing` -> `Ready` / `Cancelled`).
- Dịch vụ thông báo thời gian thực (**Notification Service** sử dụng SignalR Hub) bắn cập nhật ngay lập tức về phía giao diện khách hàng.

### Kiến trúc tổng thể hệ thống
```text
Client -> Order API -> RabbitMQ -> Kitchen Worker -> RabbitMQ -> Notification Service -> Client
              |                         |
              +------> Database <-------+
```

---

## 2. Trạng thái hiện tại: Hoàn thành Giai đoạn 2 (Tuần 3 - 4)

Dự án đã hoàn thành **Giai đoạn 1** (RESTful Minimal API) và **Giai đoạn 2** (Giao tiếp thời gian thực với SignalR Hub theo Chương 3 Giáo trình & Bài thực hành 3.2).

### Các thành phần đã triển khai:

1. **`src/Order.Api` (ASP.NET Core Minimal API & SignalR Hub):**
   - Chạy trên .NET 10 (`net10.0`).
   - **Tuyến RESTful API:**
     - `GET /`: Kiểm tra tình trạng chạy của service và trạng thái kênh SignalR.
     - `GET /menu`: Xem thực đơn nhà hàng (món ăn, giá, khả dụng).
     - `POST /orders`: Tạo đơn món mới kèm kiểm tra tính hợp lệ dữ liệu. Khi đơn được tạo, hệ thống kích hoạt tiến trình bếp mô phỏng và phát thông báo thời gian thực.
     - `GET /orders`: Lấy danh sách đơn hàng (hỗ trợ lọc `?status=...`).
     - `GET /orders/{id}`: Tra cứu chi tiết một đơn theo `Guid`.
     - `POST /orders/{id}/status`: Cho phép bếp/quản trị cập nhật trạng thái đơn thủ công (`Pending`, `Preparing`, `Ready`, `Cancelled`) và phát ngay thông báo SignalR.
   - **Tuyến SignalR Hub (`/hubs/orders`):**
     - Quản lý kết nối theo nhóm: `order:{orderId}` để đảm bảo mỗi khách chỉ nhận thông báo về đơn của chính mình.
     - Phương thức `WatchOrder(orderId)`: Máy khách đăng ký tham gia nhóm theo dõi đơn.
     - Phương thức `LeaveOrder(orderId)`: Rời nhóm khi đơn hoàn tất.
     - Sự kiện phát thời gian thực: `OrderStatusChanged` với dữ liệu `OrderStatusMessage` (OrderId, Status, Note, UpdatedAt).

2. **`src/Order.Client` (Console Application với `Microsoft.AspNetCore.SignalR.Client`):**
   - Thiết lập kết nối bất đồng bộ tới SignalR Hub (`/hubs/orders`) với tính năng tự động kết nối lại (`WithAutomaticReconnect`).
   - Đăng ký lắng nghe sự kiện `OrderStatusChanged`.
   - Gửi yêu cầu đặt món qua HTTP POST `POST /orders`, sau đó lập tức gọi `WatchOrder` để gia nhập nhóm thời gian thực.
   - Nhận thông báo tự động từ bếp khi trạng thái chuyển từ `Pending` -> `Preparing` (sau 2s) -> `Ready` (sau 3s).
   - Kiểm thử cập nhật thủ công từ bếp (`Cancelled`) và kiểm thử đường lỗi REST (`400 Bad Request`, `404 Not Found`).

---

## 3. Hướng dẫn cài đặt và chạy thử nghiệm

### Yêu cầu môi trường
- .NET 10 SDK (`dotnet --version` >= 10.0.x)
- Visual Studio 2022/2026 hoặc Visual Studio Code

### Các bước khởi chạy

1. **Khôi phục và biên dịch toàn bộ solution:**
   ```powershell
   dotnet build
   ```

2. **Khởi chạy Order API (Server):**
   Mở terminal thứ nhất tại thư mục dự án:
   ```powershell
   dotnet run --project src/Order.Api --urls http://localhost:5000
   ```
   API sẽ sẵn sàng lắng nghe tại `http://localhost:5000`, SignalR Hub tại `http://localhost:5000/hubs/orders`.

3. **Khởi chạy Order Client (Máy khách kiểm thử):**
   Mở terminal thứ hai:
   ```powershell
   dotnet run --project src/Order.Client -- http://localhost:5000
   ```
   Chương trình sẽ tự động mở kết nối SignalR, đặt món, nhận thông báo thời gian thực khi trạng thái đổi và in trực tiếp kết quả lên màn hình.

---

## 4. Lộ trình phát triển đề tài

- [x] **Giai đoạn 1 (Tuần 1 - 2):** Khởi tạo Solution, RESTful Minimal API và Console Client kiểm thử.
- [x] **Giai đoạn 2 (Tuần 3 - 4):** Tích hợp dịch vụ thông báo thời gian thực SignalR Hub (`/hubs/orders`), nhóm theo dõi theo `OrderId`, chuyển trạng thái `Pending` -> `Preparing` -> `Ready`.
- [ ] **Giai đoạn 3 (Tuần 5 - 7):** Tích hợp RabbitMQ Message Broker, xây dựng `Kitchen Worker` xử lý đơn hàng bất đồng bộ, xử lý ACK/NACK, Dead-Letter Queue và Idempotency.
- [ ] **Giai đoạn 4 (Tuần 8+):** Kết nối cơ sở dữ liệu bền vững (PostgreSQL/SQL Server), đóng gói Docker cho từng service và cấu hình `docker-compose.yml`, hoàn thiện tài liệu và quyển báo cáo bài tập lớn.
