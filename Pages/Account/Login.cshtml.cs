using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public LoginModel(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectAfterLogin(User);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string TaiKhoan, string MatKhau, bool GhiNho)
    {
        if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(MatKhau))
        {
            ModelState.AddModelError("", "Vui long nhap tai khoan va mat khau.");
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
            ModelState.AddModelError("", "Tai khoan hoac mat khau khong dung.");
            ViewData["TaiKhoan"] = TaiKhoan;
            return Page();
        }

        // Luu thong tin tam de xac thuc OTP
        TempData["PendingLogin"] = user.TenDangNhap;
        TempData["Email"] = user.Email;
        TempData["GhiNho"] = GhiNho.ToString().ToLower();

        // Vo hieu hoa cac OTP cu
        var oldOtps = await _context.XacThucEmail
            .Where(x => x.TenDangNhap == user.TenDangNhap && !x.DaSuDung)
            .ToListAsync();
        foreach (var old in oldOtps)
            old.DaSuDung = true;

        // Tao OTP moi
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

        // Gui email OTP — neu loi van cho redirect, truyen devOtp qua query
        var hoTen = $"{user.Ho} {user.Ten}".Trim();
        var emailSent = true;
        var emailError = "";
        try
        {
            await _emailService.GuiMaXacThucAsync(user.Email ?? "", maOtp, hoTen);
        }
        catch (Exception ex)
        {
            emailSent = false;
            emailError = ex.Message;
            // Van cho phep xac thuc bang OTP trong database
        }

        return RedirectToPage("/Account/VerifyOTP", new
        {
            email = user.Email,
            devOtp = emailSent ? null : maOtp,
            emailError = emailSent ? null : emailError
        });
    }

    private IActionResult RedirectAfterLogin(ClaimsPrincipal principal) =>
        RedirectAfterLogin(principal.IsInRole("Admin") ? "Admin" : "User");

    private IActionResult RedirectAfterLogin(string vaiTro) =>
        vaiTro == "Admin"
            ? RedirectToPage("/Admin/Dashboard")
            : RedirectToPage("/Customer/Home");
}
