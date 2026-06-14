using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;
using Microsoft.Extensions.Logging;

namespace ECommerceFinalProject.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(AppDbContext context, IEmailService emailService, ILogger<ForgotPasswordModel> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string Email)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError("", "Vui long nhap email.");
            return Page();
        }

        var email = Email.Trim().ToLower();
        var user = await _context.NguoiDung.FirstOrDefaultAsync(u => u.Email == email);

        // Luon tra ve thong bao chung de tranh lo tai khoan hop le (security best practice)
        // Nhung chi gui mail neu user thuc su ton tai
        if (user == null)
        {
            TempData["InfoMessage"] = "Neu email ton tai trong he thong, ma OTP da duoc gui.";
            return Page();
        }

        // Vo hieu hoa cac OTP cu chua dung
        var oldOtps = await _context.XacThucEmail
            .Where(x => x.TenDangNhap == user.TenDangNhap && !x.DaSuDung)
            .ToListAsync();
        foreach (var old in oldOtps)
            old.DaSuDung = true;

        // Tao OTP moi (5 phut)
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

        // Gui email
        var hoTen = $"{user.Ho} {user.Ten}".Trim();
        var emailSent = true;
        var emailError = "";
        try
        {
            await _emailService.GuiMaXacThucAsync(user.Email ?? email, maOtp, hoTen);
        }
        catch (Exception ex)
        {
            emailSent = false;
            emailError = ex.Message;
            _logger.LogError(ex, "[ForgotPassword] Failed to send OTP email to {Email}", user.Email);
        }

        // Chuyen sang trang ResetPassword kem email + devOtp fallback
        return RedirectToPage("/Account/ResetPassword", new
        {
            email = user.Email,
            devOtp = emailSent ? null : maOtp,
            emailError = emailSent ? null : emailError
        });
    }
}
