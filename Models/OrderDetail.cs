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
