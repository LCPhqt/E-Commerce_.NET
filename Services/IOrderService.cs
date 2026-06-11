using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string username, string diaChiGiao, string? ghiChu);
    Task<List<Order>> GetUserOrdersAsync(string username);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<List<Order>> GetAllOrdersAsync();
    Task UpdateOrderStatusAsync(int orderId, string trangThai);
    Task<decimal> GetTotalRevenueAsync();
    Task<int> GetTotalOrdersAsync();
    Task<Dictionary<string, int>> GetOrderStatusCountsAsync();
}
