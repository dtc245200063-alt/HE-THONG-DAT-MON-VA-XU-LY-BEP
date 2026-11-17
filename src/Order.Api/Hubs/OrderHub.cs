using Microsoft.AspNetCore.SignalR;

namespace Order.Api.Hubs;

public sealed class OrderHub : Hub
{
    public static string GetOrderGroupName(Guid orderId) => $"order:{orderId}";

    // Khách hàng hoặc màn hình bếp đăng ký theo dõi trạng thái một đơn hàng
    public async Task WatchOrder(Guid orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetOrderGroupName(orderId));
        await Clients.Caller.SendAsync("SubscribedToOrder", new
        {
            orderId,
            message = $"Da theo doi don hang {orderId} thanh cong tren kenh thoi gian thuc."
        });
    }

    // Hủy theo dõi đơn hàng
    public async Task LeaveOrder(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetOrderGroupName(orderId));
    }

    // Bếp hoặc quản lý phát thông báo chung tới toàn bộ khách hàng đang kết nối
    public async Task BroadcastKitchenNotice(string message)
    {
        await Clients.All.SendAsync("KitchenNotice", message);
    }
}
