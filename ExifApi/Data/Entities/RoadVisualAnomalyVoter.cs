using System;
using System.ComponentModel.DataAnnotations;

namespace ExifApi.Data.Entities;

public class Class
{
    [Key]
    public Guid Id { get; set;}
    public int? ImageId { get; set; }
    public Image? Image { get; set; }
    public int? HexagonId { get; set;}
    public Hexagon? Hexagon { get; set; }
    public DateTime CreatedDate { get; set; }
    public AnomalyType AnomalyType { get; set; }
    public decimal Confidence { get; set; }
    public int? RoadViewId { get; set;}
    public RoadView? RoadView { get; set; }
    public ((int StartingX, int StartingY), (int EndingX, int EndingY)) Rectangle { get; set;}
}
