using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Services;

public interface ICartService
{
    Task<Cart?> GetCartAsync(string username);
    Task<Cart> GetOrCreateCartAsync(string username);
    Task AddToCartAsync(string username, int productId, int quantity);
    Task UpdateQuantityAsync(string username, int cartItemId, int quantity);
    Task RemoveFromCartAsync(string username, int cartItemId);
    Task ClearCartAsync(string username);
    Task<decimal> GetTotalAsync(string username);
}
