using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Order.Client.Contracts;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine("======================================================================");
Console.WriteLine($"  HE THONG DAT MON VA XU LY BEP - GIAI DOAN 2: SIGNALR REALTIME");
Console.WriteLine($"  May khach ket noi toi: {apiBaseUrl}");
Console.WriteLine("======================================================================\n");

// 1. Thiet lap ket noi SignalR Hub
Console.WriteLine("1. Khoi tao va thiet lap ket noi SignalR Hub (/hubs/orders)...");
await using var hubConnection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/orders")
    .WithAutomaticReconnect()
    .Build();

var orderReadyTcs = new TaskCompletionSource<bool>();
Guid currentWatchingOrderId = Guid.Empty;

// Dang ky callback nhan phan hoi khi dang ky nhom thanh cong
hubConnection.On<dynamic>("SubscribedToOrder", data =>
{
    Console.WriteLine($"   [SignalR HUB] {data}");
});

// Dang ky callback nhan thong bao khi trang thai don hang thay doi theo thoi gian thuc
hubConnection.On<OrderStatusMessage>("OrderStatusChanged", message =>
{
    Console.WriteLine($"\n   >>> [THONG BAO REALTIME SIGNALR]");
    Console.WriteLine($"       Ma don     : {message.OrderId}");
    Console.WriteLine($"       Trang thai : [{message.Status.ToUpper()}]");
    Console.WriteLine($"       Chi tiet   : {message.Note}");
    Console.WriteLine($"       Thoi diem  : {message.UpdatedAt:HH:mm:ss dd/MM/yyyy}\n");

    if (message.OrderId == currentWatchingOrderId && message.Status is "Ready" or "Cancelled")
    {
        orderReadyTcs.TrySetResult(true);
    }
});

await hubConnection.StartAsync();
Console.WriteLine("   -> Ket noi SignalR thanh cong! San sang nhan thong bao thoi gian thuc.\n");

// 2. Xem danh sach mon an (GET /menu)
Console.WriteLine("2. Lay thuc don nha hang (GET /menu):");
var menu = await http.GetFromJsonAsync<List<MenuItemDto>>("/menu") ?? [];
foreach (var item in menu)
{
    var statusText = item.IsAvailable ? "Con mon" : "Tam het";
    Console.WriteLine($"   - [{item.Id}] {item.Name}: {item.Price:N0} VND ({statusText})");
}

// 3. Gui yeu cau dat mon hop le (POST /orders -> 201 Created)
Console.WriteLine("\n3. Gui yeu cau dat mon hop le:");
var validRequest = new CreateOrderRequest("Luong Viet Anh", MenuItemId: 1, Quantity: 2);
var createdOrder = await SendAndHandleAsync<OrderResponse>(
    () => http.PostAsJsonAsync("/orders", validRequest));

if (createdOrder is not null)
{
    Console.WriteLine($"   -> Dat thanh cong! Ma don: {createdOrder.Id}");
    Console.WriteLine($"   -> Khach: {createdOrder.CustomerName} | Mon: {createdOrder.MenuItemName} x{createdOrder.Quantity}");
    Console.WriteLine($"   -> Tong tien: {createdOrder.TotalPrice:N0} VND | Trang thai ban dau: [{createdOrder.Status}]");

    // 4. Dang ky theo doi trang thai don hang qua SignalR (WatchOrder)
    currentWatchingOrderId = createdOrder.Id;
    Console.WriteLine($"\n4. Dang ky nhom theo doi don hang qua SignalR: 'order:{createdOrder.Id}'...");
    await hubConnection.InvokeAsync("WatchOrder", createdOrder.Id);

    Console.WriteLine("   -> Dang cho bep tiep nhan va che bien (tu dong nhan thong bao realtime)...");

    // Cho tin hieu realtime tu bep (Preparing -> Ready) voi thoi gian cho toi da 12 giay
    var completedTask = await Task.WhenAny(orderReadyTcs.Task, Task.Delay(12000));
    if (completedTask == orderReadyTcs.Task)
    {
        Console.WriteLine("   -> Quy trinh che bien mon da HOAN TAT (Ready) qua kenh SignalR!");
    }
    else
    {
        Console.WriteLine("   [!] Timeout cho thong bao tu bep.");
    }

    // 5. Tra cuu lai chi tiet don sau khi bep da xu ly (GET /orders/{id})
    Console.WriteLine("\n5. Tra cuu lai chi tiet don tren he thong (GET /orders/{id}):");
    var finalDetail = await SendAndHandleAsync<OrderResponse>(
        () => http.GetAsync($"/orders/{createdOrder.Id}"));
    if (finalDetail is not null)
    {
        Console.WriteLine($"   -> Ma don: {finalDetail.Id} | Trang thai hien tai: [{finalDetail.Status}]");
    }
}

// 6. Kiem thu cap nhat thu cong trang thai don tu phia Bep (POST /orders/{id}/status)
Console.WriteLine("\n6. Kiem thu bep chu dong cap nhat trang thai thu cong va ban SignalR:");
var order2Request = new CreateOrderRequest("Luong Viet Anh", MenuItemId: 2, Quantity: 1);
var order2 = await SendAndHandleAsync<OrderResponse>(
    () => http.PostAsJsonAsync("/orders", order2Request));

if (order2 is not null)
{
    await hubConnection.InvokeAsync("WatchOrder", order2.Id);
    Console.WriteLine($"   -> Don moi #{order2.Id} da tao. Gui yeu cau tu bep huy don vi het nguyen lieu:");
    var cancelRequest = new UpdateOrderStatusRequest("Cancelled", "Bep bao het nguyen lieu dot xuat.");
    await SendAndHandleAsync<OrderResponse>(
        () => http.PostAsJsonAsync($"/orders/{order2.Id}/status", cancelRequest));
    await Task.Delay(1000);
}

// 7. Kiem thu cac truong hop loi dau vao (Validation 400 & 404)
Console.WriteLine("\n7. Kiem thu duong loi HTTP REST (400 Bad Request & 404 Not Found):");
var invalidRequest = new CreateOrderRequest("", MenuItemId: 1, Quantity: 1);
await SendAndHandleAsync<OrderResponse>(
    () => http.PostAsJsonAsync("/orders", invalidRequest));

await SendAndHandleAsync<OrderResponse>(
    () => http.GetAsync($"/orders/{Guid.NewGuid()}"));

Console.WriteLine("\n=== HOAN TAT TOAN BO KIEM THU GIAI DOAN 2 (REST + SIGNALR) ===");

static async Task<T?> SendAndHandleAsync<T>(Func<Task<HttpResponseMessage>> sendAsync)
{
    using var response = await sendAsync();
    Console.WriteLine($"   [HTTP {(int)response.StatusCode} {response.StatusCode}]");

    if (response.IsSuccessStatusCode)
    {
        return await response.Content.ReadFromJsonAsync<T>();
    }

    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"   [API ERROR] {error?.Code}: {error?.Message}");
    return default;
}
