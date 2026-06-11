using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("DanhMuc")]
public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Tên danh mục")]
    public string Ten { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    // Navigation
    public ICollection<Product>? Products { get; set; }
}
