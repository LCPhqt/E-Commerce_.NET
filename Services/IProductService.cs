using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Services;

public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<List<Product>> GetByCategoryAsync(int categoryId);
    Task<List<Product>> SearchAsync(string keyword);
    Task<Product?> GetByIdAsync(int id);
    Task<List<Product>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4);
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, int? categoryId = null, string? search = null, string sortBy = "newest");
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task AddCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(int id);
}
