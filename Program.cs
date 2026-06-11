using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ECommerceFinalProject.Data;
using ECommerceFinalProject.Models;
using ECommerceFinalProject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Đảm bảo database và các bảng được tạo
    db.Database.EnsureCreated();

    // Kiểm tra database có hợp lệ không (bảng SanPham có tồn tại không?)
    try
    {
        db.Products.Any();
    }
    catch
    {
        Console.WriteLine("⚠️ Database cũ không hợp lệ, đang tạo lại...");
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        Console.WriteLine("✅ Đã tạo lại database thành công!");
    }

    // Seed dữ liệu mẫu nếu database trống
    if (!db.NguoiDung.Any())
    {
        SeedData(db);
    }

    // Auto-update product images to local files
    var localMap = new Dictionary<int, string>
    {
        { 1, "/images/iphone15.jpg" },
        { 2, "/images/macbookair.jpg" },
        { 3, "/images/airpods.jpg" },
        { 4, "/images/galaxy.jpg" },
        { 5, "/images/dellxps.jpg" },
        { 6, "/images/logitech.jpg" }
    };
    var products = db.Products.Where(p => p.Id >= 1 && p.Id <= 6).ToList();
    foreach (var p in products)
    {
        if (localMap.TryGetValue(p.Id, out var url) && p.HinhAnhUrl != url)
        {
            p.HinhAnhUrl = url;
        }
    }
    db.SaveChanges();
}

static void SeedData(AppDbContext db)
{
    Console.WriteLine("📦 Đang thêm dữ liệu mẫu...");

    // 1. Tài khoản Admin
    if (!db.NguoiDung.Any(u => u.TenDangNhap == "Admin"))
    {
        db.NguoiDung.Add(new NguoiDung
        {
            TenDangNhap = "Admin",
            MatKhau = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Ho = "Admin",
            Ten = "System",
            Email = "admin@shop.com",
            VaiTro = "Admin"
        });
    }

    // 2. Danh mục (Categories)
    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new Category { Ten = "Điện thoại", MoTa = "Điện thoại thông minh các hãng" },
            new Category { Ten = "Laptop", MoTa = "Máy tính xách tay" },
            new Category { Ten = "Phụ kiện", MoTa = "Phụ kiện công nghệ" },
            new Category { Ten = "Thời trang", MoTa = "Quần áo, giày dép" },
            new Category { Ten = "Đồ gia dụng", MoTa = "Đồ dùng gia đình" }
        );
        db.SaveChanges();
    }

    // 3. Sản phẩm (Products)
    if (!db.Products.Any())
    {
        var catDienThoai = db.Categories.First(c => c.Ten == "Điện thoại").Id;
        var catLaptop = db.Categories.First(c => c.Ten == "Laptop").Id;
        var catPhuKien = db.Categories.First(c => c.Ten == "Phụ kiện").Id;

        db.Products.AddRange(
            new Product { TenSanPham = "iPhone 15 Pro Max", MoTa = "Điện thoại Apple cao cấp nhất", Gia = 34990000, HinhAnhUrl = "/images/iphone15.jpg", SoLuongTon = 50, DanhMucId = catDienThoai },
            new Product { TenSanPham = "MacBook Air M3", MoTa = "Laptop Apple M3 13 inch", Gia = 28990000, HinhAnhUrl = "/images/macbookair.jpg", SoLuongTon = 30, DanhMucId = catLaptop },
            new Product { TenSanPham = "Tai nghe AirPods Pro 2", MoTa = "Tai nghe không dây chống ồn", Gia = 5990000, HinhAnhUrl = "/images/airpods.jpg", SoLuongTon = 100, DanhMucId = catPhuKien },
            new Product { TenSanPham = "Samsung Galaxy S24 Ultra", MoTa = "Điện thoại Samsung cao cấp", Gia = 27990000, HinhAnhUrl = "/images/galaxy.jpg", SoLuongTon = 40, DanhMucId = catDienThoai },
            new Product { TenSanPham = "Dell XPS 15", MoTa = "Laptop Dell cao cấp", Gia = 32990000, HinhAnhUrl = "/images/dellxps.jpg", SoLuongTon = 20, DanhMucId = catLaptop },
            new Product { TenSanPham = "Chuột Logitech MX Master 3S", MoTa = "Chuột không dây cao cấp", Gia = 1990000, HinhAnhUrl = "/images/logitech.jpg", SoLuongTon = 80, DanhMucId = catPhuKien }
        );
    }

    db.SaveChanges();
    Console.WriteLine("✅ Đã thêm dữ liệu mẫu thành công!");
}

app.Run();
