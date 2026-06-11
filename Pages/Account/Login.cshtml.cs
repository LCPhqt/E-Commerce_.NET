using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AppDbContext _context;

    public LoginModel(AppDbContext context) => _context = context;

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string TaiKhoan, string MatKhau, bool GhiNho)
    {
        if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(MatKhau))
        {
            ModelState.AddModelError("", "Vui lòng nhập tài khoản và mật khẩu.");
            ViewData["TaiKhoan"] = TaiKhoan;
            return Page();
        }

        string taiKhoanInput = TaiKhoan.Trim().ToLower();
        bool isEmail = taiKhoanInput.Contains("@");

        NguoiDung? user = isEmail
            ? await _context.NguoiDung.FirstOrDefaultAsync(u => u.Email == taiKhoanInput)
            : await _context.NguoiDung.FirstOrDefaultAsync(u => u.TenDangNhap == TaiKhoan.Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(MatKhau, user.MatKhau))
        {
            ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
            ViewData["TaiKhoan"] = TaiKhoan;
            return Page();
        }

        var displayName = $"{user.Ho} {user.Ten}".Trim();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.TenDangNhap),
            new Claim(ClaimTypes.Name, user.TenDangNhap),
            new Claim("FullName", string.IsNullOrWhiteSpace(displayName) ? user.TenDangNhap : displayName),
            new Claim(ClaimTypes.Role, user.VaiTro),
            new Claim("Email", user.Email ?? "")
        };

        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            "Cookies",
            principal,
            new AuthenticationProperties
            {
                IsPersistent = GhiNho,
                ExpiresUtc = GhiNho
                    ? System.DateTimeOffset.UtcNow.AddDays(14)
                    : System.DateTimeOffset.UtcNow.AddMinutes(30)
            });

        return RedirectToPage("/Index");
    }
}
