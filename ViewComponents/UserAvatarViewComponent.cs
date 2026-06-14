using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerceFinalProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerceFinalProject.ViewComponents;

[ViewComponent(Name = "UserAvatar")]
public class UserAvatarViewComponent : ViewComponent
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public UserAvatarViewComponent(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// Render avatar cho user hiện tại.
    /// size: kích thước CSS (px), mặc định 32.
    /// showName: hiển thị FullName bên cạnh.
    /// shape: "circle" (mặc định) hoặc "square".
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync(int size = 32, bool showName = true, string shape = "circle")
    {
        var username = User.Identity?.Name;
        string? avatarUrl = null;
        string fullName = string.Empty;

        if (!string.IsNullOrEmpty(username) && User.Identity?.IsAuthenticated == true)
        {
            var cacheKey = $"user_avatar_{username}";
            if (!_cache.TryGetValue(cacheKey, out (string? AvatarUrl, string FullName)? cached))
            {
                var user = await _context.NguoiDung
                    .Where(u => u.TenDangNhap == username)
                    .Select(u => new { u.AvatarUrl, u.Ho, u.Ten })
                    .FirstOrDefaultAsync();

                cached = user == null
                    ? ((string?, string)?)(null, string.Empty)
                    : (user.AvatarUrl, $"{user.Ho} {user.Ten}".Trim());

                _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(5));
            }

            avatarUrl = cached?.AvatarUrl;
            fullName = cached?.FullName ?? string.Empty;
        }

        return View(new UserAvatarViewModel
        {
            AvatarUrl = string.IsNullOrEmpty(avatarUrl) ? null : avatarUrl,
            FullName = fullName,
            Size = size,
            ShowName = showName,
            Shape = shape
        });
    }
}

public class UserAvatarViewModel
{
    public string? AvatarUrl { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Size { get; set; } = 32;
    public bool ShowName { get; set; } = true;
    public string Shape { get; set; } = "circle";
}
