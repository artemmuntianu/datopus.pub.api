using System.Text.Json.Serialization;
using FluentValidation;

namespace datopus.Api.EndpointFilters;

public class InputValidatorFilter<T> : IEndpointFilter
{
    private readonly IValidator<T> _validator;

    public InputValidatorFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        T? inputData = context.Arguments.OfType<T>().FirstOrDefault();

        if (inputData is not null)
        {
            var validationResult = await _validator.ValidateAsync(inputData);
            if (!validationResult.IsValid)
            {
                var errors = validationResult
                    .Errors.GroupBy(e => ConvertToJsonPropertyName(typeof(T), e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return Results.ValidationProblem(errors);
            }
        }
        return await next.Invoke(context);
    }

    private static string ConvertToJsonPropertyName(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        if (property is not null)
        {
            var jsonAttribute = property
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                .Cast<JsonPropertyNameAttribute>()
                .FirstOrDefault();
            if (jsonAttribute is not null)
            {
                return jsonAttribute.Name;
            }
        }
        return propertyName;
    }
}
