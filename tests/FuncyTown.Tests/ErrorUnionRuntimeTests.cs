using Shouldly;
using Xunit;

namespace FuncyTown.Tests;

[ErrorUnion]
internal partial class TestError;

[ErrorCase]
internal sealed partial class TestNotFound(int id) : TestError
{
    public int Id { get; } = id;
}

[ErrorCase]
internal sealed partial class TestInvalid(string why) : TestError
{
    public string Why { get; } = why;
}

public class ErrorUnionRuntimeTests
{
    [Fact]
    public void Exhaustive_match_runs_correct_branch()
    {
        TestError error = new TestNotFound(7);

        var result = error.Match(
            testNotFound: notFound => $"not-found={notFound.Id}",
            testInvalid: invalid => $"invalid={invalid.Why}",
            many: many => $"many={many.Errors.Count}");

        result.ShouldBe("not-found=7");
    }

    [Fact]
    public void Fallback_match_runs_fallback_when_handlers_are_null()
    {
        TestError error = new TestInvalid("bad");

        var result = error.Match<string>(
            testNotFound: null,
            testInvalid: null,
            many: null,
            fallback: fallbackError => $"fallback={fallbackError.GetType().Name}");

        result.ShouldBe("fallback=TestInvalid");
    }

    [Fact]
    public void Switch_runs_correct_branch()
    {
        TestError error = new TestInvalid("bad");
        var observed = string.Empty;

        error.Switch(
            testNotFound: notFound => observed = $"not-found={notFound.Id}",
            testInvalid: invalid => observed = $"invalid={invalid.Why}",
            many: many => observed = $"many={many.Errors.Count}");

        observed.ShouldBe("invalid=bad");
    }

    [Fact]
    public void Combine_returns_many_and_many_is_matchable()
    {
        var combined = TestError.Combine([new TestNotFound(1), new TestInvalid("x")]);

        var many = combined.ShouldBeOfType<TestError.Many>();
        many.Errors.Count.ShouldBe(2);

        var result = combined.Match(
            testNotFound: notFound => $"not-found={notFound.Id}",
            testInvalid: invalid => $"invalid={invalid.Why}",
            many: matchedMany => $"many={matchedMany.Errors.Count}");

        result.ShouldBe("many=2");
    }

    [Fact]
    public void Code_defaults_to_type_name()
    {
        GetCode(new TestNotFound(7)).ShouldBe("TestNotFound");
    }

    private static string GetCode<TError>(TError error)
        where TError : IError
    {
        return error.Code;
    }
}
