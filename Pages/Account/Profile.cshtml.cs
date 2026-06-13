using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

namespace ECommerceFinalProject.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IOrderService _orderService;

    public NguoiDung? UserInfo { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public int PendingOrders { get; set; }

    public ProfileModel(AppDbContext context, IOrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    public async Task OnGetAsync()
    {
        var username = User.Identity!.Name!;
        UserInfo = await _context.NguoiDung.FirstOrDefaultAsync(u => u.TenDangNhap == username);

        var orders = await _orderService.GetUserOrdersAsync(username);
        TotalOrders = orders.Count;
        TotalSpent = orders.Where(o => o.TrangThai == "Đã giao").Sum(o => o.TongTien);
        PendingOrders = orders.Count(o => o.TrangThai == "Chờ xử lý");
    }

    public async Task<IActionResult> OnPostUpdateInfoAsync(
        string? ho, string? ten, string? email, string? soDienThoai, DateTime? ngaySinh)
    {
        var username = User.Identity!.Name!;
        var user = await _context.NguoiDung.FirstOrDefaultAsync(u => u.TenDangNhap == username);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
            return RedirectToPage();
        }

        if (!string.IsNullOrWhiteSpace(ho)) user.Ho = ho.Trim();
        if (!string.IsNullOrWhiteSpace(ten)) user.Ten = ten.Trim();
        if (!string.IsNullOrWhiteSpace(email)) user.Email = email.Trim();
        if (!string.IsNullOrWhiteSpace(soDienThoai)) user.SoDienThoai = soDienThoai.Trim();
        if (ngaySinh.HasValue) user.NgaySinh = ngaySinh.Value;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
        return RedirectToPage();
    }
}
