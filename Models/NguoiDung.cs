using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("NguoiDung")]
public class NguoiDung
{
    [Key]
    [MaxLength(50)]
    [Required]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    public string MatKhau { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required]
    public string Ho { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required]
    public string Ten { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime? NgaySinh { get; set; }

    [MaxLength(20)]
    public string? SoDienThoai { get; set; }

    [MaxLength(500)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Display(Name = "Vai trò")]
    public string VaiTro { get; set; } = "User";

    public bool DaXacThuc { get; set; } = false;

    [MaxLength(255)]
    [Display(Name = "Ảnh đại diện")]
    public string? AvatarUrl { get; set; }
}
