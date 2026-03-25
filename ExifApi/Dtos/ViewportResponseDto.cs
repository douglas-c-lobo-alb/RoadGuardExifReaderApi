namespace ExifApi.Dtos;

public class ViewportResponseDto
{
    public List<HexagonViewDto> ImageHexagons { get; set; } = [];
    public List<AnomalyHexagonViewDto> AnomalyHexagons { get; set; } = [];
}
