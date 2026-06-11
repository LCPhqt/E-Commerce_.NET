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
    (N'iPhone 15 Pro Max', N'Điện thoại Apple cao cấp nhất', 34990000, '/images/iphone15.jpg', 50, 1),
    (N'MacBook Air M3', N'Laptop Apple M3 13 inch', 28990000, '/images/macbookair.jpg', 30, 2),
    (N'Tai nghe AirPods Pro 2', N'Tai nghe không dây chống ồn', 5990000, '/images/airpods.jpg', 100, 3),
    (N'Samsung Galaxy S24 Ultra', N'Điện thoại Samsung cao cấp', 27990000, '/images/galaxy.jpg', 40, 1),
    (N'Dell XPS 15', N'Laptop Dell cao cấp', 32990000, '/images/dellxps.jpg', 20, 2),
    (N'Chuột Logitech MX Master 3S', N'Chuột không dây cao cấp', 1990000, '/images/logitech.jpg', 80, 3);
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
GO

PRINT N'✅ Database ECommerceDB đã được tạo thành công!';
GO
