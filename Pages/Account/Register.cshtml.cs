using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _context;

    public RegisterModel(AppDbContext context) => _context = context;

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
            ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin bắt buộc.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        if (MatKhau != XacNhanMatKhau)
        {
            ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        if (MatKhau.Length < 6)
        {
            ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        bool exists = await _context.NguoiDung
            .AnyAsync(u => u.TenDangNhap == TenDangNhap || u.Email == Email);

        if (exists)
        {
            ModelState.AddModelError("", "Tên đăng nhập hoặc Email đã được sử dụng.");
            return SaveFormData(TenDangNhap, Ho, Ten, NgaySinh, SoDienThoai, Email);
        }

        var user = new NguoiDung
        {
            TenDangNhap = TenDangNhap.Trim(),
            Ho = Ho.Trim(),
            Ten = Ten.Trim(),
            NgaySinh = NgaySinh,
            SoDienThoai = SoDienThoai?.Trim(),
            Email = Email.Trim().ToLower(),
            MatKhau = BCrypt.Net.BCrypt.HashPassword(MatKhau)
        };

        _context.NguoiDung.Add(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToPage("/Account/Login");
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
