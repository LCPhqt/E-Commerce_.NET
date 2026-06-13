using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceFinalProject.Models;

[Table("XacThucEmail")]
public class XacThucEmail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string MaXacThuc { get; set; } = string.Empty;

    [Required]
    public DateTime ThoiGianGui { get; set; }

    [Required]
    public DateTime ThoiGianHetHan { get; set; }

    public bool DaSuDung { get; set; } = false;
}
