using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin.Products;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IProductService _productService;

    public List<Category> Categories { get; set; } = new();

    public CreateModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync()
    {
        Categories = await _productService.GetAllCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync(string tenSanPham, int danhMucId, decimal gia, int soLuongTon, string? hinhAnhUrl, string? moTa)
    {
        if (string.IsNullOrWhiteSpace(tenSanPham))
        {
            ModelState.AddModelError("", "Vui lòng nhập tên sản phẩm.");
            Categories = await _productService.GetAllCategoriesAsync();
            return Page();
        }

        var product = new Product
        {
            TenSanPham = tenSanPham.Trim(),
            DanhMucId = danhMucId,
            Gia = gia,
            SoLuongTon = soLuongTon,
            HinhAnhUrl = hinhAnhUrl?.Trim(),
            MoTa = moTa?.Trim()
        };

        await _productService.AddAsync(product);
        TempData["SuccessMessage"] = "Thêm sản phẩm thành công.";
        return RedirectToPage("/Admin/Products/Index");
    }
}
