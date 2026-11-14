# Đề tài 04: Hệ thống đặt món và xử lý tại bếp

> **Học phần:** Phát triển ứng dụng phân tán  
> **Sinh viên thực hiện:** Lương Việt Anh  
> **Mã sinh viên:** DTC245200063  
> **Email:** dtc245200063@ictu.edu.vn  
> **Nền tảng:** .NET 10 (C#) - ASP.NET Core Minimal API  

---

## 1. Tổng quan đề tài

Xây dựng hệ thống phân tán phục vụ việc đặt món nhà hàng và đồng bộ quy trình chế biến tại bếp theo thời gian thực:
- Khách hàng xem thực đơn, gửi yêu cầu đặt món và theo dõi trạng thái đơn hàng.
- Đơn hàng được đưa vào hàng đợi thông điệp (RabbitMQ) để chuyển tiếp bất đồng bộ tới bộ phận bếp.
- Bộ phận bếp (**Kitchen Worker**) tiêu thụ (consume) đơn hàng từ hàng đợi, cập nhật tiến độ chế biến (`Pending` -> `Preparing` -> `Ready` / `Cancelled`).
- Dịch vụ thông báo thời gian thực (**Notification Service** sử dụng SignalR) bắn cập nhật ngay lập tức về phía giao diện khách hàng.

### Kiến trúc tổng thể hệ thống
```text
Client -> Order API -> RabbitMQ -> Kitchen Worker -> RabbitMQ -> Notification Service -> Client
              |                         |
              +------> Database <-------+
```

---

## 2. Trạng thái hiện tại: Giai đoạn 1 (Tuần 1 - 2)

Hiện tại dự án đang hoàn thành **Giai đoạn 1**: Thiết kế Hợp đồng RESTful API, xây dựng dịch vụ API cốt lõi và ứng dụng kiểm thử độc lập.

### Các thành phần đã triển khai:
1. **`src/Order.Api` (ASP.NET Core Minimal API):**
   - Chạy trên .NET 10 (`net10.0`).
   - Cung cấp các endpoint RESTful chuẩn:
     - `GET /`: Kiểm tra tình trạng hoạt động của service.
     - `GET /menu`: Xem danh mục các món ăn và tình trạng còn món/hết món.
     - `POST /orders`: Tạo đơn đặt món mới, kiểm tra tính hợp lệ dữ liệu (tên khách hàng, số lượng, kiểm tra món tồn tại và khả dụng).
     - `GET /orders`: Lấy danh sách đơn hàng (hỗ trợ lọc theo query `?status=...`).
     - `GET /orders/{id}`: Tra cứu chi tiết đơn hàng theo `Guid`.
   - Sử dụng kho lưu trữ tạm thời (`InMemoryOrderStore`).

2. **`src/Order.Client` (Console Application):**
   - Đóng vai trò máy khách độc lập kiểm thử tự động toàn diện các kịch bản của Order API:
     - Kịch bản thành công: Gọi menu, tạo đơn hợp lệ, truy vấn lại chi tiết đơn vừa tạo, lọc danh sách đơn đang chờ xử lý (`Pending`).
     - Kịch bản lỗi: Kiểm thử mã lỗi `400 Bad Request` khi dữ liệu không hợp lệ (tên khách hàng rỗng) và `404 Not Found` khi truy vấn ID không tồn tại.

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

2. **Khởi chạy Order API:**
   Mở terminal thứ nhất tại thư mục gốc của project:
   ```powershell
   dotnet run --project src/Order.Api --urls http://localhost:5000
   ```
   API sẽ sẵn sàng lắng nghe tại `http://localhost:5000`.

3. **Khởi chạy Order Client kiểm thử:**
   Mở terminal thứ hai:
   ```powershell
   dotnet run --project src/Order.Client -- http://localhost:5000
   ```
   Chương trình sẽ tự động thực hiện các ca kiểm thử và in kết quả chi tiết lên màn hình console.

---

## 4. Lộ trình phát triển các giai đoạn tiếp theo

- [x] **Giai đoạn 1 (Tuần 1 - 2):** Khởi tạo Solution, RESTful Minimal API và Console Client kiểm thử.
- [ ] **Giai đoạn 2 (Tuần 3 - 4):** Tích hợp dịch vụ thông báo thời gian thực SignalR Hub (`/hubs/order-notification`).
- [ ] **Giai đoạn 3 (Tuần 5 - 7):** Tích hợp RabbitMQ Message Broker, xây dựng `Kitchen Worker` xử lý đơn hàng bất đồng bộ, xử lý ACK/NACK, Dead-Letter Queue và Idempotency.
- [ ] **Giai đoạn 4 (Tuần 8+):** Kết nối cơ sở dữ liệu bền vững (PostgreSQL/SQL Server), đóng gói Docker cho từng service và cấu hình `docker-compose.yml`, hoàn thiện tài liệu và quyển báo cáo bài tập lớn.
