using System.Text.Json;
using System.Text.Json.Serialization;

namespace FuncyTown;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated via ResultJsonConverterFactory using reflection.")]
internal sealed class ResultJsonConverter<T, TError> : JsonConverter<Result<T, TError>>
{
    private const string IsSuccessProperty = "isSuccess";
    private const string ValueProperty = "value";
    private const string ErrorProperty = "error";

    private readonly string _isSuccessName;
    private readonly string _valueName;
    private readonly string _errorName;
    private readonly StringComparison _propertyNameComparison;

    public ResultJsonConverter(JsonSerializerOptions options)
    {
        var policy = options?.PropertyNamingPolicy;
        _isSuccessName = policy?.ConvertName(IsSuccessProperty) ?? IsSuccessProperty;
        _valueName = policy?.ConvertName(ValueProperty) ?? ValueProperty;
        _errorName = policy?.ConvertName(ErrorProperty) ?? ErrorProperty;
        _propertyNameComparison = options?.PropertyNameCaseInsensitive == true
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    public override Result<T, TError> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object.");
        }

        bool? isSuccess = null;
        var hasValue = false;
        var hasError = false;
        T? value = default;
        TError? error = default;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return BuildResult(isSuccess, hasValue, value, hasError, error);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a JSON property.");
            }

            var propertyName = reader.GetString();
            if (!reader.Read())
            {
                throw new JsonException("Expected a JSON property value.");
            }

            if (string.Equals(propertyName, _isSuccessName, _propertyNameComparison))
            {
                if (isSuccess.HasValue)
                {
                    throw new JsonException($"Duplicate {_isSuccessName} discriminator.");
                }

                if (reader.TokenType is not JsonTokenType.True and not JsonTokenType.False)
                {
                    throw new JsonException($"Expected a boolean {_isSuccessName} discriminator.");
                }

                isSuccess = reader.GetBoolean();
            }
            else if (string.Equals(propertyName, _valueName, _propertyNameComparison))
            {
                if (hasValue)
                {
                    throw new JsonException($"Duplicate {_valueName} property.");
                }

                if (hasError)
                {
                    throw new JsonException($"Result JSON cannot contain both {_valueName} and {_errorName} properties.");
                }

                value = JsonSerializer.Deserialize<T>(ref reader, options);
                hasValue = true;
            }
            else if (string.Equals(propertyName, _errorName, _propertyNameComparison))
            {
                if (hasError)
                {
                    throw new JsonException($"Duplicate {_errorName} property.");
                }

                if (hasValue)
                {
                    throw new JsonException($"Result JSON cannot contain both {_valueName} and {_errorName} properties.");
                }

                error = JsonSerializer.Deserialize<TError>(ref reader, options);
                hasError = true;
            }
            else
            {
                reader.Skip();
            }
        }

        throw new JsonException("Expected the end of the JSON object.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        Result<T, TError> value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!value.IsSuccess && !value.IsFailure)
        {
            throw new JsonException("Cannot serialize an uninitialized Result.");
        }

        writer.WriteStartObject();
        writer.WriteBoolean(_isSuccessName, value.IsSuccess);
        if (value.IsSuccess)
        {
            writer.WritePropertyName(_valueName);
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            writer.WritePropertyName(_errorName);
            JsonSerializer.Serialize(writer, value.Error, options);
        }

        writer.WriteEndObject();
    }

    private Result<T, TError> BuildResult(
        bool? isSuccess,
        bool hasValue,
        T? value,
        bool hasError,
        TError? error) =>
        isSuccess switch
        {
            true when hasValue => Result<T, TError>.Success(value!),
            true => throw new JsonException($"Missing {_valueName} property."),
            false when hasError => Result<T, TError>.Failure(error!),
            false => throw new JsonException($"Missing {_errorName} property."),
            _ => throw new JsonException($"Missing {_isSuccessName} discriminator."),
        };
}
