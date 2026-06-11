using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin.Products;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IProductService _productService;

    public Product? Product { get; set; }
    public List<Category> Categories { get; set; } = new();

    public EditModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync(int id)
    {
        Product = await _productService.GetByIdAsync(id);
        Categories = await _productService.GetAllCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync(int id, string tenSanPham, int danhMucId, decimal gia, int soLuongTon, string? hinhAnhUrl, string? moTa)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
            return RedirectToPage("/Admin/Products/Index");
        }

        product.TenSanPham = tenSanPham.Trim();
        product.DanhMucId = danhMucId;
        product.Gia = gia;
        product.SoLuongTon = soLuongTon;
        product.HinhAnhUrl = hinhAnhUrl?.Trim();
        product.MoTa = moTa?.Trim();

        await _productService.UpdateAsync(product);
        TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
        return RedirectToPage("/Admin/Products/Index");
    }
}
