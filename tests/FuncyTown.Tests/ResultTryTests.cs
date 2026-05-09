using Shouldly;
using Xunit;

namespace FuncyTown.ConsumerTests;

public class ResultTryTests
{
    private sealed record ExceptionalFakeError(string Code, string Message, Exception? Exception = null) : IExceptionalError<ExceptionalFakeError>
    {
        public static ExceptionalFakeError FromException(Exception exception, string? code = null, string? message = null)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return new ExceptionalFakeError(code ?? $"Fake.{exception.GetType().Name}", message ?? exception.Message, exception);
        }
    }

    [Fact]
    public void MapTry_transforms_success_value()
    {
        var result = Result<int, Error>.Success(21).MapTry(value => value * 2);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void MapTry_converts_exception_to_error_failure()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result<int, Error>.Success(21).MapTry(_ => ThrowInt(exception));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(nameof(InvalidOperationException));
        result.Error.Message.ShouldBe("boom");
        result.Error.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public void MapTry_catches_operation_canceled_exception()
    {
        var exception = new OperationCanceledException("cancelled");

        var result = Result<int, Error>.Success(21).MapTry(_ => ThrowInt(exception));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(nameof(OperationCanceledException));
        result.Error.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public void MapTry_uses_custom_exceptional_error_type()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result<int, ExceptionalFakeError>.Success(21)
            .MapTry(_ => ThrowInt(exception), code: "Storage.ReadFailed", message: "Could not read data");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Storage.ReadFailed");
        result.Error.Message.ShouldBe("Could not read data");
        result.Error.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public void MapTry_passes_failure_through_without_invoking_selector()
    {
        var error = new Error("Existing", "already failed");
        var calls = 0;

        var result = Result<int, Error>.Failure(error).MapTry(value =>
        {
            calls++;
            return value * 2;
        });

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
        calls.ShouldBe(0);
    }

    [Fact]
    public void ThenTry_chains_success_into_result()
    {
        var result = Result<int, Error>.Success(21)
            .ThenTry(value => Result<string, Error>.Success($"v={value * 2}"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("v=42");
    }

    [Fact]
    public void ThenTry_converts_exception_to_error_failure()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result<int, Error>.Success(21)
            .ThenTry(_ => ThrowStringResult(exception));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(nameof(InvalidOperationException));
        result.Error.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task MapTryAsync_converts_exception_to_error_failure()
    {
        var exception = new InvalidOperationException("boom");

        var result = await Result<int, Error>.Success(21).MapTryAsync(async _ =>
        {
            await Task.Yield();
            return ThrowInt(exception);
        });

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(nameof(InvalidOperationException));
        result.Error.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task ThenTryAsync_converts_exception_to_error_failure()
    {
        var exception = new InvalidOperationException("boom");

        var result = await Result<int, Error>.Success(21).ThenTryAsync(async _ =>
        {
            await Task.Yield();
            return ThrowStringResult(exception);
        });

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(nameof(InvalidOperationException));
        result.Error.Exception.ShouldBeSameAs(exception);
    }

    private static int ThrowInt(Exception exception) => throw exception;

    private static Result<string, Error> ThrowStringResult(Exception exception) => throw exception;
}
