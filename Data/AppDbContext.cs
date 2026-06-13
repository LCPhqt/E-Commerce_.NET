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
    public DbSet<XacThucEmail> XacThucEmail { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // NguoiDung
        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.TenDangNhap);
            entity.Property(e => e.TenDangNhap).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MatKhau).IsRequired();
            entity.Property(e => e.Ho).HasMaxLength(100).IsRequired().IsUnicode(true);
            entity.Property(e => e.Ten).HasMaxLength(100).IsRequired().IsUnicode(true);
            entity.Property(e => e.NgaySinh).HasColumnType("date");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20).IsUnicode(true);
            entity.Property(e => e.DiaChi).HasMaxLength(500).IsUnicode(true);
            entity.Property(e => e.Email).HasMaxLength(255).IsUnicode(true);
            entity.Property(e => e.VaiTro).HasMaxLength(20).HasDefaultValue("User").IsUnicode(true);
            entity.Property(e => e.DaXacThuc).HasDefaultValue(false);

            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ten).HasMaxLength(100).IsRequired().IsUnicode(true);
            entity.Property(e => e.MoTa).HasMaxLength(500).IsUnicode(true);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenSanPham).HasMaxLength(200).IsRequired().IsUnicode(true);
            entity.Property(e => e.MoTa).HasColumnType("nvarchar(max)").IsUnicode(true);
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
            entity.Property(e => e.TrangThai).HasMaxLength(50).IsRequired().IsUnicode(true);
            entity.Property(e => e.DiaChiGiao).HasMaxLength(500).IsRequired().IsUnicode(true);
            entity.Property(e => e.GhiChu).HasMaxLength(500).IsUnicode(true);

            entity.HasOne(e => e.NguoiDung)
                  .WithMany()
                  .HasForeignKey(e => e.TenDangNhap)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderDetail
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenSanPham).HasMaxLength(200).IsRequired().IsUnicode(true);
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

        // XacThucEmail
        modelBuilder.Entity<XacThucEmail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenDangNhap).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MaXacThuc).HasMaxLength(10).IsRequired();
            entity.Property(e => e.ThoiGianGui).IsRequired();
            entity.Property(e => e.ThoiGianHetHan).IsRequired();
            entity.Property(e => e.DaSuDung).HasDefaultValue(false);
        });
    }
}
