using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin.Categories;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IProductService _productService;

    public Category? Category { get; set; }

    public EditModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync(int id)
    {
        Category = await _productService.GetCategoryByIdAsync(id);
    }

    public async Task<IActionResult> OnPostAsync(int id, string ten, string? moTa)
    {
        var category = await _productService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Danh mục không tồn tại.";
            return RedirectToPage("/Admin/Categories/Index");
        }

        category.Ten = ten.Trim();
        category.MoTa = moTa?.Trim();

        await _productService.UpdateCategoryAsync(category);
        TempData["SuccessMessage"] = "Cập nhật danh mục thành công.";
        return RedirectToPage("/Admin/Categories/Index");
    }
}
