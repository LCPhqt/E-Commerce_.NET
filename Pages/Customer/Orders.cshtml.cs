using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Customer;

[Authorize]
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
        var username = User.Identity!.Name!;
        Orders = await _orderService.GetUserOrdersAsync(username);
    }

    public async Task<IActionResult> OnPostCancelAsync(int orderId)
    {
        var username = User.Identity!.Name!;
        try
        {
            await _orderService.CancelOrderAsync(orderId, username);
            TempData["SuccessMessage"] = $"Đơn hàng #{orderId} đã được hủy thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }
}
