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
