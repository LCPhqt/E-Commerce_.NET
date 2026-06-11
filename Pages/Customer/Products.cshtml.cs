using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Customer;

public class ProductsModel : PageModel
{
    private readonly IProductService _productService;

    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
    public string? SearchQuery { get; set; }
    public string? SortBy { get; set; }

    // Pagination
    public int PageIndex { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 8;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public ProductsModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync(int? categoryId, string? search, string? sortBy, int? pageIndex)
    {
        Categories = await _productService.GetAllCategoriesAsync();
        SelectedCategoryId = categoryId;
        SearchQuery = search;
        SortBy = sortBy ?? "newest";
        PageIndex = pageIndex ?? 0;

        var result = await _productService.GetPagedAsync(
            PageIndex, PageSize, categoryId, search, SortBy);

        Products = result.Items;
        TotalCount = result.TotalCount;
    }
}
