using System.Reflection;
using System.Text.Json.Serialization;

// TODO: handle null values
public static class ObjectExtensions
{
    public static Dictionary<string, object> ToDictionary<T>(this T obj)
    {
        var dictionary = new Dictionary<string, object>();
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            var jsonProperty = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            string key = jsonProperty?.Name ?? property.Name;
            object? value = property.GetValue(obj);

            if (value != null)
            {
                dictionary[key] = value;
            }
        }

        return dictionary;
    }
}
