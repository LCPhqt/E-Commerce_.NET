using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Customer;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public List<CartItem>? CartItems { get; set; }
    public decimal Total { get; set; }
    public bool HasItems { get; set; }

    public CheckoutModel(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }

    public async Task OnGetAsync(int? buyNow)
    {
        var username = User.Identity!.Name!;

        // Mua ngay: thêm sản phẩm vào giỏ rồi redirect (xóa query param)
        if (buyNow.HasValue)
        {
            try
            {
                await _cartService.AddToCartAsync(username, buyNow.Value, 1);
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                Response.Redirect("/Customer/Products");
                return;
            }
        }

        var cart = await _cartService.GetCartAsync(username);
        CartItems = cart?.CartItems?.ToList();
        Total = await _cartService.GetTotalAsync(username);
        HasItems = CartItems != null && CartItems.Any();
    }

    public async Task<IActionResult> OnPostAsync(string diaChiGiao, string? ghiChu)
    {
        if (string.IsNullOrWhiteSpace(diaChiGiao))
        {
            ModelState.AddModelError("", "Vui lòng nhập địa chỉ giao hàng.");
            return await ReloadPage();
        }

        var username = User.Identity!.Name!;
        try
        {
            var order = await _orderService.CreateOrderAsync(username, diaChiGiao.Trim(), ghiChu?.Trim());
            TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn hàng: #" + order.Id;
            return RedirectToPage("/Customer/Orders");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await ReloadPage();
        }
    }

    private async Task<IActionResult> ReloadPage()
    {
        await OnGetAsync(null);
        return Page();
    }
}
