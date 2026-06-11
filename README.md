# 🛒 ECommerceFinalProject — Trang Thương Mại Điện Tử ASP.NET Core

Đồ án cuối kỳ — Nền tảng thương mại điện tử xây dựng với **ASP.NET Core Razor Pages (.NET 10)**, **Entity Framework Core 8**, và **SQL Server**.

---

## 📋 Tổng Quan

Hệ thống gồm **2 khu vực chính**:
- **🛍️ Khu vực Customer** — Xem sản phẩm, giỏ hàng, đặt hàng
- **⚙️ Khu vực Admin** — Quản trị sản phẩm, danh mục, đơn hàng, người dùng, thống kê

### Tính Năng Chính

| # | Tính năng | Mô tả |
|---|-----------|-------|
| 1 | Hiển thị sản phẩm | Danh sách sản phẩm kèm tìm kiếm, lọc theo danh mục |
| 2 | Chi tiết sản phẩm | Xem ảnh, giá, mô tả, số lượng tồn |
| 3 | Giỏ hàng | Thêm/xoá/sửa số lượng, tự động tính tổng tiền |
| 4 | Đặt hàng | Checkout với địa chỉ giao hàng, xác nhận đơn hàng |
| 5 | Lịch sử đơn hàng | Xem trạng thái đơn hàng (Chờ xử lý → Đang giao → Đã giao → Đã huỷ) |
| 6 | Admin Dashboard | Thống kê tổng quan: sản phẩm, đơn hàng, người dùng, doanh thu |
| 12 | Ảnh sản phẩm local | Lưu trong `wwwroot/images/`, tự động cập nhật DB khi chạy app |
| 7 | Admin CRUD | Quản lý sản phẩm, danh mục đầy đủ Thêm/Sửa/Xoá |
| 8 | Admin Orders | Cập nhật trạng thái đơn hàng |
| 9 | Admin Users | Xem danh sách người dùng |
| 10 | Admin Reports | Thống kê doanh thu chi tiết |
| 11 | Phân quyền | Admin/User — User không vào được trang Admin |

---

## 🏗️ Kiến Trúc

```
ECommerceFinalProject/
├── Data/
│   └── AppDbContext.cs            # DbContext — 7 DbSets
├── Models/
│   ├── NguoiDung.cs               # Người dùng (PK: TenDangNhap)
│   ├── Category.cs                # Danh mục sản phẩm
│   ├── Product.cs                 # Sản phẩm
│   ├── Cart.cs                    # Giỏ hàng
│   ├── CartItem.cs                # Chi tiết giỏ hàng
│   ├── Order.cs                   # Đơn hàng
│   └── OrderDetail.cs             # Chi tiết đơn hàng
├── Services/
│   ├── IProductService.cs         # Interface
│   ├── ProductService.cs          # CRUD sản phẩm + danh mục
│   ├── ICartService.cs
│   ├── CartService.cs             # Quản lý giỏ hàng (DB)
│   ├── IOrderService.cs
│   └── OrderService.cs            # Tạo đơn, quản lý trạng thái, thống kê
├── Pages/
│   ├── Index.cshtml               # Trang chủ
│   ├── AccessDenied.cshtml        # 403
│   ├── Shared/
│   │   ├── _Layout.cshtml         # Layout chính
│   │   ├── _CustomerLayout.cshtml # Layout Customer
│   │   └── _AdminLayout.cshtml    # Layout Admin (sidebar)
│   ├── Account/
│   │   ├── Login.cshtml           # Đăng nhập
│   │   ├── Register.cshtml        # Đăng ký
│   │   └── Logout.cshtml          # Đăng xuất
│   ├── Customer/
│   │   ├── Home.cshtml            # Trang chủ Customer
│   │   ├── Products.cshtml        # Danh sách sản phẩm + lọc
│   │   ├── ProductDetails.cshtml  # Chi tiết sản phẩm
│   │   ├── Cart.cshtml            # Giỏ hàng
│   │   ├── Checkout.cshtml        # Thanh toán
│   │   └── Orders.cshtml          # Lịch sử đơn hàng
│   └── Admin/
│       ├── Dashboard.cshtml       # Thống kê tổng quan
│       ├── Products/              # CRUD sản phẩm
│       ├── Categories/            # CRUD danh mục
│       ├── Orders.cshtml          # Quản lý đơn hàng
│       ├── Users.cshtml           # Danh sách người dùng
│       └── Reports.cshtml         # Báo cáo doanh thu
├── wwwroot/css/
│   ├── customer.css               # CSS khu vực Customer
│   └── admin.css                  # CSS khu vực Admin
├── wwwroot/images/
│   ├── iphone15.jpg               # Ảnh sản phẩm (local, free từ Unsplash)
│   ├── macbookair.jpg
│   ├── airpods.jpg
│   ├── galaxy.jpg
│   ├── dellxps.jpg
│   └── logitech.jpg
├── Program.cs                     # Cấu hình ứng dụng
├── DatabaseScript.sql             # Script tạo DB + seed data
└── appsettings.json               # Connection string
```

### Công Nghệ Sử Dụng

| Công nghệ | Phiên bản | Mục đích |
|-----------|-----------|----------|
| ASP.NET Core | 10.0 | Web framework |
| Razor Pages | 10.0 | UI pattern |
| Entity Framework Core | 8.0.0 | ORM |
| SQL Server | — | Cơ sở dữ liệu |
| BCrypt.Net-Next | 4.0.3 | Mã hoá mật khẩu |
| Bootstrap | 5 (CDN) | Giao diện responsive |
| Bootstrap Icons | — | Icon |

---

## 🗄️ Cơ Sở Dữ Liệu

### Các Bảng

| Bảng (Table) | Model | Mô tả |
|--------------|-------|-------|
| `NguoiDung` | Người dùng | PK: `TenDangNhap` (string), có cột `VaiTro` (Admin/User) |
| `DanhMuc` | Danh mục | Phân loại sản phẩm |
| `SanPham` | Sản phẩm | FK → DanhMuc |
| `GioHang` | Giỏ hàng | FK → NguoiDung (ON DELETE CASCADE) |
| `ChiTietGioHang` | Chi tiết giỏ | FK → GioHang (CASCADE), FK → SanPham (RESTRICT) |
| `DonHang` | Đơn hàng | FK → NguoiDung (RESTRICT) |
| `ChiTietDonHang` | Chi tiết đơn | FK → DonHang (CASCADE), FK → SanPham (RESTRICT) |

### Sơ Đồ Quan Hệ

```
NguoiDung (1) ────< (N) GioHang (1) ────< (N) ChiTietGioHang (N) >──── (1) SanPham
    │                                                                    │
    └──────────────────────── (1) DonHang ────< (N) ChiTietDonHang >────┘
                                                                         
DanhMuc (1) ────< (N) SanPham
```

### Seed Data (Mẫu)

- **Tài khoản Admin**: `Admin` / `admin123`
- **5 danh mục**: Điện thoại, Laptop, Phụ kiện, Thời trang, Đồ gia dụng
- **6 sản phẩm**: iPhone 15 Pro Max, MacBook Air M3, AirPods Pro 2, Galaxy S24 Ultra, Dell XPS 15, Logitech MX Master 3S
- **Ảnh sản phẩm**: Ảnh thật từ Unsplash, lưu tại `wwwroot/images/` — không phụ thuộc Internet, không die link

---

## 🚀 Hướng Dẫn Cài Đặt

### Yêu Cầu

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Developer, hoặc Express)
- SQL Server đang chạy (dịch vụ `MSSQLSERVER`)

### Các Bước

#### 1. Clone project

```bash
git clone <url>
cd ck_Net
```

#### 2. Cấu hình Connection String

Mở `appsettings.json` và kiểm tra:

```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ECommerceDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

> Thay đổi `Server` và `Trusted_Connection` theo cấu hình SQL Server của bạn nếu cần.

#### 3. Build & Chạy

```bash
dotnet build
dotnet run
```

Ứng dụng sẽ chạy tại: **http://localhost:5000**

> **Lưu ý**: 
> - CSDL tự động được tạo khi chạy lần đầu (`EnsureCreated()`).
> - Ảnh sản phẩm tự động được cập nhật URL local mỗi khi chạy app (không cần thao tác thủ công).
> - Nếu đã có DB cũ, xoá thủ công qua SSMS trước khi chạy.

#### 4. (Không bắt buộc) Chạy Script SQL

Nếu muốn tạo DB thủ công hoặc seed data, chạy `DatabaseScript.sql` trong SQL Server Management Studio (SSMS).

---

## 🔐 Tài Khoản Mặc Định

| Vai trò | Tên đăng nhập | Mật khẩu |
|---------|---------------|----------|
| 👑 **Admin** | `Admin` | `admin123` |
| 👤 User | Đăng ký mới | — |

---

## 🧭 Hướng Dẫn Sử Dụng

### Khu vực Customer (`/Customer/`)

1. **Trang chủ** (`/Customer/Home`) — Xem sản phẩm nổi bật
2. **Sản phẩm** (`/Customer/Products`) — Tìm kiếm, lọc danh mục
3. **Chi tiết** — Click vào sản phẩm để xem + thêm giỏ hàng
4. **Giỏ hàng** (`/Customer/Cart`) — Điều chỉnh số lượng, thanh toán
5. **Đặt hàng** — Nhập địa chỉ giao hàng, xác nhận
6. **Đơn hàng** (`/Customer/Orders`) — Xem lịch sử và trạng thái

### Khu vực Admin (`/Admin/`)

1. **Dashboard** (`/Admin/Dashboard`) — Thống kê tổng quan
2. **Sản phẩm** (`/Admin/Products`) — Thêm/Sửa/Xoá sản phẩm
3. **Danh mục** (`/Admin/Categories`) — Thêm/Sửa/Xoá danh mục
4. **Đơn hàng** (`/Admin/Orders`) — Xem + cập nhật trạng thái
5. **Người dùng** (`/Admin/Users`) — Xem danh sách
6. **Báo cáo** (`/Admin/Reports`) — Thống kê doanh thu

---

## 🧪 Kiểm Tra

### Test nhanh bằng PowerShell

```powershell
# Test trang chủ
Invoke-WebRequest -Uri http://localhost:5000/Customer/Home

# Test API sản phẩm
Invoke-WebRequest -Uri http://localhost:5000/Customer/Products
```

### Luồng test cơ bản

1. ✅ Vào `http://localhost:5000` → thấy trang chủ
2. ✅ Vào `/Customer/Products` → xem danh sách sản phẩm
3. ✅ Click sản phẩm → xem chi tiết (kèm ảnh sản phẩm thật)
4. ✅ Đăng ký tài khoản mới tại `/Account/Register`
5. ✅ Đăng nhập → thêm sản phẩm vào giỏ
6. ✅ Vào giỏ hàng → tăng/giảm số lượng
7. ✅ Thanh toán → nhập địa chỉ → đặt hàng
8. ✅ Xem lịch sử đơn hàng
9. ✅ Đăng nhập `Admin`/`admin123` → vào `/Admin/Dashboard`
10. ✅ Admin thêm/sửa/xoá sản phẩm, danh mục
11. ✅ Admin cập nhật trạng thái đơn hàng
12. ✅ Admin xem báo cáo doanh thu

---

## ❌ Lỗi Thường Gặp

| Vấn đề | Nguyên nhân | Giải pháp |
|--------|-------------|-----------|
| `HTTP 400` khi thêm giỏ hàng | Thiếu Anti-Forgery Token | Build lại (đã fix) hoặc `Ctrl+F5` |
| `FK conflict GioHang` | Claim `Name` không phải username | Đăng xuất → đăng nhập lại |
| `Cannot open database` | SQL Server chưa chạy | Bật dịch vụ `MSSQLSERVER` |
| `Build failed — file locked` | App cũ còn chạy | `powershell \"Get-Process -Name ECommerceFinalProject,temp_hash \| Stop-Process -Force\"` |
| `address already in use` | Port 5000 bị chiếm | `netstat -ano \| findstr :5000` → tìm PID → `taskkill /PID <PID> /F` |

---

## 📁 Script SQL

File `DatabaseScript.sql` có thể dùng để:
- Tạo database + bảng thủ công trong SSMS
- Seed dữ liệu mẫu
- Chia sẻ cho các thành viên khác trong nhóm

```sql
-- Chạy trong SSMS:
EXECUTE DatabaseScript.sql;  -- Mở file và nhấn Execute (F5)
```

---

## 👥 Thành Viên

| Thành viên | Phân công |
|------------|-----------|
| Member 1 | Hiển thị sản phẩm Customer |
| Member 2 | Tài khoản, Auth, Phân quyền |
| Member 3 | Giỏ hàng, Đặt hàng, Đơn hàng |
| Member 4 | Admin CRUD, Dashboard, Thống kê |

---

## 📝 Giấy Phép

Đồ án cuối kỳ — Sử dụng cho mục đích học tập.
