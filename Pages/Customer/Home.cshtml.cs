using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Customer;

public class HomeModel : PageModel
{
    private readonly IProductService _productService;

    public List<Product> NewProducts { get; set; } = new();

    public HomeModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync()
    {
        var allProducts = await _productService.GetAllAsync();
        NewProducts = allProducts.Take(8).ToList();
    }
}
