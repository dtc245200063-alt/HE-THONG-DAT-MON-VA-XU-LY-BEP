using Order.Api.Contracts;

namespace Order.Api.Services;

public sealed class InMemoryOrderStore
{
    public static readonly string[] ValidStatuses =
        ["Pending", "Preparing", "Ready", "Cancelled"];

    private readonly List<MenuItemDto> _menu =
    [
        new(1, "Pho Bo Tai Chin", 55000m, true),
        new(2, "Bun Cha Ha Hanoi", 50000m, true),
        new(3, "Com Tam Suon Bi", 45000m, true),
        new(4, "Tra Dao Cam Sa", 25000m, false)
    ];

    private readonly List<OrderResponse> _orders = [];
    private readonly Lock _syncLock = new();

    public IReadOnlyList<MenuItemDto> GetMenu() => _menu;

    public MenuItemDto? GetMenuItem(int id) =>
        _menu.FirstOrDefault(m => m.Id == id);

    public OrderResponse CreateOrder(string customerName, MenuItemDto item, int quantity)
    {
        var order = new OrderResponse(
            Id: Guid.NewGuid(),
            CustomerName: customerName.Trim(),
            MenuItemId: item.Id,
            MenuItemName: item.Name,
            Quantity: quantity,
            TotalPrice: item.Price * quantity,
            OrderedAt: DateTimeOffset.UtcNow,
            Status: "Pending");

        lock (_syncLock)
        {
            _orders.Add(order);
        }

        return order;
    }

    public OrderResponse? UpdateOrderStatus(Guid id, string newStatus)
    {
        lock (_syncLock)
        {
            var index = _orders.FindIndex(o => o.Id == id);
            if (index < 0) return null;

            var existing = _orders[index];
            var updated = existing with { Status = newStatus };
            _orders[index] = updated;
            return updated;
        }
    }

    public IReadOnlyList<OrderResponse> GetOrders(string? status)
    {
        lock (_syncLock)
        {
            if (string.IsNullOrWhiteSpace(status))
                return _orders.OrderByDescending(o => o.OrderedAt).ToList();

            return _orders
                .Where(o => string.Equals(o.Status, status.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(o => o.OrderedAt)
                .ToList();
        }
    }

    public OrderResponse? GetOrderById(Guid id)
    {
        lock (_syncLock)
        {
            return _orders.FirstOrDefault(o => o.Id == id);
        }
    }
}
