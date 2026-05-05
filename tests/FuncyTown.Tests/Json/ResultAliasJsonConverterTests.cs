using System.Text.Json;
using Shouldly;
using Xunit;

namespace FuncyTown.Tests.Json;

public class ResultAliasJsonConverterTests
{
    [Fact]
    public void Value_alias_roundtrips_success_without_explicit_options_registration()
    {
        var result = JsonIntResult.Success(42);

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<JsonIntResult>(json);

        json.ShouldBe("""{"isSuccess":true,"value":42}""");
        roundtripped.IsSuccess.ShouldBeTrue();
        roundtripped.Value.ShouldBe(42);
    }

    [Fact]
    public void Value_alias_roundtrips_failure_without_explicit_options_registration()
    {
        var result = JsonIntResult.Failure(new AliasFakeError("X", "x"));

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<JsonIntResult>(json);

        json.ShouldBe("""{"isSuccess":false,"error":{"Code":"X","Message":"x"}}""");
        roundtripped.IsFailure.ShouldBeTrue();
        roundtripped.Error.Code.ShouldBe("X");
        roundtripped.Error.Message.ShouldBe("x");
    }

    [Fact]
    public void Value_alias_roundtrips_generated_error_union_case_failure_without_explicit_options_registration()
    {
        var result = JsonGeneratedUnionIntResult.Failure(new AliasGeneratedJsonNotFound(7));

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<JsonGeneratedUnionIntResult>(json);

        json.ShouldContain("\"$case\":\"AliasGeneratedJsonNotFound\"");
        json.ShouldContain("\"Id\":7");
        roundtripped.IsFailure.ShouldBeTrue();
        var error = roundtripped.Error.ShouldBeOfType<AliasGeneratedJsonNotFound>();
        error.Id.ShouldBe(7);
    }

    [Fact]
    public void Value_alias_roundtrips_generated_error_union_many_failure_without_explicit_options_registration()
    {
        var result = JsonGeneratedUnionIntResult.Failure(AliasGeneratedJsonError.Combine(
        [
            new AliasGeneratedJsonNotFound(7),
            new AliasGeneratedJsonValidation("name"),
        ]));

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<JsonGeneratedUnionIntResult>(json);

        json.ShouldContain("\"$case\":\"Many\"");
        json.ShouldContain("\"$case\":\"AliasGeneratedJsonNotFound\"");
        json.ShouldContain("\"$case\":\"AliasGeneratedJsonValidation\"");
        roundtripped.IsFailure.ShouldBeTrue();
        var many = roundtripped.Error.ShouldBeOfType<AliasGeneratedJsonError.Many>();
        many.Errors.Count.ShouldBe(2);
        many.Errors[0].ShouldBeOfType<AliasGeneratedJsonNotFound>().Id.ShouldBe(7);
        many.Errors[1].ShouldBeOfType<AliasGeneratedJsonValidation>().Field.ShouldBe("name");
    }

    [Fact]
    public void Void_alias_roundtrips_success_without_explicit_options_registration()
    {
        var result = JsonVoidResult.Success();

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<JsonVoidResult>(json);

        json.ShouldBe("""{"isSuccess":true,"value":{}}""");
        roundtripped.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Void_alias_roundtrips_failure_without_explicit_options_registration()
    {
        var result = JsonVoidResult.Failure(new AliasFakeError("Y", "y"));

        var json = JsonSerializer.Serialize(result);
        var roundtripped = JsonSerializer.Deserialize<JsonVoidResult>(json);

        json.ShouldBe("""{"isSuccess":false,"error":{"Code":"Y","Message":"y"}}""");
        roundtripped.IsFailure.ShouldBeTrue();
        roundtripped.Error.Code.ShouldBe("Y");
        roundtripped.Error.Message.ShouldBe("y");
    }
}

internal sealed record AliasFakeError(string Code, string Message) : IError;

[Result<int, AliasFakeError>]
internal readonly partial record struct JsonIntResult;

[Result<AliasFakeError>]
internal readonly partial record struct JsonVoidResult;

[ErrorUnion]
internal partial class AliasGeneratedJsonError;

[ErrorCase]
internal sealed partial class AliasGeneratedJsonNotFound(int id) : AliasGeneratedJsonError
{
    public int Id { get; } = id;
}

[ErrorCase]
internal sealed partial class AliasGeneratedJsonValidation(string field) : AliasGeneratedJsonError
{
    public string Field { get; } = field;
}

[Result<int, AliasGeneratedJsonError>]
internal readonly partial record struct JsonGeneratedUnionIntResult;
