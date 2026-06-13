using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Customer;

public class HomeModel : PageModel
{
    private readonly IProductService _productService;
    private readonly AppDbContext _context;

    public List<Product> NewProducts { get; set; } = new();
    public NguoiDung? CurrentUser { get; set; }
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    public HomeModel(IProductService productService, AppDbContext context)
    {
        _productService = productService;
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var allProducts = await _productService.GetAllAsync();
        NewProducts = allProducts.Take(8).ToList();

        if (IsAuthenticated)
        {
            var username = User.Identity?.Name;
            CurrentUser = await _context.NguoiDung
                .FirstOrDefaultAsync(u => u.TenDangNhap == username);
        }
    }
}
