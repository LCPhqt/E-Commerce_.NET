using Microsoft.EntityFrameworkCore;
using ECommerceFinalProject.Models;

namespace ECommerceFinalProject.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<NguoiDung> NguoiDung { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
