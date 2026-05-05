using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;
using Xunit;

namespace FuncyTown.Tests.Json;

public class ResultJsonConverterTests
{
    private static readonly JsonSerializerOptions SnakeCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private static readonly JsonSerializerOptions KebabCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower };
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record FakeError(string Code, string Message) : IError;

    [Fact]
    public void Roundtrips_success_without_explicit_options_registration()
    {
        var result = Result<int, FakeError>.Success(42);

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<Result<int, FakeError>>(json);

        json.ShouldBe("""{"isSuccess":true,"value":42}""");
        roundtripped.IsSuccess.ShouldBeTrue();
        roundtripped.Value.ShouldBe(42);
    }

    [Fact]
    public void Roundtrips_failure_without_explicit_options_registration()
    {
        var result = Result<int, FakeError>.Failure(new FakeError("X", "x"));

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<Result<int, FakeError>>(json);

        json.ShouldBe("""{"isSuccess":false,"error":{"Code":"X","Message":"x"}}""");
        roundtripped.IsFailure.ShouldBeTrue();
        roundtripped.Error.Code.ShouldBe("X");
        roundtripped.Error.Message.ShouldBe("x");
    }

    [Fact]
    public void Roundtrips_generated_error_union_case_failure_without_explicit_options_registration()
    {
        var result = Result<int, RawGeneratedJsonError>.Failure(new RawGeneratedJsonNotFound(7));

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<Result<int, RawGeneratedJsonError>>(json);

        json.ShouldContain("\"$case\":\"RawGeneratedJsonNotFound\"");
        json.ShouldContain("\"Id\":7");
        roundtripped.IsFailure.ShouldBeTrue();
        var error = roundtripped.Error.ShouldBeOfType<RawGeneratedJsonNotFound>();
        error.Id.ShouldBe(7);
    }

    [Fact]
    public void Roundtrips_generated_error_union_many_failure_without_explicit_options_registration()
    {
        var result = Result<int, RawGeneratedJsonError>.Failure(RawGeneratedJsonError.Combine(
        [
            new RawGeneratedJsonNotFound(7),
            new RawGeneratedJsonValidation("name"),
        ]));

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<Result<int, RawGeneratedJsonError>>(json);

        json.ShouldContain("\"$case\":\"Many\"");
        json.ShouldContain("\"$case\":\"RawGeneratedJsonNotFound\"");
        json.ShouldContain("\"$case\":\"RawGeneratedJsonValidation\"");
        roundtripped.IsFailure.ShouldBeTrue();
        var many = roundtripped.Error.ShouldBeOfType<RawGeneratedJsonError.Many>();
        many.Errors.Count.ShouldBe(2);
        many.Errors[0].ShouldBeOfType<RawGeneratedJsonNotFound>().Id.ShouldBe(7);
        many.Errors[1].ShouldBeOfType<RawGeneratedJsonValidation>().Field.ShouldBe("name");
    }

    [Fact]
    public void Deserialize_throws_when_isSuccess_is_missing()
    {
        const string json = """{"value":42}""";

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int, FakeError>>(json));
    }

    [Fact]
    public void Deserialize_throws_when_success_value_is_missing()
    {
        const string json = """{"isSuccess":true}""";

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int, FakeError>>(json));
    }

    [Fact]
    public void Deserialize_throws_when_failure_error_is_missing()
    {
        const string json = """{"isSuccess":false}""";

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int, FakeError>>(json));
    }

    [Fact]
    public void Deserialize_throws_when_isSuccess_is_duplicated()
    {
        const string json = """{"isSuccess":true,"isSuccess":true,"value":42}""";

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int, FakeError>>(json));
    }

    [Theory]
    [InlineData("""{"isSuccess":true,"value":42,"value":43}""")]
    [InlineData("""{"isSuccess":false,"error":{"Code":"X","Message":"x"},"error":{"Code":"Y","Message":"y"}}""")]
    public void Deserialize_throws_when_selected_payload_is_duplicated(string json)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int, FakeError>>(json));
    }

    [Theory]
    [InlineData("""{"isSuccess":true,"value":42,"error":{"Code":"X","Message":"x"}}""")]
    [InlineData("""{"isSuccess":false,"error":{"Code":"X","Message":"x"},"value":42}""")]
    public void Deserialize_throws_when_value_and_error_are_both_present(string json)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int, FakeError>>(json));
    }

    [Theory]
    [InlineData("""{"isSuccess":1,"value":42}""")]
    [InlineData("""{"isSuccess":null,"value":42}""")]
    [InlineData("""{"isSuccess":"true","value":42}""")]
    public void Deserialize_throws_json_exception_when_isSuccess_is_not_boolean(string json)
    {
        Should.Throw<JsonException>(() => ReadWithResultConverter(json));
    }

    [Fact]
    public void Deserialize_skips_unknown_properties()
    {
        const string json = """
            {
                "ignored": { "nested": [1, 2, 3] },
                "isSuccess": true,
                "value": 42,
                "alsoIgnored": false
            }
            """;

        var result = JsonSerializer.Deserialize<Result<int, FakeError>>(json);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Serialize_throws_when_result_is_default()
    {
        var result = default(Result<int, FakeError>);

        Should.Throw<JsonException>(() => JsonSerializer.Serialize(result));
    }

    [Fact]
    public void Honors_snake_case_naming_policy_on_write()
    {
        var result = Result<int, FakeError>.Success(42);

        var json = JsonSerializer.Serialize(result, SnakeCaseOptions);

        json.ShouldBe("""{"is_success":true,"value":42}""");
    }

    [Fact]
    public void Honors_snake_case_naming_policy_on_read()
    {
        const string json = """{"is_success":true,"value":42}""";

        var result = JsonSerializer.Deserialize<Result<int, FakeError>>(json, SnakeCaseOptions);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Honors_kebab_case_naming_policy_round_trip()
    {
        var result = Result<int, FakeError>.Failure(new FakeError("X", "x"));

        var json = JsonSerializer.Serialize(result, KebabCaseOptions);
        var roundtripped = JsonSerializer.Deserialize<Result<int, FakeError>>(json, KebabCaseOptions);

        json.ShouldStartWith("""{"is-success":false,"error":""");
        roundtripped.IsFailure.ShouldBeTrue();
        roundtripped.Error.Code.ShouldBe("X");
    }

    [Fact]
    public void Honors_property_name_case_insensitive()
    {
        const string json = """{"IsSuccess":true,"Value":42}""";

        var result = JsonSerializer.Deserialize<Result<int, FakeError>>(json, CaseInsensitiveOptions);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Property_names_are_case_sensitive_by_default()
    {
        const string json = """{"IsSuccess":true,"Value":42}""";

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int, FakeError>>(json));
    }

    private static Result<int, FakeError> ReadWithResultConverter(string json)
    {
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        var converter = (JsonConverter<Result<int, FakeError>>)new ResultJsonConverterFactory()
            .CreateConverter(typeof(Result<int, FakeError>), JsonSerializerOptions.Default);

        return converter.Read(ref reader, typeof(Result<int, FakeError>), JsonSerializerOptions.Default);
    }
}

[ErrorUnion]
internal partial class RawGeneratedJsonError;

[ErrorCase]
internal sealed partial class RawGeneratedJsonNotFound(int id) : RawGeneratedJsonError
{
    public int Id { get; } = id;
}

[ErrorCase]
internal sealed partial class RawGeneratedJsonValidation(string field) : RawGeneratedJsonError
{
    public string Field { get; } = field;
}
