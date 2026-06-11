using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Customer;

[Authorize]
public class CartModel : PageModel
{
    private readonly ICartService _cartService;

    public List<CartItem>? CartItems { get; set; }
    public decimal Total { get; set; }

    public CartModel(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task OnGetAsync()
    {
        var username = User.Identity!.Name!;
        var cart = await _cartService.GetCartAsync(username);
        CartItems = cart?.CartItems?.ToList();
        Total = await _cartService.GetTotalAsync(username);
    }

    public async Task<IActionResult> OnPostAddAsync(int productId, int quantity = 1)
    {
        var username = User.Identity!.Name!;
        try
        {
            await _cartService.AddToCartAsync(username, productId, quantity);
            TempData["SuccessMessage"] = "Đã thêm vào giỏ hàng.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int cartItemId, int quantity)
    {
        var username = User.Identity!.Name!;
        await _cartService.UpdateQuantityAsync(username, cartItemId, quantity);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int cartItemId)
    {
        var username = User.Identity!.Name!;
        await _cartService.RemoveFromCartAsync(username, cartItemId);
        return RedirectToPage();
    }
}
