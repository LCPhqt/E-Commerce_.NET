using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Account;

public class VerifyOTPModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public VerifyOTPModel(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public string Email { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        var email = Request.Query["email"].ToString();
        var hoTen = Request.Query["hoTen"].ToString();
        var pendingBase64 = Request.Query["pending"].ToString();

        if (string.IsNullOrEmpty(email))
        {
            email = TempData["Email"]?.ToString() ?? string.Empty;
            hoTen = TempData["HoTen"]?.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(email))
            return RedirectToPage("/Account/Login");

        Email = email;
        HoTen = hoTen;
        TempData["Email"] = email;
        TempData["HoTen"] = hoTen;

        // Decode pending user from query string (survives redirect)
        string tenDangNhap = string.Empty;
        if (!string.IsNullOrEmpty(pendingBase64))
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(pendingBase64));
                var pendingUser = JsonSerializer.Deserialize<NguoiDung>(json);
                tenDangNhap = pendingUser?.TenDangNhap ?? string.Empty;
                TempData["PendingRegister"] = json;
            }
            catch { }
        }
        if (string.IsNullOrEmpty(tenDangNhap))
        {
            try
            {
                var pendingUser = JsonSerializer.Deserialize<NguoiDung>(TempData["PendingRegister"]?.ToString() ?? "{}");
                tenDangNhap = pendingUser?.TenDangNhap ?? string.Empty;
            }
            catch { }
        }

        int secondsLeft = 300;
        if (!string.IsNullOrEmpty(tenDangNhap))
        {
            var existing = _context.XacThucEmail
                .Where(x => x.TenDangNhap == tenDangNhap && !x.DaSuDung)
                .OrderByDescending(x => x.ThoiGianGui)
                .FirstOrDefault();

            if (existing != null)
            {
                secondsLeft = Math.Max(0, (int)(existing.ThoiGianHetHan - DateTime.Now).TotalSeconds);
            }
        }

        ViewData["SecondsLeft"] = secondsLeft;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string MaXacThuc, string EmailHidden)
    {
        // Lay email tu hidden field hoac query
        var email = !string.IsNullOrEmpty(EmailHidden)
            ? EmailHidden
            : Request.Query["email"].ToString();

        if (string.IsNullOrEmpty(email))
            email = TempData["Email"]?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(email))
            return RedirectToPage("/Account/Login");

        ViewData["Email"] = email;
        ViewData["SecondsLeft"] = 300;

        if (string.IsNullOrWhiteSpace(MaXacThuc) || MaXacThuc.Length != 6)
        {
            ViewData["Error"] = "Vui long nhap day du 6 chu so.";
            return Page();
        }

        string? pendingLogin = TempData["PendingLogin"]?.ToString();
        string? pendingRegisterJson = TempData["PendingRegister"]?.ToString();

        // Thu xu ly dang nhap
        if (!string.IsNullOrEmpty(pendingLogin))
        {
            return await XuLyDangNhapAsync(pendingLogin, email, MaXacThuc);
        }

        // Thu xu ly dang ky
        if (!string.IsNullOrEmpty(pendingRegisterJson))
        {
            return await XuLyDangKyAsync(pendingRegisterJson, email, MaXacThuc);
        }

        ViewData["Error"] = "Phien dang nhap het han. Vui long dang nhap lai.";
        return Page();
    }

    private async Task<IActionResult> XuLyDangNhapAsync(string tenDangNhap, string email, string maXacThuc)
    {
        var otp = await _context.XacThucEmail
            .Where(x => x.TenDangNhap == tenDangNhap
                     && x.MaXacThuc == maXacThuc
                     && !x.DaSuDung
                     && x.ThoiGianHetHan > DateTime.Now)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            ViewData["Error"] = "Ma xac thuc khong dung hoac da het han.";
            return Page();
        }

        otp.DaSuDung = true;
        await _context.SaveChangesAsync();

        var user = await _context.NguoiDung.FindAsync(tenDangNhap);
        if (user == null)
            return RedirectToPage("/Account/Login");

        await SignInUserAsync(user, TempData["GhiNho"]?.ToString() == "true");

        // Xoa TempData
        TempData.Remove("Email");
        TempData.Remove("HoTen");
        TempData.Remove("PendingLogin");
        TempData.Remove("GhiNho");

        return user.VaiTro == "Admin"
            ? RedirectToPage("/Admin/Dashboard")
            : RedirectToPage("/Customer/Home");
    }

    private async Task<IActionResult> XuLyDangKyAsync(string pendingRegisterJson, string email, string maXacThuc)
    {
        NguoiDung? pendingUser;
        try
        {
            pendingUser = JsonSerializer.Deserialize<NguoiDung>(pendingRegisterJson);
        }
        catch
        {
            ViewData["Error"] = "Du lieu dang ky khong hop le.";
            return Page();
        }

        if (pendingUser == null)
        {
            ViewData["Error"] = "Du lieu dang ky khong hop le.";
            return Page();
        }

        var otp = await _context.XacThucEmail
            .Where(x => x.TenDangNhap == pendingUser.TenDangNhap
                     && x.MaXacThuc == maXacThuc
                     && !x.DaSuDung
                     && x.ThoiGianHetHan > DateTime.Now)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            ViewData["Error"] = "Ma xac thuc khong dung hoac da het han.";
            return Page();
        }

        otp.DaSuDung = true;
        pendingUser.DaXacThuc = true;
        _context.NguoiDung.Add(pendingUser);
        await _context.SaveChangesAsync();

        await SignInUserAsync(pendingUser, false);

        TempData.Remove("Email");
        TempData.Remove("HoTen");
        TempData.Remove("PendingRegister");

        return pendingUser.VaiTro == "Admin"
            ? RedirectToPage("/Admin/Dashboard")
            : RedirectToPage("/Customer/Home");
    }

    private async Task SignInUserAsync(NguoiDung user, bool ghiNho)
    {
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
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = ghiNho,
                ExpiresUtc = ghiNho
                    ? System.DateTimeOffset.UtcNow.AddDays(14)
                    : System.DateTimeOffset.UtcNow.AddMinutes(30)
            });
    }

    public async Task<IActionResult> OnGetResend(string email)
    {
        if (string.IsNullOrEmpty(email))
            return new JsonResult(new { success = false, message = "Email khong hop le." });

        string? tenDangNhap = TempData["PendingLogin"]?.ToString();
        NguoiDung? user;

        if (!string.IsNullOrEmpty(tenDangNhap))
        {
            user = await _context.NguoiDung.FindAsync(tenDangNhap);
        }
        else
        {
            try
            {
                user = JsonSerializer.Deserialize<NguoiDung>(TempData["PendingRegister"]?.ToString() ?? "{}");
                tenDangNhap = user?.TenDangNhap;
            }
            catch
            {
                return new JsonResult(new { success = false, message = "Du lieu khong hop le." });
            }
        }

        if (user == null || string.IsNullOrEmpty(tenDangNhap))
            return new JsonResult(new { success = false, message = "Phien het han." });

        // Vo hieu hoa OTP cu
        var oldOtps = await _context.XacThucEmail
            .Where(x => x.TenDangNhap == tenDangNhap && !x.DaSuDung)
            .ToListAsync();
        foreach (var old in oldOtps) old.DaSuDung = true;

        // Tao OTP moi
        var newOtp = new Random().Next(100000, 999999).ToString();
        _context.XacThucEmail.Add(new XacThucEmail
        {
            TenDangNhap = tenDangNhap,
            MaXacThuc = newOtp,
            ThoiGianGui = DateTime.Now,
            ThoiGianHetHan = DateTime.Now.AddMinutes(5),
            DaSuDung = false
        });
        await _context.SaveChangesAsync();

        var hoTen = $"{user.Ho} {user.Ten}".Trim();
        try { await _emailService.GuiMaXacThucAsync(user.Email ?? email, newOtp, hoTen); }
        catch { /* Email loi, van tra ve thanh cong vi da tao OTP trong DB */ }

        return new JsonResult(new { success = true });
    }
}
