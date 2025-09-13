namespace datopus.Core.Entities.BigQuery;

public abstract class BQBuilder
{
    public abstract string CreateQuery(
        BQMetric[] metrics,
        BQDimension[] dimensions,
        DateRange range,
        BQFilterExpression? metricFilter,
        BQFilterExpression? dimensionFilter,
        BQOrderBy[]? orderBys,
        string projectId,
        string propertyId,
        string tableId
    );
}
