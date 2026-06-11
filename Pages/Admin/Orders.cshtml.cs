using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin;

[Authorize(Roles = "Admin")]
public class OrdersModel : PageModel
{
    private readonly IOrderService _orderService;

    public List<Order> Orders { get; set; } = new();

    public OrdersModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task OnGetAsync()
    {
        Orders = await _orderService.GetAllOrdersAsync();
    }

    public async Task<IActionResult> OnPostAsync(int orderId, string trangThai)
    {
        await _orderService.UpdateOrderStatusAsync(orderId, trangThai);
        TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng #{orderId} thành công.";
        return RedirectToPage();
    }
}
