using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _context;

    public CartService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetCartAsync(string username)
    {
        return await _context.Carts
            .Include(c => c.CartItems!)
                .ThenInclude(ci => ci.SanPham!)
                    .ThenInclude(sp => sp.DanhMuc)
            .FirstOrDefaultAsync(c => c.TenDangNhap == username);
    }

    public async Task<Cart> GetOrCreateCartAsync(string username)
    {
        var cart = await GetCartAsync(username);
        if (cart == null)
        {
            cart = new Cart { TenDangNhap = username, NgayTao = DateTime.Now };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }
        return cart;
    }

    public async Task AddToCartAsync(string username, int productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            throw new InvalidOperationException("Sản phẩm không tồn tại.");

        if (product.SoLuongTon < quantity)
            throw new InvalidOperationException("Số lượng tồn không đủ.");

        var cart = await GetOrCreateCartAsync(username);

        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.GioHangId == cart.Id && ci.SanPhamId == productId);

        if (existingItem != null)
        {
            existingItem.SoLuong += quantity;
            existingItem.DonGia = product.Gia;
        }
        else
        {
            var cartItem = new CartItem
            {
                GioHangId = cart.Id,
                SanPhamId = productId,
                SoLuong = quantity,
                DonGia = product.Gia
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateQuantityAsync(string username, int cartItemId, int quantity)
    {
        if (quantity < 1)
        {
            await RemoveFromCartAsync(username, cartItemId);
            return;
        }

        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.TenDangNhap == username);
        if (cart == null) return;

        var item = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.GioHangId == cart.Id);
        if (item != null)
        {
            item.SoLuong = quantity;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveFromCartAsync(string username, int cartItemId)
    {
        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.TenDangNhap == username);
        if (cart == null) return;

        var item = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.GioHangId == cart.Id);
        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearCartAsync(string username)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.TenDangNhap == username);
        if (cart != null)
        {
            _context.CartItems.RemoveRange(cart.CartItems!);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetTotalAsync(string username)
    {
        var cart = await GetCartAsync(username);
        if (cart?.CartItems == null) return 0;
        return cart.CartItems.Sum(ci => ci.SoLuong * ci.DonGia);
    }
}
