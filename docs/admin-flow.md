# 🏢 Luồng Xử Lý Admin — ECommerceFinalProject

> **Công nghệ:** ASP.NET Core Razor Pages (.NET 10) + Entity Framework Core + SQL Server  
> **Authentication:** Cookie-based Authentication + Policy `AdminOnly` (Role = "Admin")  
> **Kiến trúc:** 3 tầng (Pages → Services → Data/EF Core)

---

## 📑 Mục lục

1. [Authentication & Authorization](#-1-authentication--authorization)
2. [Tổng quan kiến trúc Admin](#-2-tổng-quan-kiến-trúc-admin)
3. [Dashboard — Trang tổng quan](#-3-dashboard--trang-tổng-quan)
4. [Quản lý Sản phẩm (CRUD)](#-4-quản-lý-sản-phẩm-crud)
5. [Quản lý Danh mục (CRUD)](#-5-quản-lý-danh-mục-crud)
6. [Quản lý Đơn hàng](#-6-quản-lý-đơn-hàng)
7. [Quản lý Người dùng](#-7-quản-lý-người-dùng)
8. [Thống kê / Báo cáo](#-8-thống-kê--báo-cáo)
9. [Sơ đồ Database](#-9-sơ-đồ-database)
10. [Các tầng Services](#-10-các-tầng-services)

---

## 🔐 1. Authentication & Authorization

### 1.1 Cấu hình trong `Program.cs`

```csharp
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";        // Trang đăng nhập
        options.LogoutPath = "/Account/Logout";       // Trang đăng xuất
        options.AccessDeniedPath = "/AccessDenied";   // Trang báo từ chối
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});
```

### 1.2 Luồng đăng nhập (`/Account/Login`)

```
User nhập TaiKhoan + MatKhau
       │
       ▼
[1] Xác định input là TenDangNhap hay Email
    (dựa vào có chứa '@' hay không)
       │
       ▼
[2] Tra cứu DB:
    _context.NguoiDung.FirstOrDefaultAsync(...)
       │
       ▼
[3] Kiểm tra mật khẩu:
    BCrypt.Net.BCrypt.Verify(MatKhau, user.MatKhau)
       │
       ├── [Sai] → ModelState.AddModelError → về lại trang Login
       │
       └── [Đúng] → Tạo ClaimsPrincipal
                      ├── ClaimTypes.NameIdentifier = TenDangNhap
                      ├── ClaimTypes.Name = TenDangNhap
                      ├── "FullName" = Ho + Ten
                      ├── ClaimTypes.Role = VaiTro (VD: "Admin")
                      └── "Email" = Email
                     │
                     ▼
              HttpContext.SignInAsync("Cookies", principal)
                     │
                     ▼
              RedirectToPage("/Index")
```

### 1.3 Authorization Check

Tất cả các Admin Pages đều có attribute:

```csharp
[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel { ... }
```

- Nếu user **chưa đăng nhập** → redirect đến `/Account/Login`
- Nếu user **đã đăng nhập nhưng không phải Admin** → redirect đến `/AccessDenied`
- Nếu user **đã đăng nhập và là Admin** → vào được trang

### 1.4 Điều hướng sau đăng nhập

`/Index.cshtml.cs` kiểm tra role của user hiện tại:

```csharp
public IActionResult OnGet()
{
    if (User.Identity?.IsAuthenticated == true)
    {
        if (User.IsInRole("Admin"))
            return RedirectToPage("/Admin/Dashboard");
        return RedirectToPage("/Customer/Home");
    }
    return Page(); // Trang Index chung (landing page)
}
```

---

## 🏗️ 2. Tổng quan kiến trúc Admin

### 2.1 Cấu trúc thư mục

```
Pages/
├── Admin/
│   ├── _ViewStart.cshtml            → Layout = "_AdminLayout"
│   ├── Dashboard.cshtml             → Trang tổng quan
│   ├── Dashboard.cshtml.cs
│   ├── Orders.cshtml                → Quản lý đơn hàng
│   ├── Orders.cshtml.cs
│   ├── Users.cshtml                 → Quản lý người dùng (read-only)
│   ├── Users.cshtml.cs
│   ├── Reports.cshtml               → Thống kê doanh thu
│   ├── Reports.cshtml.cs
│   ├── Products/
│   │   ├── Index.cshtml             → Danh sách sản phẩm
│   │   ├── Index.cshtml.cs
│   │   ├── Create.cshtml            → Thêm sản phẩm
│   │   ├── Create.cshtml.cs
│   │   ├── Edit.cshtml              → Sửa sản phẩm
│   │   ├── Edit.cshtml.cs
│   │   ├── Delete.cshtml            → Xóa sản phẩm
│   │   └── Delete.cshtml.cs
│   └── Categories/
│       ├── Index.cshtml             → Danh sách danh mục
│       ├── Index.cshtml.cs
│       ├── Create.cshtml            → Thêm danh mục
│       ├── Create.cshtml.cs
│       ├── Edit.cshtml              → Sửa danh mục
│       └── Edit.cshtml.cs
├── Shared/
│   └── _AdminLayout.cshtml          → Layout riêng cho Admin (sidebar)
```

### 2.2 Admin Layout (`_AdminLayout.cshtml`)

- Sidebar bên trái với 6 menu chính:
  - **Dashboard** (`/Admin/Dashboard`)
  - **Sản phẩm** (`/Admin/Products/Index`)
  - **Danh mục** (`/Admin/Categories/Index`)
  - **Đơn hàng** (`/Admin/Orders`)
  - **Người dùng** (`/Admin/Users`)
  - **Thống kê** (`/Admin/Reports`)
- Topbar hiển thị tên user + badge "Admin"
- Hỗ trợ `TempData["SuccessMessage"]` và `TempData["ErrorMessage"]`

### 2.3 Sơ đồ Dependency Injection

```csharp
// Program.cs
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

### 2.4 Luồng request tổng quát

```
[Browser] → HTTP Request → [ASP.NET Core Pipeline]
                                │
                    UseAuthentication() → xác thực cookie
                    UseAuthorization() → kiểm tra policy AdminOnly
                                │
                                ▼
                    [Razor Page Handler]
                    OnGetAsync() hoặc OnPostAsync()
                                │
                                ▼
                    [Service Layer]
                    Business Logic + Validation
                                │
                                ▼
                    [AppDbContext / EF Core]
                    CRUD trên SQL Server
                                │
                                ▼
                    [Razor View (.cshtml)]
                    Render HTML response
```

---

## 📊 3. Dashboard — Trang tổng quan

### 3.1 File

- `Pages/Admin/Dashboard.cshtml` (View)
- `Pages/Admin/Dashboard.cshtml.cs` (Code-behind)
- Route: `/Admin/Dashboard`

### 3.2 Luồng xử lý

```
DashboardModel.OnGetAsync()
       │
       ├── (1) TotalProducts
       │     _context.Products.CountAsync()
       │     → Đếm tổng số sản phẩm trong DB
       │
       ├── (2) TotalOrders
       │     _orderService.GetTotalOrdersAsync()
       │     → _context.Orders.CountAsync()
       │
       ├── (3) TotalUsers
       │     _context.NguoiDung.CountAsync()
       │     → Đếm tổng số người dùng
       │
       ├── (4) TotalRevenue
       │     _orderService.GetTotalRevenueAsync()
       │     → _context.Orders
       │         .Where(o => o.TrangThai == "Đã giao")
       │         .SumAsync(o => o.TongTien)
       │
       └── (5) OrderStatusCounts
             _orderService.GetOrderStatusCountsAsync()
             → _context.Orders
                 .GroupBy(o => o.TrangThai)
                 .Select(g => new { Status = g.Key, Count = g.Count() })
                 .ToDictionaryAsync(...)
```

### 3.3 Giao diện

```
┌─────────────────────────────────────────────────────────┐
│ [Sản phẩm]    [Đơn hàng]    [Người dùng]    [Doanh thu] │
│    19            5              2          45,000,000₫  │
├──────────────────────┬──────────────────────────────────┤
│ Trạng thái đơn hàng  │ Thao tác nhanh                    │
│                      │                                  │
│ Chờ xử lý     3     │ [+ Thêm sản phẩm mới]            │
│ Đang giao     1     │ [+ Thêm danh mục mới]            │
│ Đã giao       1     │ [Xem đơn hàng mới]               │
│ Đã hủy        0     │                                  │
└──────────────────────┴──────────────────────────────────┘
```

---

## 📦 4. Quản lý Sản phẩm (CRUD)

### 4.1 Danh sách sản phẩm — `Products/Index`

**Route:** `/Admin/Products/Index`

```
IndexModel.OnGetAsync()
       │
       └── _productService.GetAllAsync()
             → _context.Products
                 .Include(p => p.DanhMuc)
                 .OrderByDescending(p => p.Id)
                 .ToListAsync()
```

**Giao diện:** Table với các cột:
| ID | Hình ảnh | Tên sản phẩm | Danh mục | Giá | Tồn kho | Thao tác |

- **Tồn kho > 0:** badge xanh lá (còn hàng)
- **Tồn kho = 0:** badge đỏ (hết hàng)
- **Thao tác:** Nút Sửa (✏️) + Xóa (🗑️)

### 4.2 Thêm sản phẩm — `Products/Create`

**Route:** `/Admin/Products/Create`

```
──── GET ────
Load danh sách danh mục → _productService.GetAllCategoriesAsync()

──── POST ────
CreateModel.OnPostAsync(tenSanPham, danhMucId, gia, soLuongTon, hinhAnhUrl, moTa)
       │
       ├── [1] Validate: tên sản phẩm không được trống
       │     → Nếu trống: ModelState.AddModelError + reload form
       │
       ├── [2] Tạo Product object
       │     var product = new Product {
       │         TenSanPham = tenSanPham.Trim(),
       │         DanhMucId = danhMucId,
       │         Gia = gia,
       │         SoLuongTon = soLuongTon,
       │         HinhAnhUrl = hinhAnhUrl?.Trim(),
       │         MoTa = moTa?.Trim()
       │     };
       │
       ├── [3] _productService.AddAsync(product)
       │     → _context.Products.Add(product)
       │     → _context.SaveChangesAsync()
       │
       └── [4] TempData["SuccessMessage"] = "Thêm sản phẩm thành công."
             → RedirectToPage("/Admin/Products/Index")
```

### 4.3 Sửa sản phẩm — `Products/Edit`

**Route:** `/Admin/Products/Edit?id={id}`

```
──── GET ────
EditModel.OnGetAsync(id)
       │
       ├── Product = _productService.GetByIdAsync(id)
       │     → _context.Products.Include(p => p.DanhMuc)
       │         .FirstOrDefaultAsync(p => p.Id == id)
       │
       └── Categories = _productService.GetAllCategoriesAsync()

──── POST ────
EditModel.OnPostAsync(id, tenSanPham, danhMucId, gia, soLuongTon, hinhAnhUrl, moTa)
       │
       ├── [1] Load product từ DB
       │     product = _productService.GetByIdAsync(id)
       │     → [null]? → TempData["ErrorMessage"] → redirect Index
       │
       ├── [2] Cập nhật các field
       │     product.TenSanPham = tenSanPham.Trim();
       │     product.DanhMucId = danhMucId;
       │     product.Gia = gia;
       │     product.SoLuongTon = soLuongTon;
       │     product.HinhAnhUrl = hinhAnhUrl?.Trim();
       │     product.MoTa = moTa?.Trim();
       │
       ├── [3] _productService.UpdateAsync(product)
       │     → _context.Products.Update(product)
       │     → _context.SaveChangesAsync()
       │
       └── [4] TempData["SuccessMessage"] → RedirectToPage
```

### 4.4 Xóa sản phẩm — `Products/Delete`

**Route:** `/Admin/Products/Delete?id={id}`

```
──── GET ────
DeleteModel.OnGetAsync(id)
       │
       └── Product = _productService.GetByIdAsync(id)
             → Hiển thị thông tin sản phẩm để xác nhận xóa

──── POST ────
DeleteModel.OnPostAsync(id)
       │
       └── _productService.DeleteAsync(id)
             → _context.Products.FindAsync(id)
             → [found] → _context.Products.Remove(product)
             → _context.SaveChangesAsync()
```

### 4.5 Tầng Service — `ProductService`

```csharp
// IProductService / ProductService
public interface IProductService
{
    // Product CRUD
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);

    // Product Query
    Task<List<Product>> GetByCategoryAsync(int categoryId);
    Task<List<Product>> SearchAsync(string keyword);
    Task<List<Product>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4);
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize,
        int? categoryId = null, string? search = null, string sortBy = "newest");

    // Category CRUD
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task AddCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(int id);
}
```

---

## 🏷️ 5. Quản lý Danh mục (CRUD)

### 5.1 Danh sách danh mục — `Categories/Index`

**Route:** `/Admin/Categories/Index`

```
IndexModel.OnGetAsync()
       │
       └── _context.Categories
             .Include(c => c.Products)     // Eager load sản phẩm để đếm
             .OrderBy(c => c.Ten)
             .ToListAsync()
```

**Giao diện:**
| ID | Tên danh mục | Mô tả | Số sản phẩm | Thao tác |

### 5.2 Thêm danh mục — `Categories/Create`

**Route:** `/Admin/Categories/Create`

```
POST (ten, moTa)
    │
    ├── Validate: tên không được trống
    ├── Tạo Category { Ten, MoTa }
    ├── _productService.AddCategoryAsync(category)
    │     → _context.Categories.Add(category)
    │     → SaveChanges()
    └── RedirectToPage("/Admin/Categories/Index")
```

### 5.3 Sửa danh mục — `Categories/Edit`

**Route:** `/Admin/Categories/Edit?id={id}`

```
GET (id)
    → _productService.GetCategoryByIdAsync(id)

POST (id, ten, moTa)
    ├── Load category → [null] → báo lỗi redirect
    ├── category.Ten = ten.Trim()
    ├── category.MoTa = moTa?.Trim()
    ├── _productService.UpdateCategoryAsync(category)
    └── RedirectToPage
```

### 5.4 Xóa danh mục

> **Ghi chú:** UI Admin **không có nút xóa danh mục**. Tuy nhiên service có hỗ trợ:

```csharp
public async Task DeleteCategoryAsync(int id)
{
    var hasProducts = await _context.Products
        .AnyAsync(p => p.DanhMucId == id);
    if (hasProducts)
        throw new InvalidOperationException(
            "Không thể xóa danh mục có sản phẩm.");

    var category = await _context.Categories.FindAsync(id);
    if (category != null)
    {
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}
```

**Logic bảo vệ:** Nếu danh mục còn chứa sản phẩm → ném exception, không cho xóa.

---

## 🚚 6. Quản lý Đơn hàng

### 6.1 Danh sách đơn hàng

**Route:** `/Admin/Orders`

```
OrdersModel.OnGetAsync()
       │
       └── _orderService.GetAllOrdersAsync()
             → _context.Orders
                 .Include(o => o.OrderDetails!)
                     .ThenInclude(od => od.SanPham)
                 .Include(o => o.NguoiDung)
                 .OrderByDescending(o => o.NgayDat)
                 .ToListAsync()
```

**Giao diện:**
| Mã ĐH | Khách hàng | Ngày đặt | Số SP | Tổng tiền | Trạng thái | Thao tác |

- **Trạng thái** hiển thị dưới dạng badge màu:
  - 🟡 `Chờ xử lý` → badge vàng
  - 🔵 `Đang giao` → badge xanh dương
  - 🟢 `Đã giao` → badge xanh lá
  - 🔴 `Đã hủy` → badge đỏ

### 6.2 Cập nhật trạng thái đơn hàng

```
POST (orderId, trangThai)
       │
       ├── Dropdown: Chờ xử lý | Đang giao | Đã giao | Đã hủy
       │
       └── _orderService.UpdateOrderStatusAsync(orderId, trangThai)
             → _context.Orders.FindAsync(orderId)
             → order.TrangThai = trangThai
             → _context.SaveChangesAsync()
```

### 6.3 Tầng Service — `OrderService`

```csharp
public interface IOrderService
{
    Task<Order> CreateOrderAsync(string username, string diaChiGiao, string? ghiChu);
    Task<List<Order>> GetUserOrdersAsync(string username);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<List<Order>> GetAllOrdersAsync();
    Task UpdateOrderStatusAsync(int orderId, string trangThai);
    Task<decimal> GetTotalRevenueAsync();
    Task<int> GetTotalOrdersAsync();
    Task<Dictionary<string, int>> GetOrderStatusCountsAsync();
}
```

### 6.4 Luồng tạo đơn hàng (xảy ra ở Customer/Checkout)

```
OrderService.CreateOrderAsync(username, diaChiGiao, ghiChu)
       │
       ├── [1] Lấy giỏ hàng: _cartService.GetCartAsync(username)
       │     → [null hoặc rỗng] → throw InvalidOperationException("Giỏ hàng trống.")
       │
       ├── [2] Kiểm tra tồn kho (stock validation)
       │     foreach item in cart:
       │         if product.SoLuongTon < item.SoLuong
       │             → throw ("Sản phẩm X không đủ số lượng tồn")
       │
       ├── [3] Tính tổng tiền: cart.Sum(ci => ci.SoLuong * ci.DonGia)
       │
       ├── [4] Tạo Order (TrangThai = "Chờ xử lý")
       │     _context.Orders.Add(order)
       │     SaveChangesAsync()
       │
       ├── [5] Tạo OrderDetails + Trừ tồn kho
       │     foreach item in cart:
       │         orderDetail = new { DonHangId, SanPhamId, TenSanPham, SoLuong, DonGia }
       │         product.SoLuongTon -= item.SoLuong
       │
       ├── [6] Xóa giỏ hàng
       │     _context.CartItems.RemoveRange(cart.CartItems)
       │
       └── [7] SaveChangesAsync() → return order
```

---

## 👥 7. Quản lý Người dùng

### 7.1 Xem danh sách

**Route:** `/Admin/Users`

```
UsersModel.OnGetAsync()
       │
       └── _context.NguoiDung.OrderBy(u => u.TenDangNhap).ToListAsync()
```

**Giao diện (read-only):**
| Tên đăng nhập | Họ tên | Email | Số điện thoại | Vai trò |

- **Admin** → badge đen
- **User** → badge xanh dương

> **Giới hạn:** Hiện tại chức năng Users chỉ hiển thị danh sách, không có form sửa/xóa/tạo user từ admin panel. Để tạo user mới, dùng trang `/Account/Register`.

---

## 📈 8. Thống kê / Báo cáo

### 8.1 Tổng quan

**Route:** `/Admin/Reports`

```
ReportsModel.OnGetAsync()
       │
       ├── (1) TotalRevenue
       │     _orderService.GetTotalRevenueAsync()
       │     → _context.Orders
       │         .Where(o => o.TrangThai == "Đã giao")
       │         .SumAsync(o => o.TongTien)
       │
       ├── (2) TotalOrders
       │     _orderService.GetTotalOrdersAsync()
       │     → COUNT of Orders
       │
       ├── (3) DeliveredOrders
       │     _context.Orders.CountAsync(o => o.TrangThai == "Đã giao")
       │
       └── (4) DeliveredOrderDetails (chi tiết)
             _context.OrderDetails
                 .Include(od => od.DonHang!).ThenInclude(o => o.NguoiDung)
                 .Include(od => od.SanPham)
                 .Where(od => od.DonHang!.TrangThai == "Đã giao")
                 .OrderByDescending(od => od.DonHang!.NgayDat)
                 .Select(od => new DeliveredOrderDetail {
                     OrderId, CustomerName, OrderDate,
                     ProductName, Quantity, UnitPrice,
                     TotalPrice = Quantity * UnitPrice
                 })
                 .ToListAsync()
```

### 8.2 Giao diện

```
┌──────────────────┬──────────────────┬──────────────────┐
│  Tổng doanh thu  │  Tổng đơn hàng   │  Đơn đã giao     │
│  45,000,000₫     │       5          │       1          │
└──────────────────┴──────────────────┴──────────────────┘

┌────────────────────────────────────────────────────────┐
│ Chi tiết đơn hàng đã giao                               │
├────────┬──────────┬──────────┬──────────┬────┬──────┬───┤
│ Mã ĐH  │ Khách    │ Ngày     │ Sản phẩm │ SL │ Giá  │   │
│        │ hàng     │ đặt      │          │    │      │   │
├────────┼──────────┼──────────┼──────────┼────┼──────┼───┤
│ #1     │ Nguyễn   │ 10/06    │ iPhone   │ 1  │ 34tr │   │
│        │ Văn A    │          │          │    │      │   │
└────────┴──────────┴──────────┴──────────┴────┴──────┴───┘
```

---

## 🗄️ 9. Sơ đồ Database

### 9.1 Các bảng

```
┌─────────────────┐       ┌─────────────────┐
│   NguoiDung     │       │   DanhMuc       │
├─────────────────┤       ├─────────────────┤
│ PK TenDangNhap  │       │ PK Id           │
│    MatKhau      │       │    Ten          │
│    Ho           │       │    MoTa         │
│    Ten          │       └────────┬────────┘
│    NgaySinh     │                │ 1
│    SoDienThoai  │                │
│    Email (UQ)   │                │ *
│    VaiTro       │       ┌────────▼────────┐
└────────┬────────┘       │   SanPham       │
         │ 1              ├─────────────────┤
         │                │ PK Id           │
         │ *              │    TenSanPham   │
┌────────▼────────┐       │    MoTa         │
│   GioHang       │       │    Gia          │
├─────────────────┤       │    HinhAnhUrl   │
│ PK Id           │       │    SoLuongTon   │
│ FK TenDangNhap  │       │ FK DanhMucId    │
│    NgayTao      │       └────────┬────────┘
└────────┬────────┘                │ 1
         │ 1                       │
         │                         │ *
┌────────▼────────┐       ┌────────▼────────┐
│ ChiTietGioHang  │       │ ChiTietDonHang  │
├─────────────────┤       ├─────────────────┤
│ PK Id           │       │ PK Id           │
│ FK GioHangId    │       │ FK DonHangId    │
│ FK SanPhamId    │       │ FK SanPhamId    │
│    SoLuong      │       │    TenSanPham   │
│    DonGia       │       │    SoLuong      │
└─────────────────┘       │    DonGia       │
                          └────────┬────────┘
                          ┌────────▼────────┐
                          │   DonHang       │
                          ├─────────────────┤
                          │ PK Id           │
                          │ FK TenDangNhap  │
                          │    NgayDat      │
                          │    TongTien     │
                          │    TrangThai    │
                          │    DiaChiGiao   │
                          │    GhiChu       │
                          └─────────────────┘
```

### 9.2 Chi tiết các bảng (theo Entity Models)

| Entity | Table | Key | Fields |
|--------|-------|-----|--------|
| `NguoiDung` | NguoiDung | PK: TenDangNhap | MatKhau, Ho, Ten, NgaySinh, SoDienThoai, Email (unique), VaiTro |
| `Category` | DanhMuc | PK: Id | Ten, MoTa |
| `Product` | SanPham | PK: Id | TenSanPham, MoTa, Gia, HinhAnhUrl, SoLuongTon, DanhMucId (FK→DanhMuc) |
| `Cart` | GioHang | PK: Id | TenDangNhap (FK→NguoiDung), NgayTao |
| `CartItem` | ChiTietGioHang | PK: Id | GioHangId (FK→GioHang), SanPhamId (FK→SanPham), SoLuong, DonGia |
| `Order` | DonHang | PK: Id | TenDangNhap (FK→NguoiDung), NgayDat, TongTien, TrangThai, DiaChiGiao, GhiChu |
| `OrderDetail` | ChiTietDonHang | PK: Id | DonHangId (FK→DonHang), SanPhamId (FK→SanPham), TenSanPham, SoLuong, DonGia |

### 9.3 Constraints đáng chú ý

```csharp
// Product → Category: ON DELETE RESTRICT (không thể xóa danh mục đang có sản phẩm)
entity.HasOne(e => e.DanhMuc)
      .WithMany(c => c.Products)
      .HasForeignKey(e => e.DanhMucId)
      .OnDelete(DeleteBehavior.Restrict);

// Order → NguoiDung: ON DELETE RESTRICT
entity.HasOne(e => e.NguoiDung)
      .WithMany()
      .HasForeignKey(e => e.TenDangNhap)
      .OnDelete(DeleteBehavior.Restrict);
```

---

## ⚙️ 10. Các tầng Services

### 10.1 Service Layer Overview

```
┌─────────────────────────────────────────────────────────┐
│                     Razor Pages                          │
│   (Dashboard, Products, Categories, Orders, Reports)    │
├─────────────────────────────────────────────────────────┤
│                   Service Layer                          │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│   │ ProductService│  │  CartService  │  │  OrderService │  │
│   └──────────────┘  └──────────────┘  └──────────────┘  │
├─────────────────────────────────────────────────────────┤
│               AppDbContext (EF Core)                     │
│                   SQL Server                             │
└─────────────────────────────────────────────────────────┘
```

### 10.2 IProductService / ProductService

**Product CRUD & Query:**
| Method | Mô tả |
|--------|-------|
| `GetAllAsync()` | Lấy tất cả sản phẩm (kèm DanhMuc), sắp xếp mới nhất |
| `GetByIdAsync(id)` | Lấy 1 sản phẩm theo Id |
| `GetByCategoryAsync(categoryId)` | Lọc sản phẩm theo danh mục |
| `SearchAsync(keyword)` | Tìm kiếm theo tên hoặc mô tả (case-insensitive) |
| `GetRelatedAsync(catId, excludeId, take)` | Lấy sản phẩm cùng danh mục (trừ sản phẩm hiện tại) |
| `GetPagedAsync(page, size, categoryId?, search?, sortBy?)` | Phân trang + lọc + sắp xếp (newest, price_asc, price_desc, name) |
| `AddAsync(product)` | Thêm sản phẩm |
| `UpdateAsync(product)` | Cập nhật sản phẩm |
| `DeleteAsync(id)` | Xóa sản phẩm |

**Category CRUD:**
| Method | Mô tả |
|--------|-------|
| `GetAllCategoriesAsync()` | Lấy tất cả danh mục (sắp xếp theo tên) |
| `GetCategoryByIdAsync(id)` | Lấy 1 danh mục |
| `AddCategoryAsync(category)` | Thêm danh mục |
| `UpdateCategoryAsync(category)` | Sửa danh mục |
| `DeleteCategoryAsync(id)` | Xóa danh mục (chỉ xóa được nếu không còn sản phẩm) |

### 10.3 IOrderService / OrderService

| Method | Mô tả | Ghi chú |
|--------|-------|---------|
| `CreateOrderAsync(username, address, note)` | Tạo đơn hàng từ giỏ hàng | Kiểm tra stock, trừ tồn kho, xóa giỏ hàng |
| `GetAllOrdersAsync()` | Lấy tất cả đơn hàng (Admin) | Include OrderDetails + NguoiDung |
| `GetUserOrdersAsync(username)` | Lấy đơn hàng của 1 user (Customer) | Sắp xếp mới nhất |
| `GetOrderByIdAsync(id)` | Lấy chi tiết 1 đơn hàng | |
| `UpdateOrderStatusAsync(orderId, status)` | Cập nhật trạng thái đơn hàng | Admin dùng trên Orders page |
| `GetTotalRevenueAsync()` | Tổng doanh thu (đơn "Đã giao") | Dùng cho Dashboard + Reports |
| `GetTotalOrdersAsync()` | Tổng số đơn hàng | Dùng cho Dashboard |
| `GetOrderStatusCountsAsync()` | Đếm số đơn theo trạng thái | Dùng cho Dashboard |

## ⚙️ 10. Các tầng Services — Hoạt động chi tiết

### 10.1 Service Layer Overview

```
┌─────────────────────────────────────────────────────────┐
│                     Razor Pages                          │
│   (Dashboard, Products, Categories, Orders, Reports)    │
├─────────────────────────────────────────────────────────┤
│                   Service Layer                          │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│   │ ProductService│  │  CartService  │  │  OrderService │  │
│   └──────────────┘  └──────────────┘  └──────────────┘  │
├─────────────────────────────────────────────────────────┤
│               AppDbContext (EF Core)                     │
│                   SQL Server                             │
└─────────────────────────────────────────────────────────┘
```

### ⚡ Nguyên lý hoạt động chung của Services

#### 1. Dependency Injection — Cách Service được Inject

```csharp
// Program.cs — Đăng ký
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

**Scoped lifetime:** Mỗi HTTP request tạo ra 1 instance riêng. Service dùng chung DbContext với PageModel (cùng request → cùng instance DbContext).

```
[HTTP Request] ──→ DI Container
                        │
           ┌───────────┼───────────┐
           ▼           ▼           ▼
   ProductService   CartService  OrderService
        │              │              │
        └──────────────┼──────────────┘
                       ▼
              AppDbContext (1 instance)
                       ▼
                 SQL Server
```

**Luồng inject chi tiết:**
```
PageModel (Dashboard)
  ├── [Inject] IOrderService orderService
  │       └── Constructor: OrderService(AppDbContext context, ICartService cartService)
  │              ├── context ← DI Container tạo 1 AppDbContext mới (Scoped)
  │              └── cartService ← DI Container tạo CartService mới
  │                     └── CartService(AppDbContext context) ← DÙNG CHUNG AppDbContext
  │
  └── [Inject] AppDbContext context ← DÙNG CHUNG với OrderService
```

> **Quan trọng:** Tất cả Service và PageModel trong cùng 1 request đều dùng **1 instance duy nhất** của `AppDbContext`. Điều này đảm bảo Change Tracker của EF Core nhìn thấy tất cả entity changes từ mọi Service.

#### 2. EF Core Change Tracking — Cách Service ghi nhận thay đổi

```
Khi gọi: _context.Products.Add(product)
         _context.Products.Update(product)
         _context.Products.Remove(product)
  
  → EF Core đánh dấu entity state trong Change Tracker:
       Added / Modified / Deleted / Unchanged
  
  → Khi gọi SaveChangesAsync():
       EF Core so sánh Change Tracker với database
       → Tự động sinh câu lệnh SQL tương ứng
       → Gửi đến SQL Server trong 1 transaction

Ví dụ Change Tracker:
┌────────────────────────────────────────────┐
│ Change Tracker (trong 1 request)           │
│                                            │
│ Product{Id=0}    → Added   (INSERT)        │
│ Order{Id=5}      → Modified (UPDATE)       │
│ CartItem{Id=3}   → Deleted  (DELETE)       │
│ Product{Id=10}   → Unchanged (no SQL)      │
└────────────────────────────────────────────┘
        │
        ▼
  _context.SaveChangesAsync()
        │
        ▼
  BEGIN TRANSACTION
    INSERT INTO SanPham ...
    UPDATE DonHang SET ... WHERE Id = 5
    DELETE FROM ChiTietGioHang WHERE Id = 3
  COMMIT TRANSACTION
```

#### 3. Async/Await Pattern — Tất cả method đều async

```csharp
// Pattern: async Task<Result> MethodAsync()
public async Task<List<Product>> GetAllAsync()
{
    return await _context.Products.ToListAsync();
    //       ^^^^^ thread trả về pool, không block
}
```

**Cơ chế:** Khi gặp `await`, thread hiện tại được trả về thread pool, không chờ I/O. Khi I/O hoàn tất, thread khác tiếp tục xử lý.

#### 4. Exception Handling — Cách Service báo lỗi

```csharp
// Service throw exception → PageModel bắt hoặc để pipeline xử lý

// Ví dụ 1: Kiểm tra business logic → throw InvalidOperationException
if (product.SoLuongTon < quantity)
    throw new InvalidOperationException("Số lượng tồn không đủ.");

// Ví dụ 2: Không tìm thấy entity → trả về null (không throw)
public async Task<Product?> GetByIdAsync(int id)
{
    return await _context.Products.FindAsync(id);
    // Nếu không tìm thấy → return null, PageModel tự xử lý
}
```

**Luồng lỗi từ Service → PageModel:**
```
ProductService.AddAsync()
  → _context.SaveChangesAsync()
    → SQL Server throw exception (VD: duplicate key, FK violation)
      → EF Core wrap thành DbUpdateException
        → Propagate lên PageModel.OnPostAsync()
          → Nếu không catch → ASP.NET Core trả về 500
```

---

### 10.2 ProductService — Hoạt động chi tiết

#### 10.2.1 Tổng quan luồng dữ liệu

```
PageModel (Admin Products CRUD)
       │
       ▼
  IProductService (interface)
       │
       ▼
  ProductService (implementation)
       │
  ┌────┴────┐
  │  Query   │  CRUD  │
  └────┬────┘
       │
       ▼
  AppDbContext.DbSet<Product>
       │
       ▼
  SQL Server: SanPham table
```

#### 10.2.2 Constructor & Dependency

```csharp
public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;  // Inject DbContext từ DI Container
    }
```

> **Lưu ý:** ProductService **chỉ phụ thuộc vào AppDbContext**, không phụ thuộc vào Service khác. Đây là leaf dependency.

#### 10.2.3 Chi tiết hoạt động từng method

##### `GetAllAsync()` — Query + Include

```csharp
public async Task<List<Product>> GetAllAsync()
{
    return await _context.Products          // DbSet<Product>
        .Include(p => p.DanhMuc)            // Eager load Category (INNER JOIN)
        .OrderByDescending(p => p.Id)       // Mới nhất trước
        .ToListAsync();                     // Execute query + materialize List
}
```

**Luồng nội bộ:**
```
_context.Products                   → IQueryable<Product> (chưa query)
    .Include(p => p.DanhMuc)        → IQueryable<Product> (thêm JOIN)
    .OrderByDescending(p => p.Id)   → IQueryable<Product> (thêm ORDER BY)
    .ToListAsync()                  → Gửi SQL đến server
                                    → Nhận kết quả
                                    → Map dòng dữ liệu → Product objects
                                    → Điền navigation property DanhMuc
                                    → return List<Product>

SQL sinh ra:
  SELECT p.Id, p.TenSanPham, p.Gia, ..., c.Id, c.Ten, c.MoTa
  FROM SanPham p
  INNER JOIN DanhMuc c ON p.DanhMucId = c.Id
  ORDER BY p.Id DESC
```

##### `GetPagedAsync()` — Query phức tạp nhất (Phân trang + Lọc + Sắp xếp)

```csharp
public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
    int pageIndex, int pageSize,
    int? categoryId = null, string? search = null, string sortBy = "newest")
{
    // Bước 1: Xây dựng IQueryable (chưa chạy SQL)
    var query = _context.Products
        .Include(p => p.DanhMuc)
        .AsQueryable();

    // Bước 2: Apply filters (nếu có) — WHERE clause
    if (categoryId.HasValue)
        query = query.Where(p => p.DanhMucId == categoryId.Value);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var kw = search.Trim().ToLower();
        query = query.Where(p =>
            p.TenSanPham.ToLower().Contains(kw) ||
            (p.MoTa != null && p.MoTa.ToLower().Contains(kw)));
    }

    // Bước 3: Đếm tổng số — 1 SQL query riêng
    var totalCount = await query.CountAsync();

    // Bước 4: Sắp xếp — ORDER BY
    query = sortBy switch
    {
        "price_asc"  => query.OrderBy(p => p.Gia),
        "price_desc" => query.OrderByDescending(p => p.Gia),
        "name"       => query.OrderBy(p => p.TenSanPham),
        _            => query.OrderByDescending(p => p.Id)
    };

    // Bước 5: Skip + Take — SQL OFFSET/FETCH NEXT
    var items = await query
        .Skip(pageIndex * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return (items, totalCount);  // Tuple: 2 giá trị trả về cùng lúc
}
```

**Luồng xử lý phân trang chi tiết:**
```
Giả sử: pageIndex=2, pageSize=10, tổng cộng 45 sản phẩm

Bước 3: SELECT COUNT(*) FROM SanPham WHERE ... → totalCount = 45

Bước 5: SELECT ... FROM SanPham WHERE ...
        ORDER BY ...
        OFFSET 20 ROWS          (vì pageIndex=2 → Skip 2*10=20)
        FETCH NEXT 10 ROWS ONLY (Take 10)

Result: Items có 10 sản phẩm (trang 3: sản phẩm 21-30)
        TotalCount = 45 (để giao diện tính số trang: ceil(45/10)=5)
```

**Lưu ý hiệu năng:** Query chạy **2 lần** đến DB:
1. `CountAsync()` → `SELECT COUNT(*)`
2. `Skip().Take().ToListAsync()` → `SELECT ... OFFSET ... FETCH NEXT`

##### `AddAsync()` — Create

```csharp
public async Task AddAsync(Product product)
{
    _context.Products.Add(product);     // EF tracking: state = Added
    await _context.SaveChangesAsync();  // INSERT vào DB
}
```

**Luồng nội bộ:**
```
[1] _context.Products.Add(product)
      → EF Core tạo một Product proxy với state = Added
      → Product.Id = 0 (vì chưa có giá trị, sẽ do DB generate)

[2] _context.SaveChangesAsync()
      → EF Core kiểm tra Change Tracker
      → Phát hiện Product state = Added
      → Sinh câu lệnh INSERT:
          INSERT INTO SanPham (TenSanPham, MoTa, Gia, HinhAnhUrl, SoLuongTon, DanhMucId)
          VALUES (@p0, @p1, @p2, @p3, @p4, @p5);
          SELECT Id FROM SanPham WHERE ...  -- Lấy Id vừa insert
      → Gửi đến SQL Server
      → Cập nhật Product.Id = giá trị từ DB
```

##### `UpdateAsync()` — Update

```csharp
public async Task UpdateAsync(Product product)
{
    _context.Products.Update(product);   // EF tracking: state = Modified
    await _context.SaveChangesAsync();   // UPDATE tất cả columns
}
```

**Luồng nội bộ:**
```
Giả sử product đến từ EditModel OnPostAsync:
  product = await _productService.GetByIdAsync(id)
  → EF Core đã tracking entity này với state = Unchanged
  
  Sau đó PageModel thay đổi properties:
  product.TenSanPham = "iPhone mới";
  product.Gia = 15000000;
  → EF Core tự động detect: state → Modified (khi gọi Update hoặc SaveChanges)

Khi gọi SaveChangesAsync():
  → EF Core so sánh original value vs current value
  → Chỉ generate UPDATE cho những column thay đổi
  → SQL:
      UPDATE SanPham
      SET TenSanPham = @p0, Gia = @p1
      WHERE Id = @p2
```

##### `DeleteAsync()` — Delete

```csharp
public async Task DeleteAsync(int id)
{
    var product = await _context.Products.FindAsync(id);  // Query + Tracking
    if (product != null)
    {
        _context.Products.Remove(product);                // state = Deleted
        await _context.SaveChangesAsync();                // DELETE
    }
}
```

**Luồng nội bộ:**
```
[1] FindAsync(id)
      → EF Core tìm trong Change Tracker trước
      → Nếu chưa có → query DB:
          SELECT * FROM SanPham WHERE Id = @p0
      → Tracking entity với state = Unchanged

[2] Remove(product)
      → state = Deleted

[3] SaveChangesAsync()
      → DELETE FROM SanPham WHERE Id = @p0
```

##### `DeleteCategoryAsync()` — Delete có validation

```csharp
public async Task DeleteCategoryAsync(int id)
{
    var hasProducts = await _context.Products
        .AnyAsync(p => p.DanhMucId == id);  // EXISTS query
    if (hasProducts)
        throw new InvalidOperationException("Không thể xóa danh mục có sản phẩm.");

    var category = await _context.Categories.FindAsync(id);
    if (category != null)
    {
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}
```

**Tại sao dùng `AnyAsync()` thay vì `CountAsync() > 0`?**
```
AnyAsync() → SELECT CASE WHEN EXISTS (SELECT 1 FROM SanPham WHERE DanhMucId = @p0)
                          THEN 1 ELSE 0 END
           → Dừng ngay khi tìm thấy bản ghi đầu tiên

CountAsync() → SELECT COUNT(*) FROM SanPham WHERE DanhMucId = @p0
            → Đếm TOÀN BỘ dù chỉ cần biết có ≥1 hay không
```

---

### 10.3 OrderService — Hoạt động chi tiết

#### 10.3.1 Tổng quan luồng dữ liệu

```
PageModel (Admin: Orders, Reports, Dashboard)
       │
       ▼
  IOrderService (interface)
       │
       ▼
  OrderService
       │
  ┌────┴────┐
  │  Order   │  OrderDetail  │
  └────┬────┘
       │
  AppDbContext
       │
  ┌────┴────────┐
  │  DonHang     │  ChiTietDonHang  │
  └────┬────────┘
       │
  SQL Server
```

#### 10.3.2 Constructor & Dependencies

```csharp
public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ICartService _cartService;  // OrderService phụ thuộc CartService

    public OrderService(AppDbContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }
```

> **Lưu ý:** OrderService gọi CartService. Vì cả 2 đều Scoped + cùng request → dùng chung 1 AppDbContext instance → Change Tracker đồng nhất.

#### 10.3.3 Chi tiết hoạt động từng method

##### `CreateOrderAsync()` — Method phức tạp nhất toàn bộ project

```csharp
public async Task<Order> CreateOrderAsync(string username, string diaChiGiao, string? ghiChu)
{
    // ─── Bước 1: Lấy giỏ hàng ───
    var cart = await _cartService.GetCartAsync(username);
    // → Gọi sang CartService (cùng DbContext instance)
    // → Query: SELECT FROM GioHang + ChiTietGioHang + SanPham + DanhMuc
    // → WHERE GioHang.TenDangNhap = username

    if (cart?.CartItems == null || !cart.CartItems.Any())
        throw new InvalidOperationException("Giỏ hàng trống.");

    // ─── Bước 2: Kiểm tra tồn kho (Stock Validation) ───
    foreach (var item in cart.CartItems)
    {
        var product = await _context.Products.FindAsync(item.SanPhamId);
        if (product == null)
            throw new InvalidOperationException($"Sản phẩm ID {item.SanPhamId} không tồn tại.");
        if (product.SoLuongTon < item.SoLuong)
            throw new InvalidOperationException(
                $"Sản phẩm \"{product.TenSanPham}\" không đủ số lượng tồn.");
    }

    // ─── Bước 3: Tính tổng tiền ───
    var total = cart.CartItems.Sum(ci => ci.SoLuong * ci.DonGia);
    // Tính trên memory (LINQ to Objects), không query DB

    // ─── Bước 4: Tạo Order ───
    var order = new Order
    {
        TenDangNhap = username,
        NgayDat = DateTime.Now,
        TongTien = total,
        TrangThai = "Chờ xử lý",  // Trạng thái mặc định
        DiaChiGiao = diaChiGiao,
        GhiChu = ghiChu
    };
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();  // LẦN 1: INSERT Order, lấy order.Id

    // ─── Bước 5: Tạo OrderDetails + Trừ tồn kho ───
    foreach (var item in cart.CartItems)
    {
        var product = await _context.Products.FindAsync(item.SanPhamId)!;

        var orderDetail = new OrderDetail
        {
            DonHangId = order.Id,         // FK → order vừa INSERT (có Id rồi)
            SanPhamId = item.SanPhamId,
            TenSanPham = product!.TenSanPham,  // Snapshot: lưu tên tại thời điểm mua
            SoLuong = item.SoLuong,
            DonGia = item.DonGia
        };
        _context.OrderDetails.Add(orderDetail);

        product.SoLuongTon -= item.SoLuong;  // Stock deduction
        // EF Core tự động tracking sự thay đổi này
    }

    // ─── Bước 6: Xóa giỏ hàng ───
    _context.CartItems.RemoveRange(cart.CartItems);

    // ─── Bước 7: Lưu tất cả ───
    await _context.SaveChangesAsync();  // LẦN 2: INSERT OrderDetails + UPDATE Stock + DELETE CartItems

    return order;
}
```

**Luồng chi tiết với dữ liệu thật:**
```
Giả sử user có giỏ hàng:
  CartItems[0]: iPhone 15 Pro Max × 1 (Giá: 34,990,000₫)
  CartItems[1]: AirPods Pro 2 × 2 (Giá: 5,990,000₫)

Bước 3: total = 1*34990000 + 2*5990000 = 46,970,000₫

Bước 4: INSERT INTO DonHang (TenDangNhap, NgayDat, TongTien, TrangThai, ...)
         VALUES ('user1', '2026-06-11', 46970000, 'Chờ xử lý', ...)
         → order.Id = 15 (giả sử)

Bước 5:
  INSERT INTO ChiTietDonHang (DonHangId, SanPhamId, TenSanPham, SoLuong, DonGia)
  VALUES (15, 1, 'iPhone 15 Pro Max', 1, 34990000)
  
  INSERT INTO ChiTietDonHang (DonHangId, SanPhamId, TenSanPham, SoLuong, DonGia)
  VALUES (15, 3, 'Tai nghe AirPods Pro 2', 2, 5990000)
  
  UPDATE SanPham SET SoLuongTon = SoLuongTon - 1 WHERE Id = 1
  UPDATE SanPham SET SoLuongTon = SoLuongTon - 2 WHERE Id = 3

Bước 6: DELETE FROM ChiTietGioHang WHERE GioHangId = 5

Tất cả gói trong 1 implicit transaction (cùng 1 SaveChangesAsync lần 2)
```

**Tại sao cần 2 lần SaveChangesAsync?**
```
Lần 1: INSERT Order → Cần order.Id cho OrderDetail.DonHangId
         Vì Order.Id do DB sinh ra (IDENTITY), không biết trước

Lần 2: INSERT OrderDetails + UPDATE Products + DELETE CartItems
         Gộp chung 1 transaction → atomic: tất cả hoặc không gì cả
```

##### `GetAllOrdersAsync()` — Admin xem tất cả đơn hàng

```csharp
public async Task<List<Order>> GetAllOrdersAsync()
{
    return await _context.Orders
        .Include(o => o.OrderDetails!)
            .ThenInclude(od => od.SanPham)
        .Include(o => o.NguoiDung)
        .OrderByDescending(o => o.NgayDat)
        .ToListAsync();
}
```

**Cấu trúc kết quả trả về (object graph):**
```
List<Order>
  ├── Order #15
  │     ├── NguoiDung { TenDangNhap = "user1", Ho = "Nguyễn", Ten = "Văn A" }
  │     ├── OrderDetails
  │     │     ├── OrderDetail: TenSanPham = "iPhone 15 Pro Max", SoLuong = 1
  │     │     │     └── SanPham { Id = 1, Gia = 34990000 }
  │     │     └── OrderDetail: TenSanPham = "AirPods Pro 2", SoLuong = 2
  │     │           └── SanPham { Id = 3, Gia = 5990000 }
  │     └── (các field: NgayDat, TongTien, TrangThai, DiaChiGiao...)
  │
  └── Order #14
        └── ...
```

##### `UpdateOrderStatusAsync()` — Cập nhật trạng thái đơn hàng

```csharp
public async Task UpdateOrderStatusAsync(int orderId, string trangThai)
{
    var order = await _context.Orders.FindAsync(orderId);
    if (order != null)
    {
        order.TrangThai = trangThai;          // EF Core tracking tự động
        await _context.SaveChangesAsync();    // UPDATE SET TrangThai = ...
    }
}
```

**Cơ chế:**
```
[1] FindAsync(orderId)
      → Query: SELECT * FROM DonHang WHERE Id = @p0
      → EF Core bắt đầu tracking entity (state = Unchanged)

[2] order.TrangThai = "Đã giao"
      → EF Core phát hiện property thay đổi
      → state = Modified (mặc dù không gọi Update())

[3] SaveChangesAsync()
      → So sánh original vs current value
      → Chỉ UPDATE column TrangThai:
          UPDATE DonHang SET TrangThai = N'Đã giao' WHERE Id = @p0
      → Các column khác không đổi thì không gửi lên
```

##### `GetTotalRevenueAsync()` — Tính doanh thu

```csharp
public async Task<decimal> GetTotalRevenueAsync()
{
    return await _context.Orders
        .Where(o => o.TrangThai == "Đã giao")  // Chỉ đơn đã giao
        .SumAsync(o => o.TongTien);             // Tổng tiền
}
```

**SQL:** `SELECT COALESCE(SUM(TongTien), 0) FROM DonHang WHERE TrangThai = N'Đã giao'`

> `SumAsync()` trả về 0 nếu không có bản ghi nào (trong LINQ to Entities), không throw exception.

##### `GetOrderStatusCountsAsync()` — Thống kê trạng thái

```csharp
public async Task<Dictionary<string, int>> GetOrderStatusCountsAsync()
{
    return await _context.Orders
        .GroupBy(o => o.TrangThai)          // GROUP BY
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .ToDictionaryAsync(g => g.Status, g => g.Count);  // Materialize thành Dictionary
}
```

**SQL sinh ra:**
```sql
SELECT [o].[TrangThai] AS [Status], COUNT(*) AS [Count]
FROM [DonHang] AS [o]
GROUP BY [o].[TrangThai]
```

**Kết quả trả về ví dụ:**
```
Dictionary<string, int> {
  { "Chờ xử lý", 3 },
  { "Đang giao", 1 },
  { "Đã giao", 1 },
  { "Đã hủy", 0 }   -- Không xuất hiện vì không có bản ghi nào
}
```

---

### 10.4 CartService — Hoạt động chi tiết

#### 10.4.1 Tổng quan luồng dữ liệu

```
PageModel (Customer Cart, Checkout)
       │
       ▼
  ICartService (interface)
       │
       ▼
  CartService
       │
  ┌────┴────┐
  │  Cart    │  CartItem  │
  └────┬────┘
       │
  AppDbContext
       │
  ┌────┴────────┐
  │  GioHang     │  ChiTietGioHang  │
  └────┬────────┘
       │
  SQL Server
```

> **Lưu ý:** Admin không gọi CartService trực tiếp. CartService được dùng bởi Customer pages (Cart, Checkout) và được OrderService gọi gián tiếp qua `CreateOrderAsync()`.

#### 10.4.2 Chi tiết hoạt động từng method

##### `GetCartAsync()` — 3 level Include

```csharp
public async Task<Cart?> GetCartAsync(string username)
{
    return await _context.Carts
        .Include(c => c.CartItems!)              // Level 1: Cart → CartItems
            .ThenInclude(ci => ci.SanPham!)       // Level 2: CartItem → Product
                .ThenInclude(sp => sp.DanhMuc)    // Level 3: Product → Category
        .FirstOrDefaultAsync(c => c.TenDangNhap == username);
}
```

**SQL sinh ra (4 bảng JOIN):**
```sql
SELECT *
FROM GioHang c
LEFT JOIN ChiTietGioHang ci ON c.Id = ci.GioHangId
LEFT JOIN SanPham p ON ci.SanPhamId = p.Id
LEFT JOIN DanhMuc dm ON p.DanhMucId = dm.Id
WHERE c.TenDangNhap = @p0
```

**Kết quả trả về (object graph):**
```
Cart {
  Id = 5,
  TenDangNhap = "user1",
  CartItems = [
    CartItem {
      Id = 10,
      SoLuong = 1,
      DonGia = 34990000,
      SanPham = Product {
        TenSanPham = "iPhone 15 Pro Max",
        DanhMuc = Category { Ten = "Điện thoại" }
      }
    },
    CartItem {
      Id = 11,
      SoLuong = 2,
      DonGia = 5990000,
      SanPham = Product {
        TenSanPham = "AirPods Pro 2",
        DanhMuc = Category { Ten = "Phụ kiện" }
      }
    }
  ]
}
```

##### `GetOrCreateCartAsync()` — Get hoặc tạo mới

```csharp
public async Task<Cart> GetOrCreateCartAsync(string username)
{
    var cart = await GetCartAsync(username);  // Query giỏ hàng hiện tại
    if (cart == null)
    {
        cart = new Cart { TenDangNhap = username, NgayTao = DateTime.Now };
        _context.Carts.Add(cart);             // Thêm mới (state = Added)
        await _context.SaveChangesAsync();    // INSERT GioHang, lấy cart.Id
    }
    return cart;
}
```

**Luồng:**
```
Lần đầu user thêm sản phẩm:
  [1] GetCartAsync("user1") → null (chưa có giỏ hàng)
  [2] Tạo Cart mới, Add, SaveChanges → INSERT GioHang
  [3] return Cart (đã có Id)

Lần sau:
  [1] GetCartAsync("user1") → Cart (đã tồn tại)
  [2] Không tạo mới, return luôn
```

##### `AddToCartAsync()` — Thêm sản phẩm vào giỏ

```csharp
public async Task AddToCartAsync(string username, int productId, int quantity)
{
    // Kiểm tra sản phẩm tồn tại
    var product = await _context.Products.FindAsync(productId);
    if (product == null)
        throw new InvalidOperationException("Sản phẩm không tồn tại.");

    // Kiểm tra tồn kho (không cho mua quá số lượng tồn)
    if (product.SoLuongTon < quantity)
        throw new InvalidOperationException("Số lượng tồn không đủ.");

    // Lấy hoặc tạo giỏ hàng cho user
    var cart = await GetOrCreateCartAsync(username);

    // Kiểm tra sản phẩm đã có trong giỏ chưa?
    var existingItem = await _context.CartItems
        .FirstOrDefaultAsync(ci => ci.GioHangId == cart.Id && ci.SanPhamId == productId);

    if (existingItem != null)
    {
        // Đã có → tăng số lượng + cập nhật giá mới
        existingItem.SoLuong += quantity;
        existingItem.DonGia = product.Gia;  // Cập nhật giá mới nhất
    }
    else
    {
        // Chưa có → thêm mới
        var cartItem = new CartItem
        {
            GioHangId = cart.Id,
            SanPhamId = productId,
            SoLuong = quantity,
            DonGia = product.Gia
        };
        _context.CartItems.Add(cartItem);
    }

    await _context.SaveChangesAsync();
}
```

**Luồng chi tiết với tình huống cụ thể:**
```
Tình huống: user1 thêm "iPhone 15 Pro Max" vào giỏ (lần 2)

[1] FindAsync(productId=1) → Product{Id=1, SoLuongTon=50}
[2] Check: 50 >= 1 → OK
[3] GetOrCreateCartAsync("user1") → Cart{Id=5} (giỏ đã có)
[4] FirstOrDefaultAsync: tìm CartItem với GioHangId=5 AND SanPhamId=1
    → Tìm thấy CartItem{Id=10, SoLuong=1, DonGia=34990000}
[5] existingItem.SoLuong += 1 → SoLuong = 2
[6] existingItem.DonGia = 34990000 (cập nhật giá mới nhất)
[7] SaveChangesAsync() → UPDATE ChiTietGioHang SET SoLuong=2, DonGia=34990000 WHERE Id=10
```

##### `UpdateQuantityAsync()` — Cập nhật số lượng

```csharp
public async Task UpdateQuantityAsync(string username, int cartItemId, int quantity)
{
    if (quantity < 1)
    {
        // Nếu số lượng < 1 → xóa item khỏi giỏ
        await RemoveFromCartAsync(username, cartItemId);
        return;
    }

    // Tìm giỏ hàng của user
    var cart = await _context.Carts
        .FirstOrDefaultAsync(c => c.TenDangNhap == username);
    if (cart == null) return;  // Không có giỏ → không làm gì

    // Tìm item trong giỏ
    var item = await _context.CartItems
        .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.GioHangId == cart.Id);
    if (item != null)
    {
        item.SoLuong = quantity;
        await _context.SaveChangesAsync();  // UPDATE ChiTietGioHang SET SoLuong = ...
    }
}
```

##### `RemoveFromCartAsync()` — Xóa 1 item

```csharp
public async Task RemoveFromCartAsync(string username, int cartItemId)
{
    var cart = await _context.Carts
        .FirstOrDefaultAsync(c => c.TenDangNhap == username);
    if (cart == null) return;

    var item = await _context.CartItems
        .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.GioHangId == cart.Id);
    if (item != null)
    {
        _context.CartItems.Remove(item);   // state = Deleted
        await _context.SaveChangesAsync(); // DELETE FROM ChiTietGioHang WHERE Id = ...
    }
}
```

##### `ClearCartAsync()` — Xóa toàn bộ giỏ hàng (sau khi đặt hàng)

```csharp
public async Task ClearCartAsync(string username)
{
    var cart = await _context.Carts
        .Include(c => c.CartItems)  // Load cả CartItems để xóa
        .FirstOrDefaultAsync(c => c.TenDangNhap == username);
    if (cart != null)
    {
        _context.CartItems.RemoveRange(cart.CartItems!); // Xóa tất cả items
        await _context.SaveChangesAsync();  // DELETE nhiều dòng 1 lúc
    }
}
```

> **Lưu ý:** Cần `Include(c => c.CartItems)` để load items vào memory trước khi xóa. Nếu không, EF Core không biết items nào cần xóa.

##### `GetTotalAsync()` — Tính tổng tiền giỏ hàng

```csharp
public async Task<decimal> GetTotalAsync(string username)
{
    var cart = await GetCartAsync(username);  // Load cart + items + products
    if (cart?.CartItems == null) return 0;
    return cart.CartItems.Sum(ci => ci.SoLuong * ci.DonGia);  // Tính trong memory
}
```

---

### 10.5 Tương tác giữa các Services

#### 10.5.1 Sơ đồ gọi Service

```
┌────────────────────────────────────────────────────────────────┐
│                        OrderService                            │
│                                                                 │
│  CreateOrderAsync()                                             │
│       │                                                         │
│       ├── Gọi CartService.GetCartAsync(username)               │
│       │     → CartService gọi AppDbContext để query cart       │
│       │                                                         │
│       ├── Gọi _context.Products.FindAsync() (check stock)     │
│       ├── Gọi _context.Orders.Add() + SaveChangesAsync()     │
│       ├── Gọi _context.Products.FindAsync() (trừ stock)      │
│       ├── Gọi _context.CartItems.RemoveRange() (xóa cart)    │
│       └── Gọi _context.SaveChangesAsync() (lưu tất cả)       │
│                                                                 │
│  * CartService.GetCartAsync được gọi,                         │
│    nhưng CartService KHÔNG gọi SaveChangesAsync ở đây          │
│    → Mọi thay đổi được lưu tập trung tại OrderService          │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                        CartService                              │
│                                                                 │
│  GetOrCreateCartAsync()                                          │
│       ├── Gọi GetCartAsync() (query cart)                      │
│       └── Nếu null → tạo mới + SaveChangesAsync()             │
│                                                                 │
│  * OrderService.CreateOrderAsync() gọi GetCartAsync()           │
│    để LẤY dữ liệu, không gọi AddToCartAsync()                  │
└────────────────────────────────────────────────────────────────┘
```

#### 10.5.2 Luồng request hoàn chỉnh (Checkout → Tạo đơn)

```
POST /Customer/Checkout (OnPostAsync)
       │
       ├── (A) Kiểm tra địa chỉ giao hàng hợp lệ
       │
       ├── (B) Gọi: _orderService.CreateOrderAsync(username, address, note)
       │           │
       │           ├── (B1) _cartService.GetCartAsync(username)
       │           │         └── AppDbContext.Query → Cart + Items + Products
       │           │
       │           ├── (B2) Kiểm tra giỏ hàng không rỗng
       │           │
       │           ├── (B3) Kiểm tra tồn kho từng sản phẩm
       │           │         └── AppDbContext.Products.FindAsync() × n lần
       │           │
       │           ├── (B4) Tính tổng tiền: Sum(SoLuong * DonGia)
       │           │
       │           ├── (B5) Tạo Order + SaveChangesAsync() (lần 1)
       │           │         └── AppDbContext.Orders.Add() + SaveChangesAsync()
       │           │
       │           ├── (B6) Tạo OrderDetails + Trừ tồn kho
       │           │         └── AppDbContext.Products.FindAsync() × n lần
       │           │         └── AppDbContext.OrderDetails.Add() × n lần
       │           │         └── product.SoLuongTon -= item.SoLuong
       │           │
       │           ├── (B7) Xóa CartItems: RemoveRange(cart.CartItems)
       │           │
       │           └── (B8) SaveChangesAsync() (lần 2)
       │                     └── Một transaction chứa:
       │                           INSERT ChiTietDonHang × n
       │                           UPDATE SanPham × n
       │                           DELETE ChiTietGioHang × n
       │
       ├── (C) Lưu order.Id vào TempData
       │
       └── (D) RedirectToPage("/Customer/Orders")

     EF Core Change Tracker trong suốt request:
       ┌──────────────────────────────────────┐
       │ Cart {Id=5} → Unchanged             │
       │ CartItem {Id=10} → Deleted          │
       │ CartItem {Id=11} → Deleted          │
       │ Product {Id=1} → Modified (stock -1)│
       │ Product {Id=3} → Modified (stock -2)│
       │ Order {Id=0} → Added (sau lần 1: Id=15)    │
       │ OrderDetail {Id=0} → Added          │
       │ OrderDetail {Id=0} → Added          │
       └──────────────────────────────────────┘
```

#### 10.5.3 Tổng kết nguyên lý hoạt động của Service Layer

| Nguyên lý | Giải thích | Ví dụ trong code |
|-----------|-----------|------------------|
| **Single Responsibility** | Mỗi Service chỉ quản lý 1 nhóm entity | ProductService → Product + Category; OrderService → Order + OrderDetail |
| **Dependency Injection** | Service nhận dependencies qua constructor | `OrderService(AppDbContext, ICartService)` |
| **Scoped Lifetime** | 1 instance / HTTP request, dùng chung DbContext | `AddScoped<IOrderService, OrderService>()` |
| **Async All I/O** | Mọi thao tác DB đều async | `ToListAsync()`, `SaveChangesAsync()`, `FindAsync()` |
| **Business Validation** | Kiểm tra logic nghiệp vụ trước khi ghi DB | Check stock, check cart empty, check category has products |
| **Exception Propagation** | Service throw → PageModel hoặc pipeline xử lý | `throw new InvalidOperationException(...)` |
| **Change Tracking** | EF Core tự động detect entity changes | Product.SoLuongTon -= quantity → tự động UPDATE |
| **Implicit Transaction** | 1 SaveChangesAsync = 1 transaction | Tất cả changes trong 1 lần SaveChanges được commit/rollback cùng nhau |
| **DTO/ViewModel Return** | Trả về object graph đã Include để tránh N+1 | `.Include().ThenInclude().ToListAsync()` |
| **Repository Pattern** | Service che giấu DbContext khỏi PageModel | PageModel không biết EF Core, chỉ gọi interface |

---

## 🔄 Luồng tổng thể (Admin + Customer)

```
┌────────────────────────────────────────────────────────────┐
│                       BROWSER                              │
└────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────┐
│                    /Account/Login                          │
│           (Cookie Authentication + Role Check)             │
└──────────────┬───────────────────────────┬─────────────────┘
               │                           │
         [Role = Admin]              [Role = User]
               │                           │
               ▼                           ▼
┌──────────────────────────┐   ┌──────────────────────────────┐
│    Admin Panel           │   │   Customer Site              │
│                          │   │                              │
│  /Admin/Dashboard        │   │  /Customer/Home              │
│  /Admin/Products/*       │   │  /Customer/Products          │
│  /Admin/Categories/*     │   │  /Customer/ProductDetails    │
│  /Admin/Orders           │   │  /Customer/Cart              │
│  /Admin/Users            │   │  /Customer/Checkout          │
│  /Admin/Reports          │   │  /Customer/Orders            │
└──────────────────────────┘   └──────────────────────────────┘
               │                           │
               └──────────┬────────────────┘
                          ▼
┌────────────────────────────────────────────────────────────┐
│                   Service Layer                             │
│   ProductService  │  CartService  │  OrderService           │
└────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌────────────────────────────────────────────────────────────┐
│               AppDbContext (EF Core)                        │
│                      SQL Server                             │
└────────────────────────────────────────────────────────────┘
```

---

## 📝 Ghi chú quan trọng

1. **Admin mặc định:** Tài khoản `Admin` / `admin123` được seed tự động khi database được tạo lần đầu
2. **Validation:** Mật khẩu được hash bằng BCrypt (`BCrypt.Net.BCrypt.HashPassword`)
3. **Stock protection:** Khi tạo đơn hàng, hệ thống kiểm tra tồn kho và tự động trừ `SoLuongTon`
4. **Revenue chỉ tính:** Các đơn hàng có trạng thái `"Đã giao"`
5. **Encoding check:** Khi khởi động, `Program.cs` kiểm tra lỗi encoding tiếng Việt và tự động tạo lại database nếu phát hiện lỗi
6. **Auto-seed image:** Mỗi lần khởi động, hệ thống tự động cập nhật đường dẫn ảnh local cho 19 sản phẩm
