using System.Text.Json.Serialization;
using datopus.Core.Entities;

public class RRWebEventPayloadDTO
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("measurement_id")]
    public string? MeasurementId { get; init; }

    [JsonPropertyName("dates_events")]
    public Dictionary<DateTime, string[]>? RRWebEventsDictionaryPerDate { get; init; }
}

public class RecordedSessionsListPayloadDTO
{
    [JsonPropertyName("date_range")]
    public DateRange? DateRange { get; set; }

    [JsonPropertyName("project_id")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("dataset_id")]
    public string? DatasetId { get; set; }
}

public class RecordedSessionDetailsPayloadDTO
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("project_id")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("date_range")]
    public DateRange? DateRange { get; set; }

    [JsonPropertyName("dataset_id")]
    public string? DatasetId { get; set; }
}

public class GetBlobSnapshotsPayloadDTO
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("date_range")]
    public DateRange? DateRange { get; set; }

    [JsonPropertyName("measurement_id")]
    public string? MeasurementId { get; init; }
}
