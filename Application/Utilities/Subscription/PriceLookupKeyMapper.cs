using System.Reflection;
using System.Runtime.Serialization;
using datopus.Core.Entities.Subscription;

public static class PriceLookupKeyParser
{
    private static readonly Lazy<Dictionary<string, PriceLookupKey>> ValueToEnumMap = new Lazy<
        Dictionary<string, PriceLookupKey>
    >(InitializeMap);

    private static Dictionary<string, PriceLookupKey> InitializeMap()
    {
        var map = new Dictionary<string, PriceLookupKey>(StringComparer.OrdinalIgnoreCase);
        var enumType = typeof(PriceLookupKey);

        foreach (PriceLookupKey value in Enum.GetValues(enumType))
        {
            string stringValue = GetEnumMemberValue(value) ?? value.ToString();
            if (!map.ContainsKey(stringValue))
            {
                map[stringValue] = value;
            }
        }
        return map;
    }

    private static string? GetEnumMemberValue(PriceLookupKey value)
    {
        var memberInfo = typeof(PriceLookupKey).GetMember(value.ToString()).FirstOrDefault();
        var enumMemberAttribute = memberInfo?.GetCustomAttribute<EnumMemberAttribute>(false);
        return enumMemberAttribute?.Value;
    }

    public static bool TryParse(string? value, out PriceLookupKey result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        return ValueToEnumMap.Value.TryGetValue(value, out result);
    }

    public static PriceLookupKey? ParseOptional(string? value)
    {
        if (TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }
}
