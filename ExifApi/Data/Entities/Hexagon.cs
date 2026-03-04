using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExifApi.Data.Entities;

public class Hexagon
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string H3Index { get; set; } = string.Empty;

    public int Resolution { get; set; }

    // FK to Image
    [ForeignKey("Image")]
    public int? ImageId { get; set; }

    // Navigation property back to Image
    public Image? Image { get; set; } = null!;
}