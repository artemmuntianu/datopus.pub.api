using System.Text;
using datopus.Core.Entities;
using datopus.Core.Entities.BigQuery;
using datopus.Core.Entities.BigQuery.Constants;
using datopus.Core.Exceptions;

namespace datopus.Core.Services.BigQuery;

class QueryBuilderResult
{
    public List<string> Inners { get; set; } = new();
    public List<string> Outers { get; set; } = new();
    public List<string> InnerNames { get; set; } = new();
    public List<string> OuterNames { get; set; } = new();
}

public class GoogleSqlBQBuilderService : BQBuilder
{
    private readonly int MaxFilterExpressionDeepnessLevel = 20;

    public override string CreateQuery(
        BQMetric[] metrics,
        BQDimension[] dimensions,
        DateRange range,
        BQFilterExpression? metricFilter,
        BQFilterExpression? dimensionFilter,
        BQOrderBy[]? orderBys,
        string projectId,
        string propertyId,
        string tableId
    )
    {
        var (inners, outers, innerNames, outerNames) = ProcessDimensions(dimensions);
        (inners, outers, innerNames) = ProcessMetrics(metrics, inners, outers, innerNames);

        string source = Source(projectId, propertyId, tableId);
        string timeFilterQuery = Time(range.Start, range.End);
        string? dimensionFilterQuery = ProcessFilterExpression(dimensionFilter, 0);
        string whereClause = Where(timeFilterQuery, dimensionFilterQuery);
        string groupByInner = Groups(innerNames);
        string groupByOuter = Groups(outerNames);

        string innerSelect = InnerSelect(source, inners.ToArray(), whereClause, groupByInner);
        string outerSelect = OuterSelect(innerSelect, outers.ToArray(), groupByOuter);
        string? metricFilterQuery = ProcessFilterExpression(metricFilter, 0);
        string orderBysQuery = ProcessOrderBys(orderBys);

        return $"{outerSelect} {(string.IsNullOrWhiteSpace(metricFilterQuery) ? "" : $"HAVING {metricFilterQuery}")} {orderBysQuery};";
    }

    private string? ProcessFilterExpression(BQFilterExpression? expression, int level)
    {
        level += 1;

        if (level > this.MaxFilterExpressionDeepnessLevel)
        {
            throw new BaseException("Max level of filter expression deepness has been reached");
        }

        return expression switch
        {
            { AndGroup: not null } => ProcessLogicalGroup(expression.AndGroup, "AND", level),
            { OrGroup: not null } => ProcessLogicalGroup(expression.OrGroup, "OR", level),
            { Filter: not null } => ProcessFilter(expression.Filter),
            _ => null,
        };
    }

    private string ProcessLogicalGroup(BQFilterExpressionList group, string operatorType, int level)
    {
        var expressions = group
            .Expressions.Select(e => ProcessFilterExpression(e, level))
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();

        if (expressions.Count == 0)
        {
            return "";
        }

        if (expressions.Count == 1)
        {
            return expressions[0]!;
        }

        return $"({string.Join($" {operatorType} ", expressions)})";
    }

    private string ProcessOrderBys(BQOrderBy[]? orderBys)
    {
        if (orderBys == null || orderBys.Length == 0)
            return "";
        return $"ORDER BY {string.Join(",", orderBys.Select((orderBy) => $"{NormalizeName(orderBy.FieldName)} {(orderBy.Desc == true ? "DESC" : "ASC")}"))}";
    }

    private string ProcessFilter(BQFilter filter)
    {
        if (filter.StringFilter != null)
        {
            return ProcessStringFilter(filter.StringFilter, filter.FieldName, filter.Custom);
        }
        else if (filter.NumericFilter != null)
        {
            return ProcessNumericFilter(filter.NumericFilter, filter.FieldName);
        }
        else if (filter.InListFilter != null)
        {
            throw new BaseException("Not supported type of big query filter");
        }
        else if (filter.BetweenFilter != null)
        {
            throw new BaseException("Not supported type of big query filter yet");
        }
        else
        {
            throw new BaseException("Big query filter doesn't contain any valid payload yet");
        }
    }

    private string ProcessStringFilter(BQStringFilter filter, string fieldName, bool isCustom)
    {
        if (isCustom)
        {
            return $@"
        EXISTS (
            SELECT 1
            FROM UNNEST(event_params)
            WHERE KEY = '{fieldName}' 
            AND {BuildStringFilterQuery(filter, "value.string_value")}
        )";
        }
        {
            return BuildStringFilterQuery(filter, fieldName);
        }
    }

    private string ProcessNumericFilter(BQNumericFilter filter, string fieldName)
    {
        return BuildNumericFilterQuery(filter, fieldName);
    }

    private string BuildStringFilterQuery(BQStringFilter filter, string fieldName)
    {
        switch (filter.MatchType)
        {
            case BQMatchType.Exact:
            {
                return $"{fieldName} = '{filter.Value}'";
            }
            case BQMatchType.NotExact:
            {
                return $"{fieldName} != '{filter.Value}'";
            }
            case BQMatchType.MatchRegex:
            {
                return $"REGEXP_CONTAINS({fieldName}, r'{filter.Value}')";
            }

            case BQMatchType.NotMatchRegex:
            {
                return $"NOT REGEXP_CONTAINS({fieldName}, r'{filter.Value}')";
            }
            case BQMatchType.BeginsWith:
            {
                return $"{fieldName} LIKE '{filter.Value}%'";
            }
            case BQMatchType.Contains:
            {
                return $"{fieldName} LIKE '%{filter.Value}%'";
            }
            case BQMatchType.EndsWith:
            {
                return $"{fieldName} LIKE '%{filter.Value}'";
            }
            default:
            {
                throw new BaseException("Invalid string filter match type");
            }
        }
    }

    private string BuildNumericFilterQuery(BQNumericFilter filter, string fieldName)
    {
        switch (filter.Operation)
        {
            case BQOperation.Equal:
            {
                return $"{fieldName} = {filter.Value}";
            }
            case BQOperation.NotEqual:
            {
                return $"{fieldName} != {filter.Value}";
            }
            case BQOperation.GreaterThan:
            {
                return $"{fieldName} > {filter.Value}";
            }
            case BQOperation.GreaterThanOrEqual:
            {
                return $"{fieldName} >= {filter.Value}";
            }
            case BQOperation.LessThan:
            {
                return $"{fieldName} < {filter.Value}";
            }
            case BQOperation.LessThanOrEqual:
            {
                return $"{fieldName} <= {filter.Value}";
            }
            default:
            {
                throw new BaseException("Invalid mumeric filter operation");
            }
        }
    }

    private string InnerSelect(
        string property,
        string[] statements,
        string? where,
        string? groups
    ) =>
        $@"SELECT {string.Join(", ", statements)}
        FROM `{property}`
        {where ?? ""}
        {groups ?? ""}
        ";

    private string OuterSelect(string innerSelect, string[] statements, string? groups) =>
        $@"
        SELECT {string.Join(", ", statements)}
        FROM ({innerSelect})
        {groups ?? ""}
        ";

    private string Time(DateTime start, DateTime end)
    {
        return $"_TABLE_SUFFIX BETWEEN '{start:yyyyMMdd}' AND '{end:yyyyMMdd}'";
    }

    private string Groups(List<string> names)
    {
        if (names.Count == 0)
            return "";

        return @$"
           GROUP BY {string.Join(", ", names)}
        ";
    }

    private string Source(string projectId, string propertyId, string tableId)
    {
        return $"{projectId}.{propertyId}.{tableId}_*";
    }

    private string Where(string time, string? dimensionFiltersQuery)
    {
        var sb = new StringBuilder("WHERE\n");
        sb.AppendLine(time);
        if (!string.IsNullOrWhiteSpace(dimensionFiltersQuery))
            sb.AppendLine($"AND {dimensionFiltersQuery}");

        return sb.ToString();
    }

    private BQStatement MetricParse(BQMetric metric)
    {
        if (MetricParsers.TryGetValue(metric.ApiName, out var statement))
        {
            return statement;
        }

        throw new UnsupportedMetricException(metric.ApiName);
    }

    private string NormalizeName(string apiName)
    {
        return apiName.Replace(".", "__");
    }

    private BQStatement DimensionParse(BQDimension dimension)
    {
        var name = NormalizeName(dimension.ApiName);

        if (dimension.Custom)
        {
            return new BQStatement
            {
                Groupable = true,
                Inner =
                    $@"
            (
            SELECT
            value.string_value
            FROM
            UNNEST (event_params)
            WHERE
            KEY = '{dimension.ApiName}'
            ) AS {name}
            ",
                Outer = name,
                Name = name,
                PseudoName = name,
            };
        }
        else
        {
            var key =
                dimension.ApiName == "event_date"
                    ? "PARSE_DATE('%Y%m%d', event_date)"
                    : dimension.ApiName;

            return new BQStatement
            {
                Groupable = true,
                Inner = $"{key} AS {name}",
                Outer = name,
                Name = name,
                PseudoName = name,
            };
        }
    }

    private static readonly Dictionary<string, BQStatement> MetricParsers = new()
    {
        { "sessions", BQMetricSqlDefinitions.Sessions },
        { "revenue", BQMetricSqlDefinitions.Revenue },
        { "events", BQMetricSqlDefinitions.Events },
        { "users", BQMetricSqlDefinitions.Users },
    };

    private (
        List<string> inners,
        List<string> outers,
        List<string> innerNames,
        List<string> outerNames
    ) ProcessDimensions(BQDimension[] dimensions)
    {
        var result = dimensions.Aggregate(
            new
            {
                Inners = new List<string>(),
                Outers = new List<string>(),
                InnerNames = new List<string>(),
                OuterNames = new List<string>(),
            },
            (curr, next) =>
            {
                var statement = DimensionParse(next);
                curr.Inners.Add(statement.Inner);
                curr.Outers.Add(statement.Outer);

                if (statement.Groupable)
                {
                    curr.InnerNames.Add(statement.Name);
                    curr.OuterNames.Add(statement.PseudoName);
                }

                return curr;
            }
        );

        return (result.Inners, result.Outers, result.InnerNames, result.OuterNames);
    }

    private (List<string> inners, List<string> outers, List<string> innerNames) ProcessMetrics(
        BQMetric[] metrics,
        List<string> inners,
        List<string> outers,
        List<string> innerNames
    )
    {
        var result = metrics.Aggregate(
            new
            {
                Inners = inners,
                Outers = outers,
                InnerNames = innerNames,
            },
            (curr, next) =>
            {
                var statement = MetricParse(next);
                curr.Inners.Add(statement.Inner);
                curr.Outers.Add(statement.Outer);

                if (statement.Groupable)
                {
                    curr.InnerNames.Add(statement.Name);
                }

                return curr;
            }
        );

        return (result.Inners, result.Outers, result.InnerNames);
    }
}
