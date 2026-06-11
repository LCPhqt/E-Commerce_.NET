---
date: 2026-06-11T10:51:19+0700
author: Bommner69
commit: 2506ea7
branch: main
repository: ck_Net
topic: "E-Commerce Platform - Complete Backend & Frontend"
tags: [design, ecommerce, razor-pages, ef-core]
status: completed
parent: "Tài liệu không có tiêu đề (3).docx"
last_updated: 2026-06-11T10:51:19+0700
last_updated_by: Bommner69
---

# Design: E-Commerce Platform — Complete Backend & Frontend

## Summary

Xây dựng hoàn chỉnh nền tảng E-Commerce ASP.NET Core Razor Pages với EF Core + SQL Server. Dự án bao gồm: hệ thống sản phẩm/danh mục, giỏ hàng/đặt hàng, trang quản trị Admin đầy đủ CRUD và thống kê. Kiến trúc theo pattern Service Layer với hai khu vực giao diện riêng biệt (Customer và Admin), sử dụng Cookie Authentication với phân quyền Role (Admin/User).

## Requirements

- [x] Hiển thị danh sách sản phẩm, tìm kiếm, lọc theo danh mục
- [x] Xem chi tiết sản phẩm với ảnh và mô tả
- [x] Giỏ hàng: thêm/xóa/sửa số lượng, tính tổng tiền
- [x] Checkout tạo đơn hàng
- [x] Lịch sử đơn hàng cho khách hàng
- [x] Admin Dashboard với thống kê
- [x] Admin CRUD sản phẩm và danh mục
- [x] Admin quản lý đơn hàng (cập nhật trạng thái)
- [x] Admin quản lý người dùng
- [x] Admin thống kê doanh thu cơ bản
- [x] Phân quyền Admin/User, chặn User vào trang Admin

## Current State Analysis

### Hiện trạng
- Project ASP.NET Core Razor Pages (.NET 10) đã được cấu hình
- AppDbContext chỉ có DbSet<NguoiDung>
- Model NguoiDung (người dùng) — chưa có Role (VaiTro)
- Auth system hoàn chỉnh: Login, Register, Logout với BCrypt + Cookie Authentication
- Layout cơ bản _Layout.cshtml với Bootstrap 5
- Trang chủ Index.cshtml đơn giản

### Key Discoveries
- `Data/AppDbContext.cs`: DbContext hiện chỉ có NguoiDung entity, cần thêm các DbSet mới
- `Models/NguoiDung.cs`: Thiếu cột Role để phân quyền Admin/User
- `Program.cs`: Đã có cấu hình Cookie Auth, cần thêm Authorization policy và DI registration
- `Pages/Account/Login.cshtml.cs`: Auth dùng ClaimsPrincipal với ClaimTypes.Name — pattern để follow
- `Pages/Shared/_Layout.cshtml`: Dùng Bootstrap 5 — pattern để follow cho các layout khác

### Constraints
- Phải giữ nguyên cấu trúc Project có sẵn (Pages, Data, Models)
- NguoiDung dùng TenDangNhap làm PK (string) — FK từ Order và Cart
- Mật khẩu đã hash bằng BCrypt — giữ nguyên pattern
- CSDL: SQL Server với connection string trong appsettings.json

## Scope

### Building
- Models: Category, Product, Cart, CartItem, Order, OrderDetail + Role trong NguoiDung
- Services: IProductService/ProductService, ICartService/CartService, IOrderService/OrderService
- Customer Layout + Product pages (Home, Products với tìm kiếm/lọc, ProductDetails)
- Customer Cart, Checkout, Order History
- Admin Layout + Dashboard, CRUD Products, CRUD Categories, Orders management, Users management, Reports
- Program.cs: DI registration, Admin authorization policy, layout navigation

### Not Building
- Thanh toán online (PayPal, VNPay, etc.) — chỉ tạo đơn hàng, trạng thái "Chờ xử lý"
- Email notification (xác nhận đơn hàng qua email)
- Review/Rating sản phẩm
- Wishlist/Favorites
- Guest cart (không đăng nhập) — giỏ hàng chỉ dành cho user đã đăng nhập
- Multi-language support
- File upload (ảnh dùng URL string)
- Real-time notifications (SignalR)
- Export báo cáo PDF/Excel

## Decisions

### Decision 1: Phân quyền bằng Role string trong NguoiDung
- **Ambiguity**: Dùng Role column hay bảng Roles riêng?
- **Explored**: 
  - Option A (Role column): Đơn giản, thêm string VaiTro vào NguoiDung. Phù hợp đồ án cuối kỳ.
  - Option B (Roles table): Chuẩn hơn, hỗ trợ nhiều role. Phức tạp hơn.
- **Decision**: Option A — Thêm cột VaiTro (string, mặc định "User") vào NguoiDung. Đơn giản, đủ cho Admin/User.

### Decision 2: Cấu trúc Admin — Pages/Admin/
- **Decision**: Dùng Pages/Admin/ thay vì Areas. Đơn giản, đồng bộ với cấu trúc Pages hiện tại.

### Decision 3: Cart lưu trong Database
- **Decision**: Cart và CartItems lưu trong DB với quan hệ tới NguoiDung (TenDangNhap). Persistent, phù hợp cho user đã đăng nhập.

### Decision 4: Ảnh sản phẩm dùng URL string
- **Decision**: Product.ImageUrl là string, lưu URL. Không xử lý upload file.

### Decision 5: Layout riêng cho Customer và Admin
- **Decision**: _CustomerLayout.cshtml cho trang người dùng, _AdminLayout.cshtml cho trang quản trị. Dùng _ViewStart.cshtml trong mỗi folder con để chỉ định layout.

## Architecture

### Models/

#### Models/Category.cs — NEW
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("DanhMuc")]
public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Tên danh mục")]
    public string Ten { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    // Navigation
    public ICollection<Product>? Products { get; set; }
}
```

#### Models/Product.cs — NEW
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("SanPham")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Tên sản phẩm")]
    public string TenSanPham { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Giá")]
    public decimal Gia { get; set; }

    [MaxLength(500)]
    [Display(Name = "Hình ảnh URL")]
    public string? HinhAnhUrl { get; set; }

    [Required]
    [Display(Name = "Số lượng tồn")]
    public int SoLuongTon { get; set; } = 0;

    [Required]
    [Display(Name = "Danh mục")]
    public int DanhMucId { get; set; }

    // Navigation
    [ForeignKey(nameof(DanhMucId))]
    public Category? DanhMuc { get; set; }
}
```

#### Models/Cart.cs — NEW
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("GioHang")]
public class Cart
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(TenDangNhap))]
    public NguoiDung? NguoiDung { get; set; }

    public ICollection<CartItem>? CartItems { get; set; }
}
```

#### Models/CartItem.cs — NEW
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("ChiTietGioHang")]
public class CartItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int GioHangId { get; set; }

    [Required]
    public int SanPhamId { get; set; }

    [Required]
    public int SoLuong { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGia { get; set; }

    // Navigation
    [ForeignKey(nameof(GioHangId))]
    public Cart? GioHang { get; set; }

    [ForeignKey(nameof(SanPhamId))]
    public Product? SanPham { get; set; }
}
```

#### Models/Order.cs — NEW
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("DonHang")]
public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    public DateTime NgayDat { get; set; } = DateTime.Now;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TongTien { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = "Chờ xử lý";

    [Required]
    [MaxLength(500)]
    [Display(Name = "Địa chỉ giao hàng")]
    public string DiaChiGiao { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    // Navigation
    [ForeignKey(nameof(TenDangNhap))]
    public NguoiDung? NguoiDung { get; set; }

    public ICollection<OrderDetail>? OrderDetails { get; set; }
}
```

#### Models/OrderDetail.cs — NEW
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("ChiTietDonHang")]
public class OrderDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DonHangId { get; set; }

    [Required]
    public int SanPhamId { get; set; }

    [Required]
    [MaxLength(200)]
    public string TenSanPham { get; set; } = string.Empty;

    [Required]
    public int SoLuong { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGia { get; set; }

    // Navigation
    [ForeignKey(nameof(DonHangId))]
    public Order? DonHang { get; set; }

    [ForeignKey(nameof(SanPhamId))]
    public Product? SanPham { get; set; }
}
```

#### Models/NguoiDung.cs — MODIFY

**Thêm property VaiTro**:
```csharp
    [MaxLength(20)]
    [Display(Name = "Vai trò")]
    public string VaiTro { get; set; } = "User";
```

**Vị trí**: Thêm sau property `Email` và trước closing brace cuối cùng của class.

### Data/

#### Data/AppDbContext.cs — MODIFY

**Full file sau khi cập nhật**:
```csharp
using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<NguoiDung> NguoiDung { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // NguoiDung
        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.TenDangNhap);
            entity.Property(e => e.TenDangNhap).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MatKhau).IsRequired();
            entity.Property(e => e.Ho).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Ten).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NgaySinh).HasColumnType("date");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.VaiTro).HasMaxLength(20).HasDefaultValue("User");

            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ten).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MoTa).HasMaxLength(500);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenSanPham).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MoTa).HasMaxLength(2000);
            entity.Property(e => e.Gia).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.HinhAnhUrl).HasMaxLength(500);
            entity.Property(e => e.SoLuongTon).IsRequired();

            entity.HasOne(e => e.DanhMuc)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.DanhMucId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Cart
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenDangNhap).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NgayTao).IsRequired();

            entity.HasOne(e => e.NguoiDung)
                  .WithMany()
                  .HasForeignKey(e => e.TenDangNhap)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CartItem
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SoLuong).IsRequired();
            entity.Property(e => e.DonGia).HasColumnType("decimal(18,2)").IsRequired();

            entity.HasOne(e => e.GioHang)
                  .WithMany(c => c.CartItems)
                  .HasForeignKey(e => e.GioHangId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SanPham)
                  .WithMany()
                  .HasForeignKey(e => e.SanPhamId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenDangNhap).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NgayDat).IsRequired();
            entity.Property(e => e.TongTien).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.TrangThai).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DiaChiGiao).HasMaxLength(500).IsRequired();
            entity.Property(e => e.GhiChu).HasMaxLength(500);

            entity.HasOne(e => e.NguoiDung)
                  .WithMany()
                  .HasForeignKey(e => e.TenDangNhap)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderDetail
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenSanPham).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SoLuong).IsRequired();
            entity.Property(e => e.DonGia).HasColumnType("decimal(18,2)").IsRequired();

            entity.HasOne(e => e.DonHang)
                  .WithMany(o => o.OrderDetails)
                  .HasForeignKey(e => e.DonHangId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SanPham)
                  .WithMany()
                  .HasForeignKey(e => e.SanPhamId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

### Services/

#### Services/IProductService.cs — NEW
```csharp

```

#### Services/ProductService.cs — NEW
```csharp

```

#### Services/ICartService.cs — NEW
```csharp

```

#### Services/CartService.cs — NEW
```csharp

```

#### Services/IOrderService.cs — NEW
```csharp

```

#### Services/OrderService.cs — NEW
```csharp

```

### Pages/

#### Pages/Shared/_CustomerLayout.cshtml — NEW
```html

```

#### Pages/Customer/Home.cshtml — NEW
```html

```

#### Pages/Customer/Home.cshtml.cs — NEW
```csharp

```

#### Pages/Customer/Products.cshtml — NEW
```html

```

#### Pages/Customer/Products.cshtml.cs — NEW
```csharp

```

#### Pages/Customer/ProductDetails.cshtml — NEW
```html

```

#### Pages/Customer/ProductDetails.cshtml.cs — NEW
```csharp

```

#### Pages/Customer/Cart.cshtml — NEW
```html

```

#### Pages/Customer/Cart.cshtml.cs — NEW
```csharp

```

#### Pages/Customer/Checkout.cshtml — NEW
```html

```

#### Pages/Customer/Checkout.cshtml.cs — NEW
```csharp

```

#### Pages/Customer/Orders.cshtml — NEW
```html

```

#### Pages/Customer/Orders.cshtml.cs — NEW
```csharp

```

#### Pages/Shared/_AdminLayout.cshtml — NEW
```html

```

#### Pages/Admin/Dashboard.cshtml — NEW
```html

```

#### Pages/Admin/Dashboard.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Products/Index.cshtml — NEW
```html

```

#### Pages/Admin/Products/Index.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Products/Create.cshtml — NEW
```html

```

#### Pages/Admin/Products/Create.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Products/Edit.cshtml — NEW
```html

```

#### Pages/Admin/Products/Edit.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Products/Delete.cshtml — NEW
```html

```

#### Pages/Admin/Products/Delete.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Categories/Index.cshtml — NEW
```html

```

#### Pages/Admin/Categories/Index.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Categories/Create.cshtml — NEW
```html

```

#### Pages/Admin/Categories/Create.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Categories/Edit.cshtml — NEW
```html

```

#### Pages/Admin/Categories/Edit.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Orders.cshtml — NEW
```html

```

#### Pages/Admin/Orders.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Users.cshtml — NEW
```html

```

#### Pages/Admin/Users.cshtml.cs — NEW
```csharp

```

#### Pages/Admin/Reports.cshtml — NEW
```html

```

#### Pages/Admin/Reports.cshtml.cs — NEW
```csharp

```

### wwwroot/

#### wwwroot/css/customer.css — NEW
```css

```

#### wwwroot/css/admin.css — NEW
```css

```

### Root

#### Program.cs — MODIFY
```csharp

```

#### Pages/Shared/_Layout.cshtml — MODIFY
```html

```

## Slices

### Slice 1: Foundation - Models & DbContext

**Files**: `Models/Category.cs`, `Models/Product.cs`, `Models/Cart.cs`, `Models/CartItem.cs`, `Models/Order.cs`, `Models/OrderDetail.cs`, `Models/NguoiDung.cs`, `Data/AppDbContext.cs`

#### Automated Verification:
- [ ] _Pending code generation_

#### Manual Verification:
- [ ] _Pending code generation_

### Slice 2: Services Layer

**Files**: `Services/IProductService.cs`, `Services/ProductService.cs`, `Services/ICartService.cs`, `Services/CartService.cs`, `Services/IOrderService.cs`, `Services/OrderService.cs`

#### Automated Verification:
- [ ] _Pending code generation_

#### Manual Verification:
- [ ] _Pending code generation_

### Slice 3: Customer Layout & Product Pages

**Files**: `Pages/Shared/_CustomerLayout.cshtml`, `wwwroot/css/customer.css`, `Pages/Customer/Home.cshtml`, `Pages/Customer/Home.cshtml.cs`, `Pages/Customer/Products.cshtml`, `Pages/Customer/Products.cshtml.cs`, `Pages/Customer/ProductDetails.cshtml`, `Pages/Customer/ProductDetails.cshtml.cs`

#### Automated Verification:
- [ ] _Pending code generation_

#### Manual Verification:
- [ ] _Pending code generation_

### Slice 4: Cart & Checkout

**Files**: `Pages/Customer/Cart.cshtml`, `Pages/Customer/Cart.cshtml.cs`, `Pages/Customer/Checkout.cshtml`, `Pages/Customer/Checkout.cshtml.cs`, `Pages/Customer/Orders.cshtml`, `Pages/Customer/Orders.cshtml.cs`

#### Automated Verification:
- [ ] _Pending code generation_

#### Manual Verification:
- [ ] _Pending code generation_

### Slice 5: Admin Layout & Management Pages

**Files**: `Pages/Shared/_AdminLayout.cshtml`, `wwwroot/css/admin.css`, `Pages/Admin/Dashboard.cshtml`, `Pages/Admin/Dashboard.cshtml.cs`, `Pages/Admin/Products/Index.cshtml`, `Pages/Admin/Products/Index.cshtml.cs`, `Pages/Admin/Products/Create.cshtml`, `Pages/Admin/Products/Create.cshtml.cs`, `Pages/Admin/Products/Edit.cshtml`, `Pages/Admin/Products/Edit.cshtml.cs`, `Pages/Admin/Products/Delete.cshtml`, `Pages/Admin/Products/Delete.cshtml.cs`, `Pages/Admin/Categories/Index.cshtml`, `Pages/Admin/Categories/Index.cshtml.cs`, `Pages/Admin/Categories/Create.cshtml`, `Pages/Admin/Categories/Create.cshtml.cs`, `Pages/Admin/Categories/Edit.cshtml`, `Pages/Admin/Categories/Edit.cshtml.cs`, `Pages/Admin/Orders.cshtml`, `Pages/Admin/Orders.cshtml.cs`, `Pages/Admin/Users.cshtml`, `Pages/Admin/Users.cshtml.cs`, `Pages/Admin/Reports.cshtml`, `Pages/Admin/Reports.cshtml.cs`

#### Automated Verification:
- [ ] _Pending code generation_

#### Manual Verification:
- [ ] _Pending code generation_

### Slice 6: Integration — Program.cs & Navigation

**Files**: `Program.cs`, `Pages/Shared/_Layout.cshtml`

#### Automated Verification:
- [ ] _Pending code generation_

#### Manual Verification:
- [ ] _Pending code generation_

## Desired End State

Sau khi hoàn thành:
1. Customer vào trang chủ → thấy danh sách sản phẩm nổi bật
2. Customer tìm kiếm/lọc sản phẩm theo danh mục
3. Customer xem chi tiết sản phẩm với ảnh, giá, mô tả
4. Customer thêm sản phẩm vào giỏ hàng, xem giỏ, tăng/giảm số lượng
5. Customer checkout → tạo đơn hàng thành công
6. Customer xem lịch sử đơn hàng
7. Admin login → vào Dashboard thấy thống kê
8. Admin CRUD sản phẩm, danh mục
9. Admin quản lý đơn hàng (cập nhật trạng thái)
10. Admin quản lý người dùng
11. Admin xem doanh thu
12. User thường không vào được trang Admin

## File Map

- `Models/Category.cs` — NEW — Model danh mục sản phẩm
- `Models/Product.cs` — NEW — Model sản phẩm
- `Models/Cart.cs` — NEW — Model giỏ hàng
- `Models/CartItem.cs` — NEW — Model chi tiết giỏ hàng
- `Models/Order.cs` — NEW — Model đơn hàng
- `Models/OrderDetail.cs` — NEW — Model chi tiết đơn hàng
- `Models/NguoiDung.cs` — MODIFY — Thêm cột Role (VaiTro)
- `Data/AppDbContext.cs` — MODIFY — Thêm DbSet cho tất cả models mới
- `Services/IProductService.cs` — NEW — Interface product service
- `Services/ProductService.cs` — NEW — Implementation product service
- `Services/ICartService.cs` — NEW — Interface cart service
- `Services/CartService.cs` — NEW — Implementation cart service
- `Services/IOrderService.cs` — NEW — Interface order service
- `Services/OrderService.cs` — NEW — Implementation order service
- `Pages/Shared/_CustomerLayout.cshtml` — NEW — Layout cho Customer pages
- `Pages/Customer/Home.cshtml` + `.cshtml.cs` — NEW — Trang chủ Customer
- `Pages/Customer/Products.cshtml` + `.cshtml.cs` — NEW — Danh sách sản phẩm
- `Pages/Customer/ProductDetails.cshtml` + `.cshtml.cs` — NEW — Chi tiết sản phẩm
- `Pages/Customer/Cart.cshtml` + `.cshtml.cs` — NEW — Giỏ hàng
- `Pages/Customer/Checkout.cshtml` + `.cshtml.cs` — NEW — Thanh toán
- `Pages/Customer/Orders.cshtml` + `.cshtml.cs` — NEW — Lịch sử đơn hàng
- `Pages/Shared/_AdminLayout.cshtml` — NEW — Layout cho Admin pages
- `Pages/Admin/Dashboard.cshtml` + `.cshtml.cs` — NEW — Admin Dashboard
- `Pages/Admin/Products/Index.cshtml` + `.cshtml.cs` — NEW — CRUD Products
- `Pages/Admin/Products/Create.cshtml` + `.cshtml.cs` — NEW — Create Product
- `Pages/Admin/Products/Edit.cshtml` + `.cshtml.cs` — NEW — Edit Product
- `Pages/Admin/Products/Delete.cshtml` + `.cshtml.cs` — NEW — Delete Product
- `Pages/Admin/Categories/Index.cshtml` + `.cshtml.cs` — NEW — CRUD Categories
- `Pages/Admin/Categories/Create.cshtml` + `.cshtml.cs` — NEW — Create Category
- `Pages/Admin/Categories/Edit.cshtml` + `.cshtml.cs` — NEW — Edit Category
- `Pages/Admin/Orders.cshtml` + `.cshtml.cs` — NEW — Quản lý đơn hàng
- `Pages/Admin/Users.cshtml` + `.cshtml.cs` — NEW — Quản lý người dùng
- `Pages/Admin/Reports.cshtml` + `.cshtml.cs` — NEW — Thống kê doanh thu
- `wwwroot/css/customer.css` — NEW — CSS cho Customer pages
- `wwwroot/css/admin.css` — NEW — CSS cho Admin pages
- `Program.cs` — MODIFY — DI, Auth policy
- `Pages/Shared/_Layout.cshtml` — MODIFY — Navigation links

## Ordering Constraints

- Slice 1 (Models) → Slice 2 (Services) → Slice 3 (Customer Pages) + Slice 4 (Cart/Checkout) + Slice 5 (Admin Pages) có thể song song sau Slice 2 → Slice 6 (Integration) cuối cùng
- Slice 3, 4, 5 không phụ thuộc lẫn nhau và có thể implement song song sau Slice 2

## Verification Notes

- Build phải thành công không lỗi sau mỗi slice: `dotnet build`
- CSDL phải tạo được: xóa database cũ trước khi chạy để EnsureCreated tạo lại
- Test login với tài khoản Admin và User thường để kiểm tra phân quyền
- Cart chỉ hoạt động với user đã đăng nhập
- Admin pages phải redirect nếu user không có role Admin

## Performance Considerations

- Sử dụng Include/ThenInclude cho EF Core để tránh N+1 queries
- Product listing nên dùng phân trang nếu nhiều sản phẩm
- Cart và Order history chỉ load của user hiện tại (Where filter)
- Admin Reports nên dùng aggregate queries (SUM, COUNT) thay vì load all records

## Migration Notes

- Dùng EnsureCreated() (đã có trong Program.cs) — không dùng Migration
- Xóa DB cũ để tạo lại với schema mới
- Role mặc định là "User" — Admin phải được gán thủ công qua DB hoặc seed data

## Pattern References

- `Pages/Account/Login.cshtml.cs:18-45` — Pattern cho PageModel với DI DbContext
- `Pages/Account/Register.cshtml.cs:33-70` — Pattern cho form handling + validation
- `Pages/Shared/_Layout.cshtml:1-30` — Pattern cho Bootstrap 5 layout + navbar
- `Program.cs:13-22` — Pattern cho Cookie Auth configuration

## Developer Context

_Empty at skeleton creation._

## Design History

- Slice 1: Foundation - Models & DbContext — ✅ completed
- Slice 2: Services Layer — ✅ completed
- Slice 3: Customer Layout & Product Pages — ✅ completed
- Slice 4: Cart & Checkout — ✅ completed
- Slice 5: Admin Layout & Management Pages — ✅ completed
- Slice 6: Integration — Program.cs & Navigation — ✅ completed

## References

- Tài liệu yêu cầu: `Tài liệu không có tiêu đề (3).docx`
- Codebase hiện tại: commit `2506ea7` trên branch `main`
