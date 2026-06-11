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
        db.ChangeTracker.Clear();
        Console.WriteLine("✅ Đã tạo lại database thành công!");
    }

    // Kiểm tra lỗi encoding tiếng Việt (ví dụ: "Đ" hiển thị thành "Ä")
    var encodingError = false;
    try
    {
        var sampleCat = db.Categories.FirstOrDefault();
        if (sampleCat != null && sampleCat.Ten.Length > 0 && sampleCat.Ten[0] == 'Ä')
        {
            encodingError = true;
            Console.WriteLine("⚠️ Phát hiện lỗi encoding dữ liệu (tiếng Việt bị hỏng), đang tạo lại database...");
        }
    }
    catch
    {
        // Nếu truy vấn lỗi, EnsureDeleted sẽ xử lý
        encodingError = true;
    }

    if (encodingError)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        db.ChangeTracker.Clear();
        Console.WriteLine("✅ Đã tạo lại database với encoding UTF-8 chính xác!");
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
        { 6, "/images/logitech.jpg" },
        { 7, "/images/pixel9.jpg" },
        { 8, "/images/asusrog.jpg" },
        { 9, "/images/powerbank.jpg" },
        { 10, "/images/keyboard.jpg" },
        { 11, "/images/speaker.jpg" },
        { 12, "/images/tshirt.jpg" },
        { 13, "/images/sneakers.jpg" },
        { 14, "/images/smartwatch.jpg" },
        { 15, "/images/backpack.jpg" },
        { 16, "/images/airfryer.jpg" },
        { 17, "/images/blender.jpg" },
        { 18, "/images/fan.jpg" },
        { 19, "/images/filter.jpg" }
    };
    var products = db.Products.Where(p => p.Id >= 1 && p.Id <= 19).ToList();
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
        var catThoiTrang = db.Categories.First(c => c.Ten == "Thời trang").Id;
        var catDoGiaDung = db.Categories.First(c => c.Ten == "Đồ gia dụng").Id;

        db.Products.AddRange(
            // Điện thoại (cats 1,4)
            new Product { TenSanPham = "iPhone 15 Pro Max", MoTa = "Điện thoại Apple cao cấp nhất", Gia = 34990000, HinhAnhUrl = "/images/iphone15.jpg", SoLuongTon = 50, DanhMucId = catDienThoai },
            new Product { TenSanPham = "MacBook Air M3", MoTa = "Laptop Apple M3 13 inch", Gia = 28990000, HinhAnhUrl = "/images/macbookair.jpg", SoLuongTon = 30, DanhMucId = catLaptop },
            new Product { TenSanPham = "Tai nghe AirPods Pro 2", MoTa = "Tai nghe không dây chống ồn", Gia = 5990000, HinhAnhUrl = "/images/airpods.jpg", SoLuongTon = 100, DanhMucId = catPhuKien },
            new Product { TenSanPham = "Samsung Galaxy S24 Ultra", MoTa = "Điện thoại Samsung cao cấp", Gia = 27990000, HinhAnhUrl = "/images/galaxy.jpg", SoLuongTon = 40, DanhMucId = catDienThoai },
            new Product { TenSanPham = "Dell XPS 15", MoTa = "Laptop Dell cao cấp", Gia = 32990000, HinhAnhUrl = "/images/dellxps.jpg", SoLuongTon = 20, DanhMucId = catLaptop },
            new Product { TenSanPham = "Chuột Logitech MX Master 3S", MoTa = "Chuột không dây cao cấp", Gia = 1990000, HinhAnhUrl = "/images/logitech.jpg", SoLuongTon = 80, DanhMucId = catPhuKien },
            // Điện thoại mới
            new Product { TenSanPham = "Google Pixel 9 Pro", MoTa = "Điện thoại Google với AI thông minh, camera 50MP", Gia = 21990000, HinhAnhUrl = "/images/pixel9.jpg", SoLuongTon = 35, DanhMucId = catDienThoai },
            // Laptop mới
            new Product { TenSanPham = "ASUS ROG Zephyrus G14", MoTa = "Laptop gaming AMD Ryzen 9, RTX 4060, 14\" 2K", Gia = 35990000, HinhAnhUrl = "/images/asusrog.jpg", SoLuongTon = 15, DanhMucId = catLaptop },
            // Phụ kiện mới
            new Product { TenSanPham = "Sạc dự phòng 20000mAh", MoTa = "Pin sạc dự phòng dung lượng cao, hỗ trợ sạc nhanh 65W", Gia = 890000, HinhAnhUrl = "/images/powerbank.jpg", SoLuongTon = 200, DanhMucId = catPhuKien },
            new Product { TenSanPham = "Bàn phím cơ Mechanical", MoTa = "Bàn phím cơ RGB switch xanh, dây kéo bọc dù", Gia = 1590000, HinhAnhUrl = "/images/keyboard.jpg", SoLuongTon = 60, DanhMucId = catPhuKien },
            new Product { TenSanPham = "Loa Bluetooth JBL Flip 6", MoTa = "Loa di động chống nước, công suất 30W", Gia = 2990000, HinhAnhUrl = "/images/speaker.jpg", SoLuongTon = 45, DanhMucId = catPhuKien },
            // Thời trang (danh mục mới)
            new Product { TenSanPham = "Áo thun nam Basic", MoTa = "Áo thun cotton 100% cao cấp, nhiều màu sắc", Gia = 299000, HinhAnhUrl = "/images/tshirt.jpg", SoLuongTon = 500, DanhMucId = catThoiTrang },
            new Product { TenSanPham = "Giày thể thao Nike Air", MoTa = "Giày chạy bộ Nike Air đế êm, siêu nhẹ", Gia = 3290000, HinhAnhUrl = "/images/sneakers.jpg", SoLuongTon = 120, DanhMucId = catThoiTrang },
            new Product { TenSanPham = "Đồng hồ Apple Watch SE", MoTa = "Đồng hồ thông minh, đo nhịp tim, GPS", Gia = 7990000, HinhAnhUrl = "/images/smartwatch.jpg", SoLuongTon = 70, DanhMucId = catThoiTrang },
            new Product { TenSanPham = "Balo thời trang chống nước", MoTa = "Balo chống nước 40L, nhiều ngăn tiện lợi", Gia = 690000, HinhAnhUrl = "/images/backpack.jpg", SoLuongTon = 150, DanhMucId = catThoiTrang },
            // Đồ gia dụng (danh mục mới)
            new Product { TenSanPham = "Nồi chiên không dầu Philips", MoTa = "Nồi chiên không dầu 6.5L, không khói, ít dầu mỡ", Gia = 3590000, HinhAnhUrl = "/images/airfryer.jpg", SoLuongTon = 40, DanhMucId = catDoGiaDung },
            new Product { TenSanPham = "Máy xay sinh tố đa năng", MoTa = "Máy xay sinh tố 6 lưỡi, 3 tốc độ, cối thủy tinh", Gia = 1290000, HinhAnhUrl = "/images/blender.jpg", SoLuongTon = 90, DanhMucId = catDoGiaDung },
            new Product { TenSanPham = "Quạt điện cây Panasonic", MoTa = "Quạt đứng Panasonic 3 cánh, 4 tốc độ, êm ái", Gia = 1590000, HinhAnhUrl = "/images/fan.jpg", SoLuongTon = 65, DanhMucId = catDoGiaDung },
            new Product { TenSanPham = "Máy lọc nước RO Kangaroo", MoTa = "Máy lọc nước RO 9 lõi, nước nóng nguội", Gia = 6990000, HinhAnhUrl = "/images/filter.jpg", SoLuongTon = 25, DanhMucId = catDoGiaDung }
        );
    }

    db.SaveChanges();
    Console.WriteLine("✅ Đã thêm dữ liệu mẫu thành công!");
}

app.Run();
