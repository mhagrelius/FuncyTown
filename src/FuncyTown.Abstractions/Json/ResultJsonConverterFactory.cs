using System.Text.Json;
using System.Text.Json.Serialization;

namespace FuncyTown;

/// <summary>Creates JSON converters for generic <see cref="Result{T, TError}"/> values.</summary>
public sealed class ResultJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert.IsGenericType &&
            typeToConvert.GetGenericTypeDefinition() == typeof(Result<,>);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);
        var genericArguments = typeToConvert.GetGenericArguments();
        var converterType = typeof(ResultJsonConverter<,>).MakeGenericType(genericArguments);
        return (JsonConverter)Activator.CreateInstance(converterType, options)!;
    }
}
