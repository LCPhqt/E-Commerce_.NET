using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ReportsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IOrderService _orderService;

    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public List<DeliveredOrderDetail> DeliveredOrderDetails { get; set; } = new();

    public ReportsModel(AppDbContext context, IOrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    public async Task OnGetAsync()
    {
        TotalRevenue = await _orderService.GetTotalRevenueAsync();
        TotalOrders = await _orderService.GetTotalOrdersAsync();

        DeliveredOrders = await _context.Orders.CountAsync(o => o.TrangThai == "Đã giao");

        DeliveredOrderDetails = await _context.OrderDetails
            .Include(od => od.DonHang!)
                .ThenInclude(o => o.NguoiDung)
            .Include(od => od.SanPham)
            .Where(od => od.DonHang!.TrangThai == "Đã giao")
            .OrderByDescending(od => od.DonHang!.NgayDat)
            .Select(od => new DeliveredOrderDetail
            {
                OrderId = od.DonHangId,
                CustomerName = od.DonHang!.NguoiDung!.Ho + " " + od.DonHang.NguoiDung.Ten,
                OrderDate = od.DonHang.NgayDat,
                ProductName = od.TenSanPham,
                Quantity = od.SoLuong,
                UnitPrice = od.DonGia,
                TotalPrice = od.SoLuong * od.DonGia
            })
            .ToListAsync();
    }

    public class DeliveredOrderDetail
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
