using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin.Products;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly IProductService _productService;

    public Product? Product { get; set; }

    public DeleteModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync(int id)
    {
        Product = await _productService.GetByIdAsync(id);
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        await _productService.DeleteAsync(id);
        TempData["SuccessMessage"] = "Xóa sản phẩm thành công.";
        return RedirectToPage("/Admin/Products/Index");
    }
}
