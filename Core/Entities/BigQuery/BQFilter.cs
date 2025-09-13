using System.Runtime.Serialization;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter<BQMatchType>))]
public enum BQMatchType
{
    [EnumMember(Value = "EXACT")]
    Exact,

    [EnumMember(Value = "NOT_EXACT")]
    NotExact,

    [EnumMember(Value = "MATCH_REGEX")]
    MatchRegex,

    [EnumMember(Value = "NOT_MATCH_REGEX")]
    NotMatchRegex,

    [EnumMember(Value = "BEGINS_WITH")]
    BeginsWith,

    [EnumMember(Value = "ENDS_WITH")]
    EndsWith,

    [EnumMember(Value = "CONTAINS")]
    Contains,
}

[JsonConverter(typeof(JsonStringEnumConverter<BQOperation>))]
public enum BQOperation
{
    [EnumMember(Value = "EQUAL")]
    Equal,

    [EnumMember(Value = "NOT_EQUAL")]
    NotEqual,

    [EnumMember(Value = "LESS_THAN")]
    LessThan,

    [EnumMember(Value = "LESS_THAN_OR_EQUAL")]
    LessThanOrEqual,

    [EnumMember(Value = "GREATER_THAN")]
    GreaterThan,

    [EnumMember(Value = "GREATER_THAN_OR_EQUAL")]
    GreaterThanOrEqual,
}

public abstract class BQFilterBase { }

public class BQStringFilter : BQFilterBase
{
    [JsonPropertyName("matchType")]
    public BQMatchType MatchType { get; init; }

    [JsonPropertyName("value")]
    public string Value { get; init; }

    [JsonPropertyName("caseSensitive")]
    public bool CaseSensitive { get; init; }

    public BQStringFilter(BQMatchType matchType, string value, bool caseSensitive = false)
    {
        MatchType = matchType;
        Value = value;
        CaseSensitive = caseSensitive;
    }
}

public class BQInListFilter : BQFilterBase
{
    [JsonPropertyName("values")]
    public List<string> Values { get; init; }

    [JsonPropertyName("caseSensitive")]
    public bool CaseSensitive { get; init; }

    public BQInListFilter(IEnumerable<string>? values = null, bool caseSensitive = false)
    {
        Values = values != null ? new List<string>(values) : new List<string>();
        CaseSensitive = caseSensitive;
    }
}

public abstract class BQNumericValue { }

public class BQNumericDoubleValue : BQNumericValue
{
    [JsonPropertyName("value")]
    public double Value { get; init; }

    public BQNumericDoubleValue(double value) => Value = value;
}

public class BQNumericIntValue : BQNumericValue
{
    [JsonPropertyName("value")]
    public long Value { get; init; }

    public BQNumericIntValue(long value) => Value = value;
}

public class BQNumericFilter : BQFilterBase
{
    [JsonPropertyName("operation")]
    public BQOperation Operation { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    public BQNumericFilter(string value) => Value = value;
}

public class BQBetweenFilter : BQFilterBase
{
    [JsonPropertyName("fromValue")]
    public double FromValue { get; init; }

    [JsonPropertyName("toValue")]
    public double ToValue { get; init; }

    public BQBetweenFilter(double fromValue, double toValue)
    {
        FromValue = fromValue;
        ToValue = toValue;
    }
}

public class BQFilter
{
    [JsonPropertyName("fieldName")]
    public string FieldName { get; init; }

    [JsonPropertyName("custom")]
    public bool Custom { get; init; }

    [JsonPropertyName("stringFilter")]
    public BQStringFilter? StringFilter { get; init; }

    [JsonPropertyName("inListFilter")]
    public BQInListFilter? InListFilter { get; init; }

    [JsonPropertyName("numericFilter")]
    public BQNumericFilter? NumericFilter { get; init; }

    [JsonPropertyName("betweenFilter")]
    public BQBetweenFilter? BetweenFilter { get; init; }

    public BQFilter(
        string fieldName,
        bool custom,
        BQStringFilter? stringFilter = null,
        BQInListFilter? inListFilter = null,
        BQNumericFilter? numericFilter = null,
        BQBetweenFilter? betweenFilter = null
    )
    {
        FieldName = fieldName;
        Custom = custom;
        StringFilter = stringFilter;
        InListFilter = inListFilter;
        NumericFilter = numericFilter;
        BetweenFilter = betweenFilter;
        Validate();
    }

    private void Validate()
    {
        int setCount =
            (StringFilter != null ? 1 : 0)
            + (InListFilter != null ? 1 : 0)
            + (NumericFilter != null ? 1 : 0)
            + (BetweenFilter != null ? 1 : 0);

        if (setCount > 1)
            throw new InvalidOperationException("Only one filter type can be set.");
        if (setCount == 0)
            throw new InvalidOperationException("At least one filter type must be set.");
    }
}

public class BQFilterExpression
{
    [JsonPropertyName("andGroup")]
    public BQFilterExpressionList? AndGroup { get; init; }

    [JsonPropertyName("orGroup")]
    public BQFilterExpressionList? OrGroup { get; init; }

    [JsonPropertyName("filter")]
    public BQFilter? Filter { get; init; }

    public BQFilterExpression(
        BQFilterExpressionList? andGroup = null,
        BQFilterExpressionList? orGroup = null,
        BQFilter? filter = null
    )
    {
        int count =
            (andGroup != null ? 1 : 0) + (orGroup != null ? 1 : 0) + (filter != null ? 1 : 0);

        if (count != 1)
            throw new ArgumentException("Exactly one field must be set in BQFilterExpression");

        AndGroup = andGroup;
        OrGroup = orGroup;
        Filter = filter;
    }
}

public class BQFilterExpressionList
{
    [JsonPropertyName("expressions")]
    public List<BQFilterExpression> Expressions { get; init; }

    public BQFilterExpressionList(List<BQFilterExpression> expressions)
    {
        if (expressions == null || expressions.Count == 0)
        {
            throw new ArgumentException(
                "Expressions list cannot be null or empty.",
                nameof(expressions)
            );
        }

        Expressions = expressions;
    }
}

public class BQFilterAndGroup
{
    [JsonPropertyName("andGroup")]
    public BQFilterExpressionList AndGroup { get; init; }

    public BQFilterAndGroup(BQFilterExpressionList andGroup) => AndGroup = andGroup;
}

public class BQFilterOrGroup
{
    [JsonPropertyName("orGroup")]
    public BQFilterExpressionList OrGroup { get; init; }

    public BQFilterOrGroup(BQFilterExpressionList orGroup) => OrGroup = orGroup;
}

public class BQFilterNotExpression
{
    [JsonPropertyName("expression")]
    public BQFilterExpression Expression { get; init; }

    public BQFilterNotExpression(BQFilterExpression expression) => Expression = expression;
}
