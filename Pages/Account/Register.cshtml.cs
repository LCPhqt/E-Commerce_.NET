using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;
using Microsoft.Extensions.Logging;

namespace ECommerceFinalProject.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(AppDbContext context, IEmailService emailService, ILogger<RegisterModel> logger)
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

    public async Task<IActionResult> OnPostAsync(
        string TenDangNhap,
        string Ho,
        string Ten,
        DateTime? NgaySinh,
        string SoDienThoai,
        string Email,
        string MatKhau,
        string XacNhanMatKhau)
    {
        if (string.IsNullOrWhiteSpace(TenDangNhap) || string.IsNullOrWhiteSpace(Ho) ||
            string.IsNullOrWhiteSpace(Ten) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(MatKhau))
        {
            ModelState.AddModelError("", "Vui long dien day du thong tin bat buoc.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        if (MatKhau != XacNhanMatKhau)
        {
            ModelState.AddModelError("", "Mat khau xac nhan khong khop.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        if (MatKhau.Length < 6)
        {
            ModelState.AddModelError("", "Mat khau phai co it nhat 6 ky tu.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        if (MatKhau.Length > 8)
        {
            ModelState.AddModelError("", "Mat khau khong duoc qua 8 ky tu.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        bool exists = await _context.NguoiDung
            .AnyAsync(u => u.TenDangNhap == TenDangNhap || u.Email == Email.ToLower().Trim());

        if (exists)
        {
            ModelState.AddModelError("", "Ten dang nhap hoac Email da duoc su dung.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        // Luu user tam thoi vao TempData de xac thuc
        var pendingUser = new NguoiDung
        {
            TenDangNhap = TenDangNhap.Trim(),
            Ho = Ho.Trim(),
            Ten = Ten.Trim(),
            NgaySinh = NgaySinh,
            SoDienThoai = SoDienThoai?.Trim(),
            Email = Email.Trim().ToLower(),
            MatKhau = BCrypt.Net.BCrypt.HashPassword(MatKhau)
        };

        TempData["PendingRegister"] = System.Text.Json.JsonSerializer.Serialize(pendingUser);
        TempData["Email"] = pendingUser.Email;

        // Tao OTP
        var maOtp = new Random().Next(100000, 999999).ToString();
        _context.XacThucEmail.Add(new XacThucEmail
        {
            TenDangNhap = pendingUser.TenDangNhap,
            MaXacThuc = maOtp,
            ThoiGianGui = DateTime.Now,
            ThoiGianHetHan = DateTime.Now.AddMinutes(5),
            DaSuDung = false
        });
        await _context.SaveChangesAsync();

        // Gui email — neu loi SMTP thi van cho redirect, nhung danh dau loi
        var hoTen = $"{pendingUser.Ho} {pendingUser.Ten}".Trim();
        TempData["HoTen"] = hoTen;
        var emailSent = true;
        var emailError = "";
        try
        {
            await _emailService.GuiMaXacThucAsync(pendingUser.Email ?? Email, maOtp, hoTen);
        }
        catch (Exception ex)
        {
            emailSent = false;
            emailError = ex.Message;
            _logger.LogError(ex, "[Register] Failed to send OTP email to {Email}", pendingUser.Email);
            TempData["EmailError"] = ex.Message;
            // Van cho phep redirect de user xac thuc bang OTP trong database
        }
        TempData["EmailSent"] = emailSent;

        // Encode pending user data so it survives the redirect
        var pendingBase64 = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(pendingUser)));

        return RedirectToPage("/Account/VerifyOTP", new
        {
            email = pendingUser.Email,
            hoTen,
            pending = pendingBase64,
            // Neu email gui loi, truyen OTP qua query de user van xac thuc duoc (dev fallback)
            devOtp = emailSent ? null : maOtp,
            emailError = emailSent ? null : emailError
        });
    }

    private IActionResult SaveFormData(string tenDangNhap, string ho, string ten, DateTime? ngaySinh, string sdt, string email)
    {
        ViewData["TenDangNhap"] = tenDangNhap;
        ViewData["Ho"] = ho;
        ViewData["Ten"] = ten;
        ViewData["NgaySinh"] = ngaySinh?.ToString("yyyy-MM-dd");
        ViewData["SoDienThoai"] = sdt;
        ViewData["Email"] = email;
        return Page();
    }
}
