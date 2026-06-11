using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Customer;

public class ProductDetailsModel : PageModel
{
    private readonly IProductService _productService;

    public Product? Product { get; set; }
    public List<Product> RelatedProducts { get; set; } = new();

    public ProductDetailsModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync(int id)
    {
        Product = await _productService.GetByIdAsync(id);
        if (Product != null)
        {
            RelatedProducts = await _productService.GetRelatedAsync(Product.DanhMucId, Product.Id, 4);
        }
    }
}
