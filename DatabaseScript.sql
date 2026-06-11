-- ============================================
-- Script tạo database ECommerceDB
-- Dùng cho SQL Server
-- Các thành viên chạy file này trong SSMS
-- trước khi chạy project (hoặc để EF Core tự tạo)
-- ============================================

-- Tạo database nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ECommerceDB')
BEGIN
    CREATE DATABASE ECommerceDB;
END
GO

USE ECommerceDB;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- ============================================
-- TẠO CÁC BẢNG
-- ============================================

-- 1. NguoiDung (Users / Tài khoản)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NguoiDung')
BEGIN
    CREATE TABLE NguoiDung (
        TenDangNhap NVARCHAR(50) PRIMARY KEY,
        MatKhau NVARCHAR(200) NOT NULL,
        Ho NVARCHAR(50) NOT NULL,
        Ten NVARCHAR(50) NOT NULL,
        NgaySinh DATE,
        SoDienThoai NVARCHAR(20),
        Email NVARCHAR(100),
        VaiTro NVARCHAR(20) NOT NULL DEFAULT 'User'
    );

    CREATE UNIQUE INDEX IX_NguoiDung_Email ON NguoiDung(Email) WHERE Email IS NOT NULL;
END
GO

-- 2. DanhMuc (Categories)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DanhMuc')
BEGIN
    CREATE TABLE DanhMuc (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Ten NVARCHAR(100) NOT NULL,
        MoTa NVARCHAR(500)
    );
END
GO

-- 3. SanPham (Products)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SanPham')
BEGIN
    CREATE TABLE SanPham (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenSanPham NVARCHAR(200) NOT NULL,
        MoTa NVARCHAR(2000),
        Gia DECIMAL(18,2) NOT NULL,
        HinhAnhUrl NVARCHAR(500),
        SoLuongTon INT NOT NULL DEFAULT 0,
        DanhMucId INT NOT NULL,
        CONSTRAINT FK_SanPham_DanhMuc FOREIGN KEY (DanhMucId) REFERENCES DanhMuc(Id)
    );
END
GO

-- 4. GioHang (Carts)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GioHang')
BEGIN
    CREATE TABLE GioHang (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap NVARCHAR(50) NOT NULL,
        NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_GioHang_NguoiDung FOREIGN KEY (TenDangNhap) REFERENCES NguoiDung(TenDangNhap) ON DELETE CASCADE
    );
END
GO

-- 5. ChiTietGioHang (CartItems)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChiTietGioHang')
BEGIN
    CREATE TABLE ChiTietGioHang (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        GioHangId INT NOT NULL,
        SanPhamId INT NOT NULL,
        SoLuong INT NOT NULL DEFAULT 1,
        DonGia DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_ChiTietGioHang_GioHang FOREIGN KEY (GioHangId) REFERENCES GioHang(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ChiTietGioHang_SanPham FOREIGN KEY (SanPhamId) REFERENCES SanPham(Id)
    );
END
GO

-- 6. DonHang (Orders)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DonHang')
BEGIN
    CREATE TABLE DonHang (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap NVARCHAR(50) NOT NULL,
        NgayDat DATETIME2 NOT NULL DEFAULT GETDATE(),
        TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
        TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Chờ xử lý',
        DiaChiGiao NVARCHAR(500),
        GhiChu NVARCHAR(1000),
        CONSTRAINT FK_DonHang_NguoiDung FOREIGN KEY (TenDangNhap) REFERENCES NguoiDung(TenDangNhap)
    );
END
GO

-- 7. ChiTietDonHang (OrderDetails)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChiTietDonHang')
BEGIN
    CREATE TABLE ChiTietDonHang (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DonHangId INT NOT NULL,
        SanPhamId INT NOT NULL,
        TenSanPham NVARCHAR(200) NOT NULL,
        SoLuong INT NOT NULL DEFAULT 1,
        DonGia DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_ChiTietDonHang_DonHang FOREIGN KEY (DonHangId) REFERENCES DonHang(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ChiTietDonHang_SanPham FOREIGN KEY (SanPhamId) REFERENCES SanPham(Id)
    );
END
GO

-- ============================================
-- DỮ LIỆU MẪU (SEED DATA)
-- ============================================

-- Tài khoản Admin (username: Admin, password: admin123)
IF NOT EXISTS (SELECT * FROM NguoiDung WHERE TenDangNhap = 'Admin')
BEGIN
    INSERT INTO NguoiDung (TenDangNhap, MatKhau, Ho, Ten, Email, VaiTro)
    VALUES ('Admin', '$2a$11$YktPsQgIbMCpCAXOLw81rO4wwzQm27OAhNVYbdcx6u6ejd.rQ/2ZO', N'Admin', N'System', 'admin@shop.com', 'Admin');
END
GO

-- Danh mục mẫu
IF NOT EXISTS (SELECT * FROM DanhMuc)
BEGIN
    INSERT INTO DanhMuc (Ten, MoTa) VALUES
    (N'Điện thoại', N'Điện thoại thông minh các hãng'),
    (N'Laptop', N'Máy tính xách tay'),
    (N'Phụ kiện', N'Phụ kiện công nghệ'),
    (N'Thời trang', N'Quần áo, giày dép'),
    (N'Đồ gia dụng', N'Đồ dùng gia đình');
END
GO

-- Sản phẩm mẫu
IF NOT EXISTS (SELECT * FROM SanPham)
BEGIN
    INSERT INTO SanPham (TenSanPham, MoTa, Gia, HinhAnhUrl, SoLuongTon, DanhMucId) VALUES
    -- Điện thoại (DanhMucId=1)
    (N'iPhone 15 Pro Max', N'Điện thoại Apple cao cấp nhất', 34990000, '/images/iphone15.jpg', 50, 1),
    (N'Samsung Galaxy S24 Ultra', N'Điện thoại Samsung cao cấp', 27990000, '/images/galaxy.jpg', 40, 1),
    (N'Google Pixel 9 Pro', N'Điện thoại Google với AI thông minh, camera 50MP', 21990000, '/images/pixel9.jpg', 35, 1),
    -- Laptop (DanhMucId=2)
    (N'MacBook Air M3', N'Laptop Apple M3 13 inch', 28990000, '/images/macbookair.jpg', 30, 2),
    (N'Dell XPS 15', N'Laptop Dell cao cấp', 32990000, '/images/dellxps.jpg', 20, 2),
    (N'ASUS ROG Zephyrus G14', N'Laptop gaming AMD Ryzen 9, RTX 4060, 14 inch 2K', 35990000, '/images/asusrog.jpg', 15, 2),
    -- Phụ kiện (DanhMucId=3)
    (N'Tai nghe AirPods Pro 2', N'Tai nghe không dây chống ồn', 5990000, '/images/airpods.jpg', 100, 3),
    (N'Chuột Logitech MX Master 3S', N'Chuột không dây cao cấp', 1990000, '/images/logitech.jpg', 80, 3),
    (N'Sạc dự phòng 20000mAh', N'Pin sạc dự phòng dung lượng cao, hỗ trợ sạc nhanh 65W', 890000, '/images/powerbank.jpg', 200, 3),
    (N'Bàn phím cơ Mechanical', N'Bàn phím cơ RGB switch xanh, dây kéo bọc dù', 1590000, '/images/keyboard.jpg', 60, 3),
    (N'Loa Bluetooth JBL Flip 6', N'Loa di động chống nước, công suất 30W', 2990000, '/images/speaker.jpg', 45, 3),
    -- Thời trang (DanhMucId=4)
    (N'Áo thun nam Basic', N'Áo thun cotton 100% cao cấp, nhiều màu sắc', 299000, '/images/tshirt.jpg', 500, 4),
    (N'Giày thể thao Nike Air', N'Giày chạy bộ Nike Air đế êm, siêu nhẹ', 3290000, '/images/sneakers.jpg', 120, 4),
    (N'Đồng hồ Apple Watch SE', N'Đồng hồ thông minh, đo nhịp tim, GPS', 7990000, '/images/smartwatch.jpg', 70, 4),
    (N'Balo thời trang chống nước', N'Balo chống nước 40L, nhiều ngăn tiện lợi', 690000, '/images/backpack.jpg', 150, 4),
    -- Đồ gia dụng (DanhMucId=5)
    (N'Nồi chiên không dầu Philips', N'Nồi chiên không dầu 6.5L, không khói, ít dầu mỡ', 3590000, '/images/airfryer.jpg', 40, 5),
    (N'Máy xay sinh tố đa năng', N'Máy xay sinh tố 6 lưỡi, 3 tốc độ, cối thủy tinh', 1290000, '/images/blender.jpg', 90, 5),
    (N'Quạt điện cây Panasonic', N'Quạt đứng Panasonic 3 cánh, 4 tốc độ, êm ái', 1590000, '/images/fan.jpg', 65, 5),
    (N'Máy lọc nước RO Kangaroo', N'Máy lọc nước RO 9 lõi, nước nóng nguội', 6990000, '/images/filter.jpg', 25, 5);
END
GO

-- ============================================
-- CẬP NHẬT ẢNH CHO DỮ LIỆU HIỆN TẠI
-- Chạy các lệnh này nếu bạn đã có DB cũ
-- ============================================
-- Cập nhật ảnh sang local images (chạy 1 lần nếu code chưa tự động)
UPDATE SanPham SET HinhAnhUrl = '/images/iphone15.jpg' WHERE Id = 1;
UPDATE SanPham SET HinhAnhUrl = '/images/macbookair.jpg' WHERE Id = 2;
UPDATE SanPham SET HinhAnhUrl = '/images/airpods.jpg' WHERE Id = 3;
UPDATE SanPham SET HinhAnhUrl = '/images/galaxy.jpg' WHERE Id = 4;
UPDATE SanPham SET HinhAnhUrl = '/images/dellxps.jpg' WHERE Id = 5;
UPDATE SanPham SET HinhAnhUrl = '/images/logitech.jpg' WHERE Id = 6;
UPDATE SanPham SET HinhAnhUrl = '/images/pixel9.jpg' WHERE Id = 7;
UPDATE SanPham SET HinhAnhUrl = '/images/asusrog.jpg' WHERE Id = 8;
UPDATE SanPham SET HinhAnhUrl = '/images/powerbank.jpg' WHERE Id = 9;
UPDATE SanPham SET HinhAnhUrl = '/images/keyboard.jpg' WHERE Id = 10;
UPDATE SanPham SET HinhAnhUrl = '/images/speaker.jpg' WHERE Id = 11;
UPDATE SanPham SET HinhAnhUrl = '/images/tshirt.jpg' WHERE Id = 12;
UPDATE SanPham SET HinhAnhUrl = '/images/sneakers.jpg' WHERE Id = 13;
UPDATE SanPham SET HinhAnhUrl = '/images/smartwatch.jpg' WHERE Id = 14;
UPDATE SanPham SET HinhAnhUrl = '/images/backpack.jpg' WHERE Id = 15;
UPDATE SanPham SET HinhAnhUrl = '/images/airfryer.jpg' WHERE Id = 16;
UPDATE SanPham SET HinhAnhUrl = '/images/blender.jpg' WHERE Id = 17;
UPDATE SanPham SET HinhAnhUrl = '/images/fan.jpg' WHERE Id = 18;
UPDATE SanPham SET HinhAnhUrl = '/images/filter.jpg' WHERE Id = 19;
GO

PRINT N'✅ Database ECommerceDB đã được tạo thành công!';
GO
