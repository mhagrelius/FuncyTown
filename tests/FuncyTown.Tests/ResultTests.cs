using Shouldly;
using Xunit;

namespace FuncyTown.Tests;

public class ResultTests
{
    private sealed record FakeError(string Code, string Message) : IError;

    [Fact]
    public void Success_factory_produces_success()
    {
        var r = Result<int, FakeError>.Success(42);
        r.IsSuccess.ShouldBeTrue();
        r.IsFailure.ShouldBeFalse();
        r.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_factory_produces_failure()
    {
        var err = new FakeError("NotFound", "missing");
        var r = Result<int, FakeError>.Failure(err);
        r.IsFailure.ShouldBeTrue();
        r.IsSuccess.ShouldBeFalse();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public void Default_result_reports_uninitialized()
    {
        Result<int, FakeError> r = default;
        r.IsUninitialized.ShouldBeTrue();
        r.IsSuccess.ShouldBeFalse();
        r.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Constructed_result_does_not_report_uninitialized()
    {
        Result<int, FakeError>.Success(1).IsUninitialized.ShouldBeFalse();
        Result<int, FakeError>.Failure(new FakeError("x", "y")).IsUninitialized.ShouldBeFalse();
    }

    [Fact]
    public void Accessing_value_on_failure_throws()
    {
        var r = Result<int, FakeError>.Failure(new FakeError("X", "x"));
        Should.Throw<ResultException>(() => _ = r.Value);
    }

    [Fact]
    public void Accessing_error_on_success_throws()
    {
        var r = Result<int, FakeError>.Success(1);
        Should.Throw<ResultException>(() => _ = r.Error);
    }

    [Fact]
    public void Default_initialized_result_throws_on_value_or_error()
    {
        var r = default(Result<int, FakeError>);
        r.IsSuccess.ShouldBeFalse();
        r.IsFailure.ShouldBeFalse();
        Should.Throw<ResultException>(() => _ = r.Value);
        Should.Throw<ResultException>(() => _ = r.Error);
    }

    [Fact]
    public void ToString_describes_success_without_accessing_error()
    {
        var r = Result<int, FakeError>.Success(42);
        r.ToString().ShouldBe("Success(42)");
    }

    [Fact]
    public void ToString_describes_failure_without_accessing_value()
    {
        var r = Result<int, FakeError>.Failure(new FakeError("X", "x"));
        r.ToString().ShouldBe("Failure(FakeError { Code = X, Message = x })");
    }

    [Fact]
    public void ToString_describes_uninitialized_result()
    {
        var r = default(Result<int, FakeError>);
        r.ToString().ShouldBe("Uninitialized");
    }

    [Fact]
    public void Map_transforms_success_value()
    {
        var r = Result<int, FakeError>.Success(2).Map(x => x * 21);
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(42);
    }

    [Fact]
    public void Map_passes_failure_through_unchanged()
    {
        var err = new FakeError("X", "x");
        var r = Result<int, FakeError>.Failure(err).Map(x => x * 2);
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public void Transform_alias_matches_Map()
    {
        var a = Result<int, FakeError>.Success(3).Map(x => x + 1);
        var b = Result<int, FakeError>.Success(3).Transform(x => x + 1);
        a.Value.ShouldBe(b.Value);
    }

    [Fact]
    public void Select_alias_matches_Map()
    {
        var a = Result<int, FakeError>.Success(3).Map(x => x + 1);
        var b = Result<int, FakeError>.Success(3).Select(x => x + 1);
        a.Value.ShouldBe(b.Value);
    }

    [Fact]
    public void Bind_chains_success_into_next_result()
    {
        var r = Result<int, FakeError>.Success(2)
            .Bind(x => Result<string, FakeError>.Success($"v={x}"));
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe("v=2");
    }

    [Fact]
    public void Bind_short_circuits_on_failure()
    {
        var err = new FakeError("X", "x");
        var r = Result<int, FakeError>.Failure(err)
            .Bind(x => Result<string, FakeError>.Success("nope"));
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public void Bind_propagates_inner_failure()
    {
        var err = new FakeError("X", "x");
        var r = Result<int, FakeError>.Success(2)
            .Bind(x => Result<string, FakeError>.Failure(err));
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public void Then_AndThen_SelectMany_aliases_match_Bind()
    {
        var src = Result<int, FakeError>.Success(2);
        var a = src.Bind(x => Result<int, FakeError>.Success(x + 1));
        var b = src.Then(x => Result<int, FakeError>.Success(x + 1));
        var c = src.AndThen(x => Result<int, FakeError>.Success(x + 1));
        var d = src.SelectMany(x => Result<int, FakeError>.Success(x + 1));
        a.Value.ShouldBe(b.Value);
        b.Value.ShouldBe(c.Value);
        c.Value.ShouldBe(d.Value);
    }

    [Fact]
    public void MapError_transforms_failure_error()
    {
        var r = Result<int, FakeError>.Failure(new FakeError("A", "a"))
            .MapError(e => new FakeError("B", e.Message + "!"));
        r.IsFailure.ShouldBeTrue();
        r.Error.Code.ShouldBe("B");
        r.Error.Message.ShouldBe("a!");
    }

    [Fact]
    public void MapError_passes_success_through_unchanged()
    {
        var r = Result<int, FakeError>.Success(7)
            .MapError(e => new FakeError("B", "b"));
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(7);
    }

    [Fact]
    public void TransformError_alias_matches_MapError()
    {
        var a = Result<int, FakeError>.Failure(new FakeError("A", "a"))
            .MapError(e => new FakeError("B", "b"));
        var b = Result<int, FakeError>.Failure(new FakeError("A", "a"))
            .TransformError(e => new FakeError("B", "b"));
        a.Error.Code.ShouldBe(b.Error.Code);
    }

    [Fact]
    public void Recover_converts_failure_to_success()
    {
        var r = Result<int, FakeError>.Failure(new FakeError("X", "x")).Recover(_ => -1);
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(-1);
    }

    [Fact]
    public void Recover_leaves_success_unchanged()
    {
        var r = Result<int, FakeError>.Success(5).Recover(_ => -1);
        r.Value.ShouldBe(5);
    }

    [Fact]
    public void OrElse_alias_matches_Recover()
    {
        var a = Result<int, FakeError>.Failure(new FakeError("X", "x")).Recover(_ => 9);
        var b = Result<int, FakeError>.Failure(new FakeError("X", "x")).OrElse(_ => 9);
        a.Value.ShouldBe(b.Value);
    }

    [Fact]
    public void RecoverWith_replaces_failure_with_another_result()
    {
        var r = Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .RecoverWith(_ => Result<int, FakeError>.Success(11));
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(11);
    }

    [Fact]
    public void RecoverWith_can_propagate_a_different_failure()
    {
        var newErr = new FakeError("Y", "y");
        var r = Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .RecoverWith(_ => Result<int, FakeError>.Failure(newErr));
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(newErr);
    }

    [Fact]
    public void OrElseThen_alias_matches_RecoverWith()
    {
        var a = Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .RecoverWith(_ => Result<int, FakeError>.Success(7));
        var b = Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .OrElseThen(_ => Result<int, FakeError>.Success(7));
        a.Value.ShouldBe(b.Value);
    }

    [Fact]
    public void OnSuccess_runs_action_on_success_and_returns_same_result()
    {
        var seen = 0;
        var r = Result<int, FakeError>.Success(7).OnSuccess(v => seen = v);
        seen.ShouldBe(7);
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(7);
    }

    [Fact]
    public void OnSuccess_does_nothing_on_failure()
    {
        var seen = 0;
        var r = Result<int, FakeError>.Failure(new FakeError("X", "x")).OnSuccess(v => seen = v);
        seen.ShouldBe(0);
        r.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void OnFailure_runs_action_on_failure_and_returns_same_result()
    {
        FakeError? seen = null;
        var err = new FakeError("X", "x");
        var r = Result<int, FakeError>.Failure(err).OnFailure(e => seen = e);
        seen.ShouldBe(err);
        r.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void OnFailure_does_nothing_on_success()
    {
        FakeError? seen = null;
        var r = Result<int, FakeError>.Success(1).OnFailure(e => seen = e);
        seen.ShouldBeNull();
        r.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Tap_IfSuccessful_aliases_match_OnSuccess()
    {
        var counts = new int[3];
        Result<int, FakeError>.Success(1).OnSuccess(_ => counts[0]++);
        Result<int, FakeError>.Success(1).Tap(_ => counts[1]++);
        Result<int, FakeError>.Success(1).IfSuccessful(_ => counts[2]++);
        counts.ShouldAllBe(c => c == 1);
    }

    [Fact]
    public void TapError_IfFailed_aliases_match_OnFailure()
    {
        var counts = new int[3];
        var err = new FakeError("X", "x");
        Result<int, FakeError>.Failure(err).OnFailure(_ => counts[0]++);
        Result<int, FakeError>.Failure(err).TapError(_ => counts[1]++);
        Result<int, FakeError>.Failure(err).IfFailed(_ => counts[2]++);
        counts.ShouldAllBe(c => c == 1);
    }

    [Fact]
    public void OnSuccess_and_success_aliases_throw_on_uninitialized_result()
    {
        var r = default(Result<int, FakeError>);
        Should.Throw<ResultException>(() => r.OnSuccess(_ => { }));
        Should.Throw<ResultException>(() => r.Tap(_ => { }));
        Should.Throw<ResultException>(() => r.IfSuccessful(_ => { }));
    }

    [Fact]
    public void OnFailure_and_failure_aliases_throw_on_uninitialized_result()
    {
        var r = default(Result<int, FakeError>);
        Should.Throw<ResultException>(() => r.OnFailure(_ => { }));
        Should.Throw<ResultException>(() => r.TapError(_ => { }));
        Should.Throw<ResultException>(() => r.IfFailed(_ => { }));
    }

    [Fact]
    public void Ensure_keeps_success_when_predicate_holds()
    {
        var r = Result<int, FakeError>.Success(5)
            .Ensure(v => v > 0, new FakeError("NonPositive", "must be > 0"));
        r.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Ensure_converts_to_failure_when_predicate_fails()
    {
        var err = new FakeError("NonPositive", "must be > 0");
        var r = Result<int, FakeError>.Success(-1).Ensure(v => v > 0, err);
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public void Ensure_leaves_existing_failure_unchanged()
    {
        var original = new FakeError("X", "x");
        var fallback = new FakeError("Y", "y");
        var r = Result<int, FakeError>.Failure(original).Ensure(v => true, fallback);
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(original);
    }

    [Fact]
    public void Validate_alias_matches_Ensure()
    {
        var err = new FakeError("X", "x");
        var a = Result<int, FakeError>.Success(-1).Ensure(v => v > 0, err);
        var b = Result<int, FakeError>.Success(-1).Validate(v => v > 0, err);
        a.IsFailure.ShouldBe(b.IsFailure);
    }

    [Fact]
    public void Match_runs_success_branch_on_success()
    {
        var s = Result<int, FakeError>.Success(7).Match(v => $"ok={v}", e => $"err={e.Code}");
        s.ShouldBe("ok=7");
    }

    [Fact]
    public void Match_runs_failure_branch_on_failure()
    {
        var err = new FakeError("X", "x");
        var s = Result<int, FakeError>.Failure(err).Match(v => $"ok={v}", e => $"err={e.Code}");
        s.ShouldBe("err=X");
    }

    [Fact]
    public void Match_throws_on_uninitialized()
    {
        var r = default(Result<int, FakeError>);
        Should.Throw<ResultException>(() => r.Match(v => v, _ => 0));
    }
}
