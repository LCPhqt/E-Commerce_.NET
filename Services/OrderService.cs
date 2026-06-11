using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ICartService _cartService;

    public OrderService(AppDbContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public async Task<Order> CreateOrderAsync(string username, string diaChiGiao, string? ghiChu)
    {
        var cart = await _cartService.GetCartAsync(username);
        if (cart?.CartItems == null || !cart.CartItems.Any())
            throw new InvalidOperationException("Giỏ hàng trống.");

        // Check stock
        foreach (var item in cart.CartItems)
        {
            var product = await _context.Products.FindAsync(item.SanPhamId);
            if (product == null)
                throw new InvalidOperationException($"Sản phẩm ID {item.SanPhamId} không tồn tại.");
            if (product.SoLuongTon < item.SoLuong)
                throw new InvalidOperationException($"Sản phẩm \"{product.TenSanPham}\" không đủ số lượng tồn.");
        }

        var total = cart.CartItems.Sum(ci => ci.SoLuong * ci.DonGia);

        var order = new Order
        {
            TenDangNhap = username,
            NgayDat = DateTime.Now,
            TongTien = total,
            TrangThai = "Chờ xử lý",
            DiaChiGiao = diaChiGiao,
            GhiChu = ghiChu
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Create order details and update stock
        foreach (var item in cart.CartItems)
        {
            var product = await _context.Products.FindAsync(item.SanPhamId)!;
            var orderDetail = new OrderDetail
            {
                DonHangId = order.Id,
                SanPhamId = item.SanPhamId,
                TenSanPham = product!.TenSanPham,
                SoLuong = item.SoLuong,
                DonGia = item.DonGia
            };
            _context.OrderDetails.Add(orderDetail);

            product.SoLuongTon -= item.SoLuong;
        }

        // Clear cart
        _context.CartItems.RemoveRange(cart.CartItems);

        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<List<Order>> GetUserOrdersAsync(string username)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.SanPham)
            .Where(o => o.TenDangNhap == username)
            .OrderByDescending(o => o.NgayDat)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.SanPham)
            .Include(o => o.NguoiDung)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.SanPham)
            .Include(o => o.NguoiDung)
            .OrderByDescending(o => o.NgayDat)
            .ToListAsync();
    }

    public async Task UpdateOrderStatusAsync(int orderId, string trangThai)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.TrangThai = trangThai;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetTotalRevenueAsync()
    {
        return await _context.Orders
            .Where(o => o.TrangThai == "Đã giao")
            .SumAsync(o => o.TongTien);
    }

    public async Task<int> GetTotalOrdersAsync()
    {
        return await _context.Orders.CountAsync();
    }

    public async Task<Dictionary<string, int>> GetOrderStatusCountsAsync()
    {
        return await _context.Orders
            .GroupBy(o => o.TrangThai)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);
    }
}
