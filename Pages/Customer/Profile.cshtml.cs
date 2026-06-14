using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Pages.Customer;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public ProfileModel(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }
    private void InvalidateAvatarCache(string username)
    {
        if (!string.IsNullOrEmpty(username))
            _cache.Remove($"user_avatar_{username}");
    }

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    public NguoiDung? CurrentUser { get; set; }
    public string HoTen => CurrentUser == null ? string.Empty : $"{CurrentUser.Ho} {CurrentUser.Ten}".Trim();

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxAvatarSize = 2 * 1024 * 1024; // 2MB

    public class ProfileInput
    {
        public string Ho { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public DateTime? NgaySinh { get; set; }
        public string? SoDienThoai { get; set; }
        public string? DiaChi { get; set; }
        public string? Email { get; set; }
    }

    private async Task<NguoiDung?> LoadCurrentUserAsync()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.NguoiDung.FirstOrDefaultAsync(u => u.TenDangNhap == username);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentUser = await LoadCurrentUserAsync();
        if (CurrentUser == null) return RedirectToPage("/Account/Login");

        Input = new ProfileInput
        {
            Ho = CurrentUser.Ho,
            Ten = CurrentUser.Ten,
            NgaySinh = CurrentUser.NgaySinh,
            SoDienThoai = CurrentUser.SoDienThoai,
            DiaChi = CurrentUser.DiaChi,
            Email = CurrentUser.Email
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CurrentUser = await LoadCurrentUserAsync();
        if (CurrentUser == null) return RedirectToPage("/Account/Login");

        if (string.IsNullOrWhiteSpace(Input.Ho) || string.IsNullOrWhiteSpace(Input.Ten))
        {
            ModelState.AddModelError(string.Empty, "Họ và tên không được để trống.");
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(Input.Email) && Input.Email != CurrentUser.Email)
        {
            var emailExists = await _context.NguoiDung
                .AnyAsync(u => u.Email == Input.Email.Trim().ToLower() && u.TenDangNhap != CurrentUser.TenDangNhap);
            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "Email này đã được sử dụng bởi tài khoản khác.");
                return Page();
            }
        }

        CurrentUser.Ho = Input.Ho.Trim();
        CurrentUser.Ten = Input.Ten.Trim();
        CurrentUser.NgaySinh = Input.NgaySinh;
        CurrentUser.SoDienThoai = Input.SoDienThoai?.Trim();
        CurrentUser.DiaChi = Input.DiaChi?.Trim();
        CurrentUser.Email = Input.Email?.Trim().ToLower();

        await _context.SaveChangesAsync();

        // Cập nhật claim FullName để hiển thị trên navbar ngay lập tức
        var identity = (System.Security.Claims.ClaimsIdentity)User.Identity!;
        var fullNameClaim = identity.FindFirst("FullName");
        var newFullName = $"{CurrentUser.Ho} {CurrentUser.Ten}".Trim();
        if (fullNameClaim != null)
        {
            identity.RemoveClaim(fullNameClaim);
        }
        identity.AddClaim(new System.Security.Claims.Claim("FullName", newFullName));
        await HttpContext.SignInAsync("Cookies",
            new System.Security.Claims.ClaimsPrincipal(identity),
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = true
            });

        TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
        InvalidateAvatarCache(CurrentUser.TenDangNhap);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadAvatarAsync()
    {
        CurrentUser = await LoadCurrentUserAsync();
        if (CurrentUser == null) return RedirectToPage("/Account/Login");

        if (AvatarFile == null || AvatarFile.Length == 0)
        {
            TempData["AvatarError"] = "Vui lòng chọn một tệp ảnh.";
            return RedirectToPage();
        }

        if (AvatarFile.Length > MaxAvatarSize)
        {
            TempData["AvatarError"] = "Ảnh quá lớn. Kích thước tối đa là 2MB.";
            return RedirectToPage();
        }

        var ext = Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            TempData["AvatarError"] = "Chỉ chấp nhận định dạng ảnh: JPG, PNG, WEBP.";
            return RedirectToPage();
        }

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        // Xóa ảnh cũ nếu là file local trong uploads
        if (!string.IsNullOrEmpty(CurrentUser.AvatarUrl) &&
            CurrentUser.AvatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                CurrentUser.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldPath))
            {
                try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
            }
        }

        var fileName = $"{CurrentUser.TenDangNhap}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await AvatarFile.CopyToAsync(stream);
        }

        CurrentUser.AvatarUrl = $"/uploads/avatars/{fileName}";
        await _context.SaveChangesAsync();
        InvalidateAvatarCache(CurrentUser.TenDangNhap);

        TempData["AvatarSuccess"] = "Cập nhật ảnh đại diện thành công!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAvatarAsync()
    {
        CurrentUser = await LoadCurrentUserAsync();
        if (CurrentUser == null) return RedirectToPage("/Account/Login");

        if (!string.IsNullOrEmpty(CurrentUser.AvatarUrl))
        {
            if (CurrentUser.AvatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    CurrentUser.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
                }
            }
            CurrentUser.AvatarUrl = null;
            await _context.SaveChangesAsync();
            InvalidateAvatarCache(CurrentUser.TenDangNhap);
        }

        TempData["AvatarSuccess"] = "Đã xóa ảnh đại diện.";
        return RedirectToPage();
    }
}
