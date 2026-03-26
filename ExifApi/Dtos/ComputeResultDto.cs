namespace ExifApi.Dtos;

public record ComputeResultDto(
    int AnomaliesCreated,
    int AnomaliesReopened,
    int AnomaliesUpdated,
    int VotesDeleted);