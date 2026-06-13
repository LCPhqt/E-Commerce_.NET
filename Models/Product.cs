using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("SanPham")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Tên sản phẩm")]
    public string TenSanPham { get; set; } = string.Empty;

    [Display(Name = "Mô tả chi tiết")]
    public string? MoTa { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Giá")]
    public decimal Gia { get; set; }

    [MaxLength(500)]
    [Display(Name = "Hình ảnh URL")]
    public string? HinhAnhUrl { get; set; }

    [Required]
    [Display(Name = "Số lượng tồn")]
    public int SoLuongTon { get; set; } = 0;

    [Required]
    [Display(Name = "Danh mục")]
    public int DanhMucId { get; set; }

    // Navigation
    [ForeignKey(nameof(DanhMucId))]
    public Category? DanhMuc { get; set; }
}
