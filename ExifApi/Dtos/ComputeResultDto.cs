namespace ExifApi.Dtos;

public record ComputeResultDto(
    int AnomaliesCreated,
    int AnomaliesReopened,
    int VotesDeleted);