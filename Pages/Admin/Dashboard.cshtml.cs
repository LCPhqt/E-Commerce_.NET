using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IOrderService _orderService;

    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, int> OrderStatusCounts { get; set; } = new();

    public DashboardModel(AppDbContext context, IOrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    public async Task OnGetAsync()
    {
        TotalProducts = await _context.Products.CountAsync();
        TotalOrders = await _orderService.GetTotalOrdersAsync();
        TotalUsers = await _context.NguoiDung.CountAsync();
        TotalRevenue = await _orderService.GetTotalRevenueAsync();
        OrderStatusCounts = await _orderService.GetOrderStatusCountsAsync();
    }
}
