namespace ExifApi.Dtos;

public class ViewImageDto
{
    public int Id { get; set; }
    public string? FilePath { get; set; }
    public DateTime? DateTaken { get; set; }
    public string? AnomalyNotes { get; set; }
}
