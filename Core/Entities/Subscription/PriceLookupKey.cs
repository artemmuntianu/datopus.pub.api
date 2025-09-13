using System.Runtime.Serialization;

namespace datopus.Core.Entities.Subscription;

public enum PriceLookupKey
{
    [EnumMember(Value = "collect_mo")]
    CollectMonthly,

    [EnumMember(Value = "collect_yr")]
    CollectYearly,

    [EnumMember(Value = "optimize_mo")]
    OptimizeMonthly,

    [EnumMember(Value = "optimize_yr")]
    OptimizeYearly,

    [EnumMember(Value = "scale_mo")]
    ScaleMonthly,

    [EnumMember(Value = "scale_yr")]
    ScaleYearly,
}

public static class PriceLookupKeyExtensions
{
    public static string? GetStringValue(this PriceLookupKey key)
    {
        var type = key.GetType();
        var memberInfo = type.GetMember(key.ToString());
        if (memberInfo.Length > 0)
        {
            var attrs = memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false);
            if (attrs.Length > 0)
            {
                return ((EnumMemberAttribute)attrs[0]).Value;
            }
        }
        return key.ToString();
    }
}
