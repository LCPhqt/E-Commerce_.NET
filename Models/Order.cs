using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

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
    public DateTime NgayDat { get; set; } = DateTime.Now;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Tổng tiền")]
    public decimal TongTien { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = "Chờ xử lý";

    [Required]
    [MaxLength(500)]
    [Display(Name = "Địa chỉ giao hàng")]
    public string DiaChiGiao { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    // Navigation
    [ForeignKey(nameof(TenDangNhap))]
    public NguoiDung? NguoiDung { get; set; }

    public ICollection<OrderDetail>? OrderDetails { get; set; }
}
