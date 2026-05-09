using System.Text.Json;
using Shouldly;
using Xunit;

namespace FuncyTown.Tests;

public class ErrorTests
{
    [Fact]
    public void Constructor_stores_code_message_and_exception()
    {
        var exception = new InvalidOperationException("boom");

        var error = new Error("Invalid", "Something failed", exception);

        error.Code.ShouldBe("Invalid");
        error.Message.ShouldBe("Something failed");
        error.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public void FromException_uses_exception_type_name_and_message_by_default()
    {
        var exception = new InvalidOperationException("boom");

        var error = Error.FromException(exception);

        error.Code.ShouldBe(nameof(InvalidOperationException));
        error.Message.ShouldBe("boom");
        error.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public void FromException_allows_code_and_message_overrides()
    {
        var exception = new InvalidOperationException("boom");

        var error = Error.FromException(exception, code: "Storage.WriteFailed", message: "Could not persist data");

        error.Code.ShouldBe("Storage.WriteFailed");
        error.Message.ShouldBe("Could not persist data");
        error.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public void FromException_rejects_null_exception()
    {
        Should.Throw<ArgumentNullException>(() => Error.FromException(null!));
    }

    [Fact]
    public void Exception_is_not_serialized_to_json()
    {
        var error = Error.FromException(new InvalidOperationException("boom"));

        var json = JsonSerializer.Serialize(error);
        using var document = JsonDocument.Parse(json);

        document.RootElement.GetProperty("Code").GetString().ShouldBe(nameof(InvalidOperationException));
        document.RootElement.GetProperty("Message").GetString().ShouldBe("boom");
        document.RootElement.TryGetProperty("Exception", out _).ShouldBeFalse();
    }
}
