using System.Text.Json.Serialization;
using datopus.Core.Entities;
using datopus.Core.Entities.BigQuery;

namespace datopus.Api.DTOs;

public class BQQuery
{
    [JsonPropertyName("dimensions")]
    public BQDimension[] Dimensions { get; set; } = Array.Empty<BQDimension>();

    [JsonPropertyName("metrics")]
    public BQMetric[] Metrics { get; set; } = Array.Empty<BQMetric>();

    [JsonPropertyName("dateRange")]
    public required DateRange DateRange { get; set; }

    [JsonPropertyName("metricFilter")]
    public BQFilterExpression? MetricFilter { get; set; }

    [JsonPropertyName("dimensionFilter")]
    public BQFilterExpression? DimensionFilter { get; set; }

    [JsonPropertyName("orderBys")]
    public BQOrderBy[]? OrderBys { get; set; }
}
