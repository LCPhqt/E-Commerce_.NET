using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;
using Microsoft.Extensions.Logging;

namespace ECommerceFinalProject.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(AppDbContext context, IEmailService emailService, ILogger<ResetPasswordModel> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public string Email { get; set; } = string.Empty;
    public string? DevOtp { get; set; }
    public string? EmailError { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        var email = Request.Query["email"].ToString();
        if (string.IsNullOrEmpty(email))
        {
            email = TempData["ResetEmail"]?.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(email))
            return RedirectToPage("/Account/ForgotPassword");

        Email = email;
        DevOtp = Request.Query["devOtp"].ToString();
        if (string.IsNullOrEmpty(DevOtp)) DevOtp = null;
        EmailError = Request.Query["emailError"].ToString();
        if (string.IsNullOrEmpty(EmailError)) EmailError = null;

        TempData["ResetEmail"] = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string Email, string MaXacThuc, string MatKhauMoi, string XacNhanMatKhau)
    {
        this.Email = Email ?? "";

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(MaXacThuc))
        {
            ModelState.AddModelError("", "Vui long nhap day du email va ma OTP.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(MatKhauMoi) || MatKhauMoi != XacNhanMatKhau)
        {
            ModelState.AddModelError("", "Mat khau moi va xac nhan mat khau khong khop.");
            return Page();
        }

        if (MatKhauMoi.Length < 6)
        {
            ModelState.AddModelError("", "Mat khau phai co it nhat 6 ky tu.");
            return Page();
        }

        if (MatKhauMoi.Length > 8)
        {
            ModelState.AddModelError("", "Mat khau khong duoc qua 8 ky tu.");
            return Page();
        }

        var email = Email.Trim().ToLower();
        var user = await _context.NguoiDung.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            ModelState.AddModelError("", "Email khong ton tai trong he thong.");
            return Page();
        }

        // Kiem tra OTP: chua su dung, chua het han, va khop
        var otp = await _context.XacThucEmail
            .Where(x => x.TenDangNhap == user.TenDangNhap
                        && x.MaXacThuc == MaXacThuc.Trim()
                        && !x.DaSuDung
                        && x.ThoiGianHetHan > DateTime.Now)
            .OrderByDescending(x => x.ThoiGianGui)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            ModelState.AddModelError("", "Ma OTP khong dung hoac da het han.");
            return Page();
        }

        // Cap nhat mat khau moi (BCrypt hash)
        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(MatKhauMoi);
        otp.DaSuDung = true; // danh dau OTP da su dung
        await _context.SaveChangesAsync();

        // Vo hieu hoa cac OTP con lai cua user (an toan)
        var otherOtps = await _context.XacThucEmail
            .Where(x => x.TenDangNhap == user.TenDangNhap && !x.DaSuDung)
            .ToListAsync();
        foreach (var o in otherOtps)
            o.DaSuDung = true;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Dat lai mat khau thanh cong. Vui long dang nhap.";
        return RedirectToPage("/Account/Login");
    }

    public async Task<IActionResult> OnGetResendAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new JsonResult(new { success = false, message = "Email khong hop le." });

        var e = email.Trim().ToLower();
        var user = await _context.NguoiDung.FirstOrDefaultAsync(u => u.Email == e);
        if (user == null)
            return new JsonResult(new { success = false, message = "Email khong ton tai." });

        var oldOtps = await _context.XacThucEmail
            .Where(x => x.TenDangNhap == user.TenDangNhap && !x.DaSuDung)
            .ToListAsync();
        foreach (var o in oldOtps) o.DaSuDung = true;

        var maOtp = new Random().Next(100000, 999999).ToString();
        _context.XacThucEmail.Add(new XacThucEmail
        {
            TenDangNhap = user.TenDangNhap,
            MaXacThuc = maOtp,
            ThoiGianGui = DateTime.Now,
            ThoiGianHetHan = DateTime.Now.AddMinutes(5),
            DaSuDung = false
        });
        await _context.SaveChangesAsync();

        var hoTen = $"{user.Ho} {user.Ten}".Trim();
        try
        {
            await _emailService.GuiMaXacThucAsync(user.Email ?? e, maOtp, hoTen);
            return new JsonResult(new { success = true, message = "Da gui lai ma OTP. Vui long kiem tra email." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResetPassword-Resend] Failed to send OTP to {Email}", user.Email);
            // Tra ve OTP de dev fallback
            return new JsonResult(new { success = true, message = $"SMTP loi: {ex.Message}", devOtp = maOtp });
        }
    }
}
