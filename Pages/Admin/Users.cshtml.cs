using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly AppDbContext _context;

    public List<NguoiDung> Users { get; set; } = new();

    public UsersModel(AppDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        Users = await _context.NguoiDung.OrderBy(u => u.TenDangNhap).ToListAsync();
    }
}
