using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.DanhMuc)
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<List<Product>> GetByCategoryAsync(int categoryId)
    {
        return await _context.Products
            .Include(p => p.DanhMuc)
            .Where(p => p.DanhMucId == categoryId)
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<List<Product>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllAsync();

        keyword = keyword.Trim().ToLower();
        return await _context.Products
            .Include(p => p.DanhMuc)
            .Where(p => p.TenSanPham.ToLower().Contains(keyword)
                     || (p.MoTa != null && p.MoTa.ToLower().Contains(keyword)))
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.DanhMuc)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4)
    {
        return await _context.Products
            .Include(p => p.DanhMuc)
            .Where(p => p.DanhMucId == categoryId && p.Id != excludeProductId)
            .OrderByDescending(p => p.Id)
            .Take(take)
            .ToListAsync();
    }

    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, int? categoryId = null, string? search = null, string sortBy = "newest")
    {
        var query = _context.Products
            .Include(p => p.DanhMuc)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.DanhMucId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(p => p.TenSanPham.ToLower().Contains(kw)
                                  || (p.MoTa != null && p.MoTa.ToLower().Contains(kw)));
        }

        var totalCount = await query.CountAsync();

        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Gia),
            "price_desc" => query.OrderByDescending(p => p.Gia),
            "name" => query.OrderBy(p => p.TenSanPham),
            _ => query.OrderByDescending(p => p.Id)
        };

        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Ten)
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddCategoryAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var hasProducts = await _context.Products.AnyAsync(p => p.DanhMucId == id);
        if (hasProducts)
            throw new InvalidOperationException("Không thể xóa danh mục có sản phẩm.");

        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
