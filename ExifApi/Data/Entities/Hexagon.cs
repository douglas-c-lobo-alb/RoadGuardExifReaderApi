using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using H3Standard;

namespace ExifApi.Data.Entities;

public class Hexagon
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string H3Index { get; set; } = string.Empty;
    // Resolution is encoded inside the H3 index -- derived, not stored
    // Denotes that a property or class should be excluded from database mapping.
    [NotMapped]
    public int Resolution => string.IsNullOrEmpty(H3Index)
        ? -1
        : H3Net.GetResolution(H3Net.StringToH3(H3Index));
    // FK to Image
    [ForeignKey("Image")]
    public int? ImageId { get; set; }
    // Navigation property back to Image
    public Image? Image { get; set; } = null!;
}