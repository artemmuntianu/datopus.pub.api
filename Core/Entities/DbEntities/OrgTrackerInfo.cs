using System.Text.Json.Serialization;

public class OrgTrackerInfo
{
    [JsonPropertyName("s_lookup_key")]
    public required string LookupKey { get; set; }

    [JsonPropertyName("s_status")]
    public required string SubscriptionStatus { get; set; }

    [JsonPropertyName("s_mtu_limit_exceeded")]
    public bool? MtuLimitExceeded { get; set; }

    [JsonPropertyName("measurement_id")]
    public string? MeasurementId { get; set; }

    [JsonPropertyName("bq_source_exist")]
    public required bool BQSourceExists { get; set; }
}
