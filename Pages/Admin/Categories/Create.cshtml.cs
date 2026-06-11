using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin.Categories;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IProductService _productService;

    public CreateModel(IProductService productService)
    {
        _productService = productService;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string ten, string? moTa)
    {
        if (string.IsNullOrWhiteSpace(ten))
        {
            ModelState.AddModelError("", "Vui lòng nhập tên danh mục.");
            return Page();
        }

        var category = new Category
        {
            Ten = ten.Trim(),
            MoTa = moTa?.Trim()
        };

        await _productService.AddCategoryAsync(category);
        TempData["SuccessMessage"] = "Thêm danh mục thành công.";
        return RedirectToPage("/Admin/Categories/Index");
    }
}
