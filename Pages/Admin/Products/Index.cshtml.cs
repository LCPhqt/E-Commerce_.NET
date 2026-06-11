using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Admin.Products;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IProductService _productService;

    public List<Product> Products { get; set; } = new();

    public IndexModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync()
    {
        Products = await _productService.GetAllAsync();
    }
}
