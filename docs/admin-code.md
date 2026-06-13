# 🔬 Giải Thích Chi Tiết Code Admin

> **Project:** ECommerceFinalProject — ASP.NET Core Razor Pages (.NET 10) + EF Core + SQL Server  
> **Mục đích:** Phân tích từng method, từng class trong chức năng Admin, giải thích cách chúng hoạt động

---

## 📑 Mục lục

1. [Program.cs — Cấu hình Authentication & Authorization](#1-programcs--cấu-hình-authentication--authorization)
2. [AppDbContext — Cấu hình Database Context](#2-appdbcontext--cấu-hình-database-context)
3. [Models — Định nghĩa thực thể](#3-models--định-nghĩa-thực-thể)
4. [Services — Business Logic Layer](#4-services--business-logic-layer)
5. [Admin Pages — Code-behind](#5-admin-pages--code-behind)
6. [Admin Views — .cshtml](#6-admin-views--cshtml)

---

## 1. Program.cs — Cấu hình Authentication & Authorization

### 1.1 Service Registration

```csharp
// File: Program.cs (đầu file)

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Razor Pages
builder.Services.AddRazorPages();

// Đăng ký DbContext với SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("DefaultConnection")));

// Đăng ký Services (Scoped = 1 instance / HTTP request)
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

**Giải thích:**
- `AddScoped<>()`: Mỗi request HTTP sẽ tạo một instance mới của service, dùng chung trong suốt request đó
- `AddDbContext<>()`: EF Core DbContext cũng là Scoped — tự động dùng chung với Services

### 1.2 Cookie Authentication — Cấu hình

```csharp
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";          // URL đăng nhập
        options.LogoutPath = "/Account/Logout";         // URL đăng xuất
        options.AccessDeniedPath = "/AccessDenied";     // URL khi bị từ chối
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Session: 30 phút
        options.SlidingExpiration = true;               // Tự động gia hạn
    });
```

**Giải thích `SlidingExpiration = true`:** Mỗi lần user tương tác trong vòng 30 phút, cookie được gia hạn thêm 30 phút nữa. Nếu user không tương tác quá 30 phút → cookie hết hạn → phải đăng nhập lại.

### 1.3 Authorization Policy — AdminOnly

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});
```

Policy này cho phép dùng `[Authorize(Roles = "Admin")]` trên các PageModel để chặn non-Admin.

### 1.4 Middleware Pipeline

```csharp
var app = builder.Build();

app.UseHttpsRedirection();       // Chuyển HTTP → HTTPS
app.UseStaticFiles();            // Phục vụ file tĩnh (css, js, images)
app.UseRouting();                // Xác định route cho request
app.UseAuthentication();         // Đọc cookie → xác thực user
app.UseAuthorization();          // Kiểm tra policy/role
app.MapRazorPages();             // Map tất cả Razor Pages
```

**Thứ tự middleware rất quan trọng:** `UseAuthentication` phải TRƯỚC `UseAuthorization`, nếu không thì authorization không biết user là ai.

### 1.5 Database Initialization & Seeding

Khi ứng dụng khởi động, `Program.cs` thực hiện chuỗi xử lý database:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // DEBUG: Tạo database + bảng nếu chưa có
    db.Database.EnsureCreated();

    // DEBUG: Kiểm tra database cũ có hợp lệ không
    try { db.Products.Any(); }
    catch
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    // DEBUG: Kiểm tra lỗi encoding tiếng Việt
    var encodingError = false;
    try
    {
        var sampleCat = db.Categories.FirstOrDefault();
        if (sampleCat != null && sampleCat.Ten.Length > 0 && sampleCat.Ten[0] == 'Ä')
            encodingError = true;
    }
    catch { encodingError = true; }

    if (encodingError)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    // Seed dữ liệu mẫu
    if (!db.NguoiDung.Any()) SeedData(db);
    else SeedMissingProducts(db);

    // Auto-update product images
    AutoUpdateImages(db);
}
```

> **Lưu ý:** `EnsureCreated()` không dùng migration — nó tạo database dựa trên model. Nếu model thay đổi, nó không update mà cần xóa đi tạo lại.

---

## 2. AppDbContext — Cấu hình Database Context

### 2.1 DbSet Properties

```csharp
// File: Data/AppDbContext.cs

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 7 DbSet tương ứng 7 bảng
    public DbSet<NguoiDung> NguoiDung { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
}
```

### 2.2 OnModelCreating — Fluent API Configuration

Phương thức này được EF Core gọi khi xây dựng model, dùng để cấu hình chi tiết từng entity:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
```

**NguoiDung:**
```csharp
    modelBuilder.Entity<NguoiDung>(entity =>
    {
        entity.HasKey(e => e.TenDangNhap);       // PK là TenDangNhap (string)
        entity.Property(e => e.TenDangNhap).HasMaxLength(50).IsRequired();
        entity.Property(e => e.MatKhau).IsRequired();
        entity.Property(e => e.Ho).HasMaxLength(100).IsRequired().IsUnicode(true);
        entity.Property(e => e.Ten).HasMaxLength(100).IsRequired().IsUnicode(true);
        entity.Property(e => e.NgaySinh).HasColumnType("date"); // Chỉ lưu ngày, không giờ
        entity.Property(e => e.SoDienThoai).HasMaxLength(20).IsUnicode(true);
        entity.Property(e => e.Email).HasMaxLength(255).IsUnicode(true);
        entity.Property(e => e.VaiTro)
              .HasMaxLength(20)
              .HasDefaultValue("User")          // Mặc định là "User"
              .IsUnicode(true);
        entity.HasIndex(e => e.Email).IsUnique();  // Unique Index trên Email
    });
```

**Category:**
```csharp
    modelBuilder.Entity<Category>(entity =>
    {
        entity.HasKey(e => e.Id);                    // PK auto-increment
        entity.Property(e => e.Ten).HasMaxLength(100).IsRequired().IsUnicode(true);
        entity.Property(e => e.MoTa).HasMaxLength(500).IsUnicode(true);
    });
```

**Product:**
```csharp
    modelBuilder.Entity<Product>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TenSanPham).HasMaxLength(200).IsRequired().IsUnicode(true);
        entity.Property(e => e.MoTa).HasMaxLength(2000).IsUnicode(true);
        entity.Property(e => e.Gia).HasColumnType("decimal(18,2)").IsRequired(); // Kiểu tiền tệ
        entity.Property(e => e.HinhAnhUrl).HasMaxLength(500);
        entity.Property(e => e.SoLuongTon).IsRequired();

        // FK → Category với Restrict (không cho xóa danh mục nếu còn SP)
        entity.HasOne(e => e.DanhMuc)
              .WithMany(c => c.Products)
              .HasForeignKey(e => e.DanhMucId)
              .OnDelete(DeleteBehavior.Restrict);
    });
```

**Order — đáng chú ý:**
```csharp
    modelBuilder.Entity<Order>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TenDangNhap).HasMaxLength(50).IsRequired();
        entity.Property(e => e.NgayDat).IsRequired();
        entity.Property(e => e.TongTien).HasColumnType("decimal(18,2)").IsRequired();
        entity.Property(e => e.TrangThai).HasMaxLength(50).IsRequired().IsUnicode(true);
        entity.Property(e => e.DiaChiGiao).HasMaxLength(500).IsRequired().IsUnicode(true);
        entity.Property(e => e.GhiChu).HasMaxLength(500).IsUnicode(true);

        // FK → NguoiDung với Restrict (không cho xóa user đã có đơn hàng)
        entity.HasOne(e => e.NguoiDung)
              .WithMany()
              .HasForeignKey(e => e.TenDangNhap)
              .OnDelete(DeleteBehavior.Restrict);
    });
```

**OrderDetail:**
```csharp
    modelBuilder.Entity<OrderDetail>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TenSanPham).HasMaxLength(200).IsRequired().IsUnicode(true);
        entity.Property(e => e.SoLuong).IsRequired();
        entity.Property(e => e.DonGia).HasColumnType("decimal(18,2)").IsRequired();

        // FK → Order với Cascade (xóa Order thì OrderDetail tự động xóa)
        entity.HasOne(e => e.DonHang)
              .WithMany(o => o.OrderDetails)
              .HasForeignKey(e => e.DonHangId)
              .OnDelete(DeleteBehavior.Cascade);

        // FK → Product với Restrict
        entity.HasOne(e => e.SanPham)
              .WithMany()
              .HasForeignKey(e => e.SanPhamId)
              .OnDelete(DeleteBehavior.Restrict);
    });
```

**Tổng kết DeleteBehavior:**
| Relationship | Behavior | Ý nghĩa |
|---|---|---|
| Product → Category | **Restrict** | Không xóa danh mục nếu còn SP |
| CartItem → Cart | **Cascade** | Xóa giỏ hàng → xóa luôn item |
| CartItem → Product | **Restrict** | Không xóa SP nếu còn trong giỏ |
| Order → NguoiDung | **Restrict** | Không xóa user nếu còn đơn hàng |
| OrderDetail → Order | **Cascade** | Xóa đơn hàng → xóa luôn chi tiết |
| OrderDetail → Product | **Restrict** | Không xóa SP nếu còn trong chi tiết đơn |

---

## 3. Models — Định nghĩa thực thể

### 3.1 NguoiDung.cs

```csharp
[Table("NguoiDung")]          // Tên bảng trong SQL Server
public class NguoiDung
{
    [Key]                      // Primary Key
    [MaxLength(50)]
    [Required]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    public string MatKhau { get; set; } = string.Empty;  // BCrypt hash

    [MaxLength(100)]
    [Required]
    public string Ho { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required]
    public string Ten { get; set; } = string.Empty;

    [Column(TypeName = "date")]     // SQL type: date (không có time)
    public DateTime? NgaySinh { get; set; }

    [MaxLength(20)]
    public string? SoDienThoai { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Display(Name = "Vai trò")]
    public string VaiTro { get; set; } = "User";  // Mặc định: "User"
}
```

**Điểm đặc biệt:** `TenDangNhap` là string PK — không phải int auto-increment.  
`MatKhau` lưu BCrypt hash, không lưu plain text.

### 3.2 Product.cs

```csharp
[Table("SanPham")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Auto-increment
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Tên sản phẩm")]
    public string TenSanPham { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? MoTa { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Gia { get; set; }

    [MaxLength(500)]
    public string? HinhAnhUrl { get; set; }

    [Required]
    public int SoLuongTon { get; set; } = 0;

    [Required]
    [Display(Name = "Danh mục")]
    public int DanhMucId { get; set; }

    // Navigation Property — cho phép truy cập Category từ Product
    [ForeignKey(nameof(DanhMucId))]
    public Category? DanhMuc { get; set; }
}
```

**Navigation Property `DanhMuc`:** Cho phép `product.DanhMuc.Ten` khi query có `.Include(p => p.DanhMuc)`.

### 3.3 Order.cs

```csharp
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
    public DateTime NgayDat { get; set; } = DateTime.Now;  // Mặc định: ngay khi tạo

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TongTien { get; set; }

    [Required]
    [MaxLength(50)]
    public string TrangThai { get; set; } = "Chờ xử lý";  // 4 trạng thái

    [Required]
    [MaxLength(500)]
    public string DiaChiGiao { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? GhiChu { get; set; }

    // Navigation — cho phép truy cập thông tin user
    [ForeignKey(nameof(TenDangNhap))]
    public NguoiDung? NguoiDung { get; set; }

    // Navigation — collection các OrderDetail
    public ICollection<OrderDetail>? OrderDetails { get; set; }
}
```

**4 trạng thái đơn hàng:** `"Chờ xử lý"` → `"Đang giao"` → `"Đã giao"` / `"Đã hủy"`

### 3.4 Các Model còn lại (Category, Cart, CartItem, OrderDetail)

Cấu trúc tương tự, dùng `[Table]` attribute để map tên bảng tiếng Việt và `[ForeignKey]` để khai báo quan hệ.

---

## 4. Services — Business Logic Layer

### 🧠 Nguyên lý hoạt động của Services

#### A. Dependency Injection — Cách Service được tạo và inject

```csharp
// Program.cs — Đăng ký
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

**Scoped lifetime** nghĩa là:
- Mỗi HTTP request → 1 bộ instance mới của các Service
- Tất cả Service trong cùng request dùng **chung 1 AppDbContext instance**
- Đảm bảo Change Tracker của EF Core đồng nhất xuyên suốt request

```
[HTTP Request] ──→ DI Container
                        │
           ┌───────────┼───────────┐
           ▼           ▼           ▼
   ProductService   CartService  OrderService
        │              │              │
        └──────────────┼──────────────┘
                       ▼
              AppDbContext (1 instance duy nhất)
                       ▼
                 SQL Server
```

#### B. EF Core Change Tracking — Cách Service ghi nhận thay đổi

Khi Service gọi các method trên DbContext, EF Core không gửi SQL ngay mà đánh dấu **entity state** trong Change Tracker:

```
─── Entity States ───

_addContext.Products.Add(product)_      → State = **Added**    (sẽ INSERT)
_Chỉnh sửa property trên entity_         → State = **Modified** (sẽ UPDATE)
_context.Products.Remove(product)_     → State = **Deleted**   (sẽ DELETE)
_Chỉ đọc, không đụng đến_               → State = **Unchanged** (không làm gì)

─── Khi gọi SaveChangesAsync() ───

Change Tracker lúc này:
┌────────────────────────────────────────────┐
│ Product{Id=0, TenSanPham="X"} → Added     │
│ Order{Id=5, TrangThai="Đã giao"} → Modified│
│ CartItem{Id=3} → Deleted                   │
│ Product{Id=10} → Unchanged (bỏ qua)        │
└────────────────────────────────────────────┘
        │
        ▼
  BEGIN TRANSACTION          ← 1 transaction cho tất cả
    INSERT INTO SanPham ...
    UPDATE DonHang SET ... WHERE Id = 5
    DELETE FROM ChiTietGioHang WHERE Id = 3
  COMMIT TRANSACTION
```

#### C. Auto-Detect Changes — Cách EF Core biết entity đã thay đổi

```csharp
// Service load entity
var product = await _context.Products.FindAsync(id);
// → product được tracking với state = Unchanged
// → EF Core lưu snapshot: originalValues = { TenSanPham="iPhone", Gia=34990000 }

// PageModel chỉnh sửa
product.TenSanPham = "iPhone 16 Pro";
product.Gia = 39990000;

// Khi SaveChangesAsync() được gọi:
// → EF Core so sánh currentValues với originalValues
// → Phát hiện: TenSanPham và Gia đã thay đổi
// → Sinh UPDATE chỉ cho 2 column đó:
//   UPDATE SanPham SET TenSanPham = @p0, Gia = @p1 WHERE Id = @p2
```

**Lưu ý:** EF Core tự động detect changes ngay cả khi không gọi `Update()`. Chỉ cần thay đổi property trên entity đang được tracking là đủ.

#### D. Async/Await — Không block thread

```csharp
// Tất cả method trong Service đều theo pattern:
public async Task<List<Product>> MethodAsync()
{
    return await _context.Products.ToListAsync();
    //       ^^^^^ thread trả về thread pool, không chờ I/O
}
```

**Luồng:**
```
Thread Pool Thread #1 ──→ Gọi MethodAsync()
                              │
                        await _context.Products.ToListAsync()
                              │
                         ┌────┴────┐
                         │  I/O     │ ← Thread #1 trả về pool
                         │  chờ     │
                         │  SQL     │
                         └────┬────┘
                              │
                    Thread Pool Thread #2 ──→ Tiếp tục sau await
```

#### E. Exception Handling — Service báo lỗi cho PageModel

```csharp
// Business validation → throw InvalidOperationException
if (product.SoLuongTon < quantity)
    throw new InvalidOperationException("Số lượng tồn không đủ.");

// Entity không tồn tại → return null (không throw)
public async Task<Product?> GetByIdAsync(int id)
{
    return await _context.Products.FindAsync(id);
    // null → PageModel tự kiểm tra và xử lý
}

// Lỗi DB → DbUpdateException tự động propagate
await _context.SaveChangesAsync();
// → Nếu SQL fail (FK violation, duplicate key...)
// → EF Core wrap thành DbUpdateException
// → Nếu PageModel không catch → ASP.NET Core trả về 500
```

---

### 4.1 ProductService — Chi tiết từng method

```csharp
// File: Services/ProductService.cs

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    // Constructor Injection: DI Container tự động cung cấp AppDbContext
    public ProductService(AppDbContext context)
    {
        _context = context;
    }
```

**Đặc điểm:** ProductService là **leaf service** — chỉ phụ thuộc vào AppDbContext, không gọi service khác.

#### GetAllAsync()

```csharp
public async Task<List<Product>> GetAllAsync()
{
    return await _context.Products
        .Include(p => p.DanhMuc)       // Eager load Category (JOIN)
        .OrderByDescending(p => p.Id)  // Mới nhất trước
        .ToListAsync();
}
```

**SQL generated:**
```sql
SELECT p.*, c.*
FROM SanPham p
INNER JOIN DanhMuc c ON p.DanhMucId = c.Id
ORDER BY p.Id DESC
```

#### GetByCategoryAsync()

```csharp
public async Task<List<Product>> GetByCategoryAsync(int categoryId)
{
    return await _context.Products
        .Include(p => p.DanhMuc)
        .Where(p => p.DanhMucId == categoryId)  -- Lọc theo danh mục
        .OrderByDescending(p => p.Id)
        .ToListAsync();
}
```

#### SearchAsync()

```csharp
public async Task<List<Product>> SearchAsync(string keyword)
{
    if (string.IsNullOrWhiteSpace(keyword))
        return await GetAllAsync();  -- Nếu keyword rỗng → trả về tất cả

    keyword = keyword.Trim().ToLower();
    return await _context.Products
        .Include(p => p.DanhMuc)
        .Where(p => p.TenSanPham.ToLower().Contains(keyword)      -- Tìm trong tên
                 || (p.MoTa != null && p.MoTa.ToLower().Contains(keyword)))  -- hoặc mô tả
        .OrderByDescending(p => p.Id)
        .ToListAsync();
}
```

**Cách hoạt động:** Chuyển keyword và tên sản phẩm về lower case, dùng `Contains()` để tìm kiếm LIKE '%keyword%'. Tuy nhiên cách này **không index-friendly** và chậm trên dữ liệu lớn vì dùng hàm `ToLower()` trong SQL.

#### GetPagedAsync() — Phân trang nâng cao

```csharp
public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
    int pageIndex,           // Trang hiện tại (0-based)
    int pageSize,            // Số item mỗi trang
    int? categoryId = null,  // Lọc danh mục (optional)
    string? search = null,   // Từ khóa tìm kiếm (optional)
    string sortBy = "newest" // Cách sắp xếp
)
{
    // Bước 1: Khởi tạo query với Include
    var query = _context.Products
        .Include(p => p.DanhMuc)
        .AsQueryable();  // Cho phép xây dựng dynamic query

    // Bước 2: Apply filters (nếu có)
    if (categoryId.HasValue)
        query = query.Where(p => p.DanhMucId == categoryId.Value);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var kw = search.Trim().ToLower();
        query = query.Where(p => p.TenSanPham.ToLower().Contains(kw)
                              || (p.MoTa != null && p.MoTa.ToLower().Contains(kw)));
    }

    // Bước 3: Đếm tổng số (quan trọng cho phân trang)
    var totalCount = await query.CountAsync();

    // Bước 4: Sắp xếp
    query = sortBy switch
    {
        "price_asc"   => query.OrderBy(p => p.Gia),             // Giá tăng dần
        "price_desc"  => query.OrderByDescending(p => p.Gia),   // Giá giảm dần
        "name"        => query.OrderBy(p => p.TenSanPham),      // Tên A→Z
        _             => query.OrderByDescending(p => p.Id)     // Mới nhất
    };

    // Bước 5: Skip + Take (phân trang)
    var items = await query
        .Skip(pageIndex * pageSize)   // Bỏ qua item của trang trước
        .Take(pageSize)               // Lấy đúng số item của trang này
        .ToListAsync();

    return (items, totalCount);
}
```

**Cách phân trang hoạt động:**
- `pageIndex = 0`, `pageSize = 10` → `Skip(0)` → `Take(10)` → 10 item đầu
- `pageIndex = 1`, `pageSize = 10` → `Skip(10)` → `Take(10)` → 10 item tiếp theo

#### AddAsync()

```csharp
public async Task AddAsync(Product product)
{
    _context.Products.Add(product);    // Tracking entity (Added state)
    await _context.SaveChangesAsync(); // INSERT INTO SanPham VALUES (...)
}
```

#### UpdateAsync()

```csharp
public async Task UpdateAsync(Product product)
{
    _context.Products.Update(product);   // Mark entity as Modified
    await _context.SaveChangesAsync();   // UPDATE SanPham SET ... WHERE Id = ...
}
```

#### DeleteAsync()

```csharp
public async Task DeleteAsync(int id)
{
    var product = await _context.Products.FindAsync(id); // Tìm entity
    if (product != null)
    {
        _context.Products.Remove(product);  // Mark as Deleted
        await _context.SaveChangesAsync();  // DELETE FROM SanPham WHERE Id = ...
    }
}
```

**Lưu ý:** `FindAsync(id)` chỉ tìm bằng Primary Key. Nếu không tìm thấy, trả về `null`, không throw exception.

#### DeleteCategoryAsync() — Có validation

```csharp
public async Task DeleteCategoryAsync(int id)
{
    // Kiểm tra xem danh mục có sản phẩm không
    var hasProducts = await _context.Products
        .AnyAsync(p => p.DanhMucId == id);
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

Dùng `AnyAsync()` — hiệu quả hơn `CountAsync() > 0` vì `ANY` dừng ngay khi tìm thấy bản ghi đầu tiên.

### 4.2 OrderService — Chi tiết từng method

```csharp
// File: Services/OrderService.cs

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ICartService _cartService;

    public OrderService(AppDbContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }
```

#### CreateOrderAsync() — Method phức tạp nhất

```csharp
public async Task<Order> CreateOrderAsync(string username, string diaChiGiao, string? ghiChu)
{
    // Bước 1: Lấy giỏ hàng của user
    var cart = await _cartService.GetCartAsync(username);

    // Kiểm tra giỏ hàng có tồn tại và có item không
    if (cart?.CartItems == null || !cart.CartItems.Any())
        throw new InvalidOperationException("Giỏ hàng trống.");

    // Bước 2: Kiểm tra tồn kho cho từng sản phẩm
    foreach (var item in cart.CartItems)
    {
        var product = await _context.Products.FindAsync(item.SanPhamId);
        if (product == null)
            throw new InvalidOperationException($"Sản phẩm ID {item.SanPhamId} không tồn tại.");

        // Nếu số lượng mua > số lượng tồn → báo lỗi
        if (product.SoLuongTon < item.SoLuong)
            throw new InvalidOperationException(
                $"Sản phẩm \"{product.TenSanPham}\" không đủ số lượng tồn.");
    }

    // Bước 3: Tính tổng tiền
    var total = cart.CartItems.Sum(ci => ci.SoLuong * ci.DonGia);

    // Bước 4: Tạo Order (trạng thái mặc định "Chờ xử lý")
    var order = new Order
    {
        TenDangNhap = username,
        NgayDat = DateTime.Now,
        TongTien = total,
        TrangThai = "Chờ xử lý",
        DiaChiGiao = diaChiGiao,
        GhiChu = ghiChu
    };

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();  // Lúc này order.Id được sinh ra

    // Bước 5: Tạo OrderDetails + Trừ tồn kho
    foreach (var item in cart.CartItems)
    {
        var product = await _context.Products.FindAsync(item.SanPhamId)!;

        var orderDetail = new OrderDetail
        {
            DonHangId = order.Id,         // FK đến order vừa tạo
            SanPhamId = item.SanPhamId,
            TenSanPham = product!.TenSanPham,  // Lưu tên SP tại thời điểm mua
            SoLuong = item.SoLuong,
            DonGia = item.DonGia
        };
        _context.OrderDetails.Add(orderDetail);

        // Trừ tồn kho
        product.SoLuongTon -= item.SoLuong;
    }

    // Bước 6: Xóa giỏ hàng (sau khi đã tạo đơn thành công)
    _context.CartItems.RemoveRange(cart.CartItems);

    // Bước 7: Lưu tất cả thay đổi
    await _context.SaveChangesAsync();

    return order;
}
```

**Giải thích `SaveChangesAsync()` được gọi 2 lần:**
1. Lần 1 — Để EF Core generate `order.Id` (identity insert) → dùng được `order.Id` cho `OrderDetail.DonHangId`
2. Lần 2 — Lưu OrderDetails + cập nhật tồn kho + xóa CartItems

Có thể gộp thành 1 lần nếu dùng `order.Id` sau khi `SaveChanges`, nhưng cách này rõ ràng hơn.

#### GetAllOrdersAsync()

```csharp
public async Task<List<Order>> GetAllOrdersAsync()
{
    return await _context.Orders
        .Include(o => o.OrderDetails!)      // JOIN ChiTietDonHang
            .ThenInclude(od => od.SanPham)   // JOIN SanPham (qua OrderDetail)
        .Include(o => o.NguoiDung)           // JOIN NguoiDung
        .OrderByDescending(o => o.NgayDat)  // Mới nhất trước
        .ToListAsync();
}
```

**SQL generated (3 JOINs):**
```sql
SELECT *
FROM DonHang o
LEFT JOIN ChiTietDonHang od ON o.Id = od.DonHangId
LEFT JOIN SanPham p ON od.SanPhamId = p.Id
LEFT JOIN NguoiDung u ON o.TenDangNhap = u.TenDangNhap
ORDER BY o.NgayDat DESC
```

#### UpdateOrderStatusAsync()

```csharp
public async Task UpdateOrderStatusAsync(int orderId, string trangThai)
{
    var order = await _context.Orders.FindAsync(orderId);
    if (order != null)
    {
        order.TrangThai = trangThai;          // Cập nhật trạng thái
        await _context.SaveChangesAsync();    // UPDATE DonHang SET TrangThai = ...
    }
}
```

#### GetTotalRevenueAsync()

```csharp
public async Task<decimal> GetTotalRevenueAsync()
{
    return await _context.Orders
        .Where(o => o.TrangThai == "Đã giao") // Chỉ tính đơn đã giao
        .SumAsync(o => o.TongTien);           // SUM tổng tiền
}
```

#### GetOrderStatusCountsAsync()

```csharp
public async Task<Dictionary<string, int>> GetOrderStatusCountsAsync()
{
    return await _context.Orders
        .GroupBy(o => o.TrangThai)                    // GROUP BY TrangThai
        .Select(g => new { Status = g.Key, Count = g.Count() }) // SELECT key, count
        .ToDictionaryAsync(g => g.Status, g => g.Count); // → Dictionary
}
```

**SQL generated:**
```sql
SELECT [o].[TrangThai] AS [Status], COUNT(*) AS [Count]
FROM [DonHang] AS [o]
GROUP BY [o].[TrangThai]
```

### 4.3 CartService — Chi tiết

```csharp
public class CartService : ICartService
{
    private readonly AppDbContext _context;

    public CartService(AppDbContext context)
    {
        _context = context;
    }
```

#### GetCartAsync()

```csharp
public async Task<Cart?> GetCartAsync(string username)
{
    return await _context.Carts
        .Include(c => c.CartItems!)
            .ThenInclude(ci => ci.SanPham!)
                .ThenInclude(sp => sp.DanhMuc)   // 3 level JOIN!
        .FirstOrDefaultAsync(c => c.TenDangNhap == username);
}
```

**3 level Include:** Cart → CartItems → Product → Category. Kết quả là object graph đầy đủ để hiển thị giỏ hàng mà không cần query thêm.

#### AddToCartAsync()

```csharp
public async Task AddToCartAsync(string username, int productId, int quantity)
{
    // Kiểm tra sản phẩm tồn tại
    var product = await _context.Products.FindAsync(productId);
    if (product == null)
        throw new InvalidOperationException("Sản phẩm không tồn tại.");

    // Kiểm tra tồn kho
    if (product.SoLuongTon < quantity)
        throw new InvalidOperationException("Số lượng tồn không đủ.");

    // Lấy hoặc tạo giỏ hàng
    var cart = await GetOrCreateCartAsync(username);

    // Kiểm tra sản phẩm đã có trong giỏ chưa
    var existingItem = await _context.CartItems
        .FirstOrDefaultAsync(ci => ci.GioHangId == cart.Id && ci.SanPhamId == productId);

    if (existingItem != null)
    {
        // Nếu đã có → tăng số lượng + cập nhật đơn giá
        existingItem.SoLuong += quantity;
        existingItem.DonGia = product.Gia;  // Cập nhật giá mới nhất
    }
    else
    {
        // Nếu chưa có → thêm mới
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

#### UpdateQuantityAsync()

```csharp
public async Task UpdateQuantityAsync(string username, int cartItemId, int quantity)
{
    if (quantity < 1)
    {
        // Nếu số lượng < 1 → xóa item
        await RemoveFromCartAsync(username, cartItemId);
        return;
    }

    var cart = await _context.Carts
        .FirstOrDefaultAsync(c => c.TenDangNhap == username);
    if (cart == null) return;

    var item = await _context.CartItems
        .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.GioHangId == cart.Id);
    if (item != null)
    {
        item.SoLuong = quantity;
        await _context.SaveChangesAsync();
    }
}
```

### 4.4 Tương tác giữa các Services

#### 4.4.1 Sơ đồ gọi Service

```
┌────────────────────────────────────────────────────────────────┐
│                        OrderService                            │
│                                                                 │
│  CreateOrderAsync()                                             │
│       │                                                         │
│       ├── Gọi CartService.GetCartAsync(username)               │
│       │     → Cùng DbContext instance → Change Tracker đồng bộ │
│       │                                                         │
│       ├── _context.Products.FindAsync() (check stock)          │
│       ├── _context.Orders.Add() + SaveChangesAsync() (lần 1)  │
│       ├── _context.Products.FindAsync() (trừ stock)           │
│       ├── _context.CartItems.RemoveRange() (xóa cart)         │
│       └── _context.SaveChangesAsync() (lần 2 - 1 transaction) │
│                                                                 │
│  * CartService.GetCartAsync chỉ được gọi để LẤY dữ liệu        │
│  * OrderService tự quản lý SaveChanges — không qua CartService  │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                        CartService                              │
│                                                                 │
│  GetOrCreateCartAsync() → GetCartAsync() + thêm mới nếu null   │
│  AddToCartAsync()       → GetOrCreateCartAsync() + thêm item   │
│  UpdateQuantityAsync()  → GetCart() + chỉnh quantity           │
│                                                                 │
│  * CartService KHÔNG gọi OrderService                           │
│  * CartService là dependency của OrderService (chiều ngược lại)│
└────────────────────────────────────────────────────────────────┘
```

#### 4.4.2 Luồng gọi Service hoàn chỉnh (Checkout → Tạo đơn hàng)

```
POST /Customer/Checkout (PageModel.OnPostAsync)
       │
       ├── Validate form (địa chỉ giao hàng)
       │
       ├── ─── Gọi Service ───
       │
       ├── [1] _orderService.CreateOrderAsync(username, diaChiGiao, ghiChu)
       │           │
       │           ├── [1a] _cartService.GetCartAsync(username)
       │           │         → AppDbContext.Query()
       │           │           SELECT ... FROM GioHang + ChiTietGioHang + SanPham
       │           │           WHERE TenDangNhap = @p0
       │           │
       │           ├── [1b] Kiểm tra: cart != null && cart.CartItems.Any()
       │           │         → throw nếu giỏ hàng trống
       │           │
       │           ├── [1c] Kiểm tra tồn kho từng item
       │           │         foreach (var item in cart.CartItems)
       │           │             _context.Products.FindAsync(item.SanPhamId)
       │           │             if (product.SoLuongTon < item.SoLuong) → throw
       │           │
       │           ├── [1d] Tính tổng: cart.CartItems.Sum(ci => ci.SoLuong * ci.DonGia)
       │           │
       │           ├── [1e] Tạo Order → SaveChangesAsync()
       │           │         → INSERT INTO DonHang (Id=0→15, TrangThai='Chờ xử lý'...)
       │           │
       │           ├── [1f] Tạo OrderDetails + Trừ tồn kho
       │           │         foreach (var item in cart.CartItems)
       │           │             _context.OrderDetails.Add(orderDetail)
       │           │             product.SoLuongTon -= item.SoLuong
       │           │
       │           ├── [1g] Xóa CartItems
       │           │         _context.CartItems.RemoveRange(cart.CartItems)
       │           │
       │           └── [1h] SaveChangesAsync()
       │                     → Transaction: INSERT OrderDetails + UPDATE Stock + DELETE CartItems
       │
       ├── [2] TempData["OrderId"] = order.Id
       │
       └── [3] RedirectToPage("/Customer/Orders")

EF Core Change Tracker tại thời điểm [1h]:
┌───────────────────────────────────────────────┐
│ Cart{Id=5, TenDangNhap="user1"} → Unchanged  │
│ CartItem{Id=10, SanPhamId=1} → Deleted       │
│ CartItem{Id=11, SanPhamId=3} → Deleted       │
│ Product{Id=1, SoLuongTon=50→49} → Modified   │
│ Product{Id=3, SoLuongTon=100→98} → Modified  │
│ Order{Id=0→15} → Added                       │
│ OrderDetail{Id=0, DonHangId=15} → Added      │
│ OrderDetail{Id=0, DonHangId=15} → Added      │
└───────────────────────────────────────────────┘
        │
        ▼  SaveChangesAsync() lần 2
  BEGIN TRANSACTION
    INSERT INTO ChiTietDonHang (DonHangId, SanPhamId, TenSanPham, SoLuong, DonGia)
    VALUES (15, 1, 'iPhone 15 Pro Max', 1, 34990000)
    INSERT INTO ChiTietDonHang (DonHangId, SanPhamId, TenSanPham, SoLuong, DonGia)
    VALUES (15, 3, 'Tai nghe AirPods Pro 2', 2, 5990000)

    UPDATE SanPham SET SoLuongTon = 49 WHERE Id = 1
    UPDATE SanPham SET SoLuongTon = 98 WHERE Id = 3

    DELETE FROM ChiTietGioHang WHERE GioHangId = 5
  COMMIT TRANSACTION
```

#### 4.4.3 Tổng kết nguyên lý Service Layer

| Nguyên lý | Giải thích | Code evidence |
|-----------|-----------|---------------|
| **Single Responsibility** | Mỗi Service quản lý 1 nhóm entity riêng | ProductService → Product+Category; OrderService → Order+OrderDetail |
| **Dependency Injection** | Nhận dependencies qua constructor | `public OrderService(AppDbContext ctx, ICartService cartSvc)` |
| **Scoped Lifetime** | 1 instance/request, chung DbContext | `AddScoped<IOrderService, OrderService>()` |
| **Async All I/O** | Mọi method DB đều async | `ToListAsync()`, `SaveChangesAsync()`, `FindAsync()` |
| **Business Validation** | Kiểm tra logic trước khi ghi | Check stock, check cart empty, check category has products |
| **Exception Propagation** | Service throw → PageModel xử lý | `throw new InvalidOperationException(...)` |
| **Change Tracking** | EF Core tự động detect changes | `product.SoLuongTon -= quantity` → tự động UPDATE |
| **Implicit Transaction** | 1 SaveChangesAsync = 1 transaction | Gộp nhiều changes, commit/rollback atomically |
| **Eager Loading** | Include/ThenInclude tránh N+1 query | `.Include().ThenInclude().ToListAsync()` |
| **Repository Pattern** | Che giấu DbContext khỏi PageModel | PageModel chỉ gọi interface, không biết EF Core |

---

## 5. Admin Pages — Code-behind

### 5.1 DashboardModel

```csharp
// File: Pages/Admin/Dashboard.cshtml.cs

[Authorize(Roles = "Admin")]          // Chỉ Admin mới vào được
public class DashboardModel : PageModel
{
    private readonly AppDbContext _context;    // Direct DI (không qua service)
    private readonly IOrderService _orderService;

    // Properties được set trong OnGetAsync, dùng trong .cshtml
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, int> OrderStatusCounts { get; set; } = new();

    public DashboardModel(AppDbContext context, IOrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    public async Task OnGetAsync()
    {
        // Chạy 5 query song song (không await từng cái)
        TotalProducts = await _context.Products.CountAsync();
        TotalOrders = await _orderService.GetTotalOrdersAsync();
        TotalUsers = await _context.NguoiDung.CountAsync();
        TotalRevenue = await _orderService.GetTotalRevenueAsync();
        OrderStatusCounts = await _orderService.GetOrderStatusCountsAsync();
    }
}
```

**Giải thích:** Dashboard dùng trực tiếp `AppDbContext` cho `Products` và `NguoiDung` (đơn giản chỉ Count), nhưng dùng `IOrderService` cho các thống kê phức tạp hơn.

### 5.2 Products/IndexModel

```csharp
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IProductService _productService;

    public List<Product> Products { get; set; } = new();

    public IndexModel(IProductService productService)
    {
        _productService = productService;  // Inject service
    }

    public async Task OnGetAsync()
    {
        Products = await _productService.GetAllAsync();
    }
}
```

**Đặc điểm:** Chỉ có `OnGetAsync` — GET request tới trang là render luôn.

### 5.3 Products/CreateModel

```csharp
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IProductService _productService;

    public List<Category> Categories { get; set; } = new();

    public CreateModel(IProductService productService)
    {
        _productService = productService;
    }

    // GET: Load dropdown categories
    public async Task OnGetAsync()
    {
        Categories = await _productService.GetAllCategoriesAsync();
    }

    // POST: Xử lý form submit
    public async Task<IActionResult> OnPostAsync(
        string tenSanPham,          // Từ input name="TenSanPham"
        int danhMucId,              // Từ select name="DanhMucId"
        decimal gia,                // Từ input name="Gia"
        int soLuongTon,             // Từ input name="SoLuongTon"
        string? hinhAnhUrl,         // Từ input name="HinhAnhUrl"
        string? moTa)               // Từ textarea name="MoTa"
    {
        // Validation
        if (string.IsNullOrWhiteSpace(tenSanPham))
        {
            ModelState.AddModelError("", "Vui lòng nhập tên sản phẩm.");
            Categories = await _productService.GetAllCategoriesAsync();
            return Page();  // Trả về form với lỗi
        }

        // Map form → Entity
        var product = new Product
        {
            TenSanPham = tenSanPham.Trim(),
            DanhMucId = danhMucId,
            Gia = gia,
            SoLuongTon = soLuongTon,
            HinhAnhUrl = hinhAnhUrl?.Trim(),
            MoTa = moTa?.Trim()
        };

        await _productService.AddAsync(product);

        // Dùng TempData (tồn tại qua redirect)
        TempData["SuccessMessage"] = "Thêm sản phẩm thành công.";
        return RedirectToPage("/Admin/Products/Index");
    }
}
```

**Cơ chế binding:** Razor Pages tự động map `name="TenSanPham"` trong form HTML → tham số `string tenSanPham` của `OnPostAsync`. Đây là **model binding** mặc định của ASP.NET Core.

### 5.4 Products/EditModel

```csharp
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IProductService _productService;

    // Dùng public property → có thể truy cập từ .cshtml
    public Product? Product { get; set; }
    public List<Category> Categories { get; set; } = new();

    public EditModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync(int id)  // id từ query string: ?id=5
    {
        Product = await _productService.GetByIdAsync(id);
        Categories = await _productService.GetAllCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync(
        int id, string tenSanPham, int danhMucId, decimal gia,
        int soLuongTon, string? hinhAnhUrl, string? moTa)
    {
        // Load entity từ DB
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
            return RedirectToPage("/Admin/Products/Index");
        }

        // Update properties (mutation tracking)
        product.TenSanPham = tenSanPham.Trim();
        product.DanhMucId = danhMucId;
        product.Gia = gia;
        product.SoLuongTon = soLuongTon;
        product.HinhAnhUrl = hinhAnhUrl?.Trim();
        product.MoTa = moTa?.Trim();

        await _productService.UpdateAsync(product);
        TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
        return RedirectToPage("/Admin/Products/Index");
    }
}
```

**Giải thích pattern:**
- `OnGetAsync(id)` — Load entity + categories để render form edit với dữ liệu sẵn
- `OnPostAsync(...)` — Load lại entity từ DB (đảm bảo entity được EF Core tracking), chỉnh sửa properties, sau đó `UpdateAsync` sẽ detect changes và gửi `UPDATE` statement tối ưu

### 5.5 Products/DeleteModel

```csharp
[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly IProductService _productService;

    public Product? Product { get; set; }

    public DeleteModel(IProductService productService)
    {
        _productService = productService;
    }

    // GET: Hiển thị thông tin xác nhận xóa
    public async Task OnGetAsync(int id)
    {
        Product = await _productService.GetByIdAsync(id);
    }

    // POST: Thực hiện xóa
    public async Task<IActionResult> OnPostAsync(int id)
    {
        await _productService.DeleteAsync(id);
        TempData["SuccessMessage"] = "Xóa sản phẩm thành công.";
        return RedirectToPage("/Admin/Products/Index");
    }
}
```

**Pattern Confirm Delete:**
- `GET` → load product info, hiển thị form confirm
- `POST` → xóa thật, redirect về danh sách

### 5.6 OrdersModel

```csharp
[Authorize(Roles = "Admin")]
public class OrdersModel : PageModel
{
    private readonly IOrderService _orderService;

    public List<Order> Orders { get; set; } = new();

    public OrdersModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET: List tất cả đơn hàng
    public async Task OnGetAsync()
    {
        Orders = await _orderService.GetAllOrdersAsync();
    }

    // POST: Cập nhật trạng thái đơn hàng
    public async Task<IActionResult> OnPostAsync(int orderId, string trangThai)
    {
        await _orderService.UpdateOrderStatusAsync(orderId, trangThai);
        TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng #{orderId} thành công.";
        return RedirectToPage();  // Redirect về chính trang Orders (không cần đường dẫn)
    }
}
```

**`RedirectToPage()` không tham số:** Chuyển hướng về chính trang hiện tại — tương đương `RedirectToPage("/Admin/Orders")`.

### 5.7 ReportsModel

```csharp
[Authorize(Roles = "Admin")]
public class ReportsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IOrderService _orderService;

    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public List<DeliveredOrderDetail> DeliveredOrderDetails { get; set; } = new();

    public ReportsModel(AppDbContext context, IOrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    public async Task OnGetAsync()
    {
        // 3 thống kê tổng quan
        TotalRevenue = await _orderService.GetTotalRevenueAsync();
        TotalOrders = await _orderService.GetTotalOrdersAsync();
        DeliveredOrders = await _context.Orders
            .CountAsync(o => o.TrangThai == "Đã giao");

        // Chi tiết đơn hàng đã giao (query phức tạp)
        DeliveredOrderDetails = await _context.OrderDetails
            .Include(od => od.DonHang!)
                .ThenInclude(o => o.NguoiDung)
            .Include(od => od.SanPham)
            .Where(od => od.DonHang!.TrangThai == "Đã giao")
            .OrderByDescending(od => od.DonHang!.NgayDat)
            .Select(od => new DeliveredOrderDetail
            {
                OrderId = od.DonHangId,
                CustomerName = od.DonHang!.NguoiDung!.Ho + " " + od.DonHang.NguoiDung.Ten,
                OrderDate = od.DonHang.NgayDat,
                ProductName = od.TenSanPham,
                Quantity = od.SoLuong,
                UnitPrice = od.DonGia,
                TotalPrice = od.SoLuong * od.DonGia
            })
            .ToListAsync();
    }

    // DTO class — không phải entity, không có trong DbContext
    public class DeliveredOrderDetail
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
```

**Giải thích `DeliveredOrderDetail`:** Đây là **ViewModel/ DTO** thuần — không phải entity, không có attribute, không mapping với bảng nào. Dùng để chứa dữ liệu đã được transform (concat họ tên, tính thành tiền) trước khi gửi ra view.

### 5.8 UsersModel

```csharp
[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly AppDbContext _context;

    public List<NguoiDung> Users { get; set; } = new();

    public UsersModel(AppDbContext context)
    {
        _context = context;
    }

    // Chỉ có GET — read-only
    public async Task OnGetAsync()
    {
        Users = await _context.NguoiDung
            .OrderBy(u => u.TenDangNhap)  // Sắp xếp theo tên đăng nhập
            .ToListAsync();
    }
}
```

### 5.9 Categories/IndexModel

```csharp
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public List<Category> Categories { get; set; } = new();

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        Categories = await _context.Categories
            .Include(c => c.Products)          // Lấy cả danh sách sản phẩm để đếm
            .OrderBy(c => c.Ten)               // Sắp xếp theo tên A→Z
            .ToListAsync();
    }
}
```

### 5.10 Categories/EditModel

```csharp
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IProductService _productService;

    public Category? Category { get; set; }

    public EditModel(IProductService productService)
    {
        _productService = productService;
    }

    public async Task OnGetAsync(int id)
    {
        Category = await _productService.GetCategoryByIdAsync(id);
    }

    public async Task<IActionResult> OnPostAsync(int id, string ten, string? moTa)
    {
        var category = await _productService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Danh mục không tồn tại.";
            return RedirectToPage("/Admin/Categories/Index");
        }

        category.Ten = ten.Trim();
        category.MoTa = moTa?.Trim();

        await _productService.UpdateCategoryAsync(category);
        TempData["SuccessMessage"] = "Cập nhật danh mục thành công.";
        return RedirectToPage("/Admin/Categories/Index");
    }
}
```

---

## 6. Admin Views — .cshtml

### 6.1 _AdminLayout.cshtml — Layout chung

```html
<!DOCTYPE html>
<html lang="vi">
<head>
    <!-- Google Fonts: Bodoni Moda (heading) + Outfit (body) -->
    <!-- Bootstrap 5.3.3 + Bootstrap Icons -->
    <!-- /css/admin.css (custom styles) -->
</head>
<body>
    <div class="admin-wrapper">
        <!-- SIDEBAR - luôn hiển thị bên trái -->
        <nav class="admin-sidebar">
            <div class="sidebar-header">
                <a href="/Admin/Dashboard">
                    <h5>Admin</h5>
                    <small>ECommerce</small>
                </a>
            </div>
            <ul class="sidebar-nav">
                <!-- 6 menu chính, active page dùng ViewData["ActivePage"] -->
                <li>
                    <a class="nav-link @(... ViewData["ActivePage"] == "Dashboard" ? "active" : "")"
                       href="/Admin/Dashboard">
                        <i class="bi bi-speedometer2"></i> Dashboard
                    </a>
                </li>
                <!-- Products, Categories, Orders, Users, Reports -->
                <!-- Link về trang người dùng + Đăng xuất -->
            </ul>
        </nav>

        <!-- MAIN CONTENT -->
        <div class="admin-content">
            <!-- Topbar: Title + Tên user + Badge Admin -->
            <nav class="admin-topbar">...</nav>

            <div class="admin-main">
                <!-- TempData messages (success/error) -->
                @if (TempData["SuccessMessage"] != null)
                {
                    <div class="alert-editorial-admin success">
                        @TempData["SuccessMessage"]
                    </div>
                }
                @if (TempData["ErrorMessage"] != null)
                {
                    <div class="alert-editorial-admin error">
                        @TempData["ErrorMessage"]
                    </div>
                }

                @RenderBody()   <!-- Nội dung trang con -->
            </div>
        </div>
    </div>

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

**Cơ chế `TempData`:**
- Dùng `TempData["SuccessMessage"]` trong code-behind
- Hiển thị trong layout (mọi trang Admin đều có)
- Tồn tại qua 1 redirect, sau đó tự động xóa

### 6.2 Dashboard.cshtml

```html
@page
@model ECommerceFinalProject.Pages.Admin.DashboardModel
@{
    ViewData["Title"] = "Dashboard";
    ViewData["ActivePage"] = "Dashboard";   // Để sidebar highlight
}

<!-- 4 Stat Cards -->
<div class="row g-4 mb-4">
    <div class="col-md-3">
        <div class="stat-card stat-blue">
            <div>
                <div class="stat-number">@Model.TotalProducts</div>
                <div class="stat-label">Sản phẩm</div>
            </div>
            <div class="stat-icon"><i class="bi bi-box"></i></div>
        </div>
    </div>
    <!-- Tương tự: TotalOrders (green), TotalUsers (orange), TotalRevenue (red) -->
</div>

<!-- 2 cột: Status counts + Quick actions -->
<div class="row g-4">
    <div class="col-md-6">
        <!-- Bảng trạng thái đơn hàng -->
        <table>
            @foreach (var status in Model.OrderStatusCounts)
            {
                <tr>
                    <td><span class="status-badge status-@class">@status.Key</span></td>
                    <td>@status.Value</td>
                </tr>
            }
        </table>
    </div>
    <div class="col-md-6">
        <!-- Các nút thao tác nhanh -->
        <a href="/Admin/Products/Create">+ Thêm sản phẩm mới</a>
        <a href="/Admin/Categories/Create">+ Thêm danh mục mới</a>
        <a href="/Admin/Orders">Xem đơn hàng mới</a>
    </div>
</div>
```

**CSS Class `@class`:** Dùng ternary operator để chọn class CSS dựa trên tên trạng thái:
```csharp
status.Key == "Chờ xử lý" ? "status-pending" :
status.Key == "Đang giao" ? "status-shipping" :
status.Key == "Đã giao" ? "status-delivered" :
"status-cancelled"
```

### 6.3 Orders.cshtml — Form update inline

```html
@foreach (var order in Model.Orders)
{
    <tr>
        <td>#@order.Id</td>
        <td>@order.NguoiDung?.Ho @order.NguoiDung?.Ten</td>
        <td>@order.NgayDat.ToString("dd/MM/yyyy HH:mm")</td>
        <td>@(order.OrderDetails?.Count ?? 0)</td>
        <td>@order.TongTien.ToString("N0")₫</td>
        <td>
            <span class="status-badge @GetStatusClass(order.TrangThai)">
                @order.TrangThai
            </span>
        </td>
        <td>
            <!-- Form inline: mỗi đơn hàng có 1 form riêng -->
            <form method="post" class="d-flex gap-1">
                <input type="hidden" name="orderId" value="@order.Id" />
                <select name="trangThai">
                    <option value="Chờ xử lý" selected="@(...)">Chờ xử lý</option>
                    <option value="Đang giao" selected="@(...)">Đang giao</option>
                    <option value="Đã giao" selected="@(...)">Đã giao</option>
                    <option value="Đã hủy" selected="@(...)">Đã hủy</option>
                </select>
                <button type="submit"><i class="bi bi-check"></i></button>
            </form>
        </td>
    </tr>
}
```

**Mỗi đơn hàng một form riêng:** Mỗi dòng trong table có `<form method="post">` riêng, với `orderId` là hidden field. Khi submit, chỉ có `orderId` và `trangThai` của đơn hàng đó được gửi lên.

### 6.4 Reports.cshtml — Hiển thị chi tiết đã giao

```html
<!-- 3 Stat Cards: Tổng doanh thu, Tổng đơn hàng, Đơn đã giao -->

<!-- Chi tiết đơn hàng đã giao -->
@if (Model.DeliveredOrderDetails.Any())
{
    <table class="table">
        <thead>
            <tr>
                <th>Mã ĐH</th>
                <th>Khách hàng</th>
                <th>Ngày đặt</th>
                <th>Sản phẩm</th>
                <th>SL</th>
                <th>Đơn giá</th>
                <th>Thành tiền</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var detail in Model.DeliveredOrderDetails)
            {
                <tr>
                    <td>#@detail.OrderId</td>
                    <td>@detail.CustomerName</td>       <!-- Đã ghép Ho + Ten từ query -->
                    <td>@detail.OrderDate.ToString("dd/MM/yyyy")</td>
                    <td>@detail.ProductName</td>
                    <td>@detail.Quantity</td>
                    <td>@detail.UnitPrice.ToString("N0")₫</td>
                    <td>@detail.TotalPrice.ToString("N0")₫</td>  <!-- Đã tính từ query -->
                </tr>
            }
        </tbody>
    </table>
}
```

---

## 📌 Tổng kết các pattern chính

| Pattern | Ví dụ | Mô tả |
|---------|-------|-------|
| **Dependency Injection** | Constructor nhận `IProductService` | ASP.NET Core tự động inject instance |
| **Async/Await** | `await _service.GetAllAsync()` | I/O operations bất đồng bộ, không block thread |
| **TempData** | `TempData["SuccessMessage"]` | Lưu thông báo qua redirect, tự động xóa sau 1 lần đọc |
| **PRG (Post-Redirect-Get)** | `OnPostAsync` → `RedirectToPage` → `OnGetAsync` | Tránh submit trùng khi F5 |
| **Eager Loading** | `.Include().ThenInclude()` | JOIN nhiều bảng, tránh N+1 query |
| **Repository Pattern** | Service layer che giấu DbContext | Pages không biết đến EF Core — chỉ gọi Service |
| **ViewData / ViewBag** | `ViewData["ActivePage"]` | Truyền dữ liệu từ PageModel → Layout |
| **DTO / ViewModel** | `DeliveredOrderDetail` | Class thuần không mapping DB, chứa dữ liệu đã transform |
| **Request Lifecycle** | `OnGetAsync` (GET) / `OnPostAsync` (POST) | Razor Page handler tự động routing dựa trên HTTP method |
