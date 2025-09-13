namespace datopus.Application.DTOs;

public class RrwebSnapshotBlob
{
    public required DateOnly Date { get; init; }

    public required string SessionId { get; set; }

    public required string MeasurementId { get; set; }

    public required byte[] CompressedData { get; set; }
}
