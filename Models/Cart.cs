using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("GioHang")]
public class Cart
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(TenDangNhap))]
    public NguoiDung? NguoiDung { get; set; }

    public ICollection<CartItem>? CartItems { get; set; }
}
