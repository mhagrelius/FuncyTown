using Shouldly;
using Xunit;

namespace FuncyTown.Tests;

public class ResultAsyncTests
{
    private sealed record FakeError(string Code, string Message) : IError;

    [Fact]
    public async Task MapAsync_transforms_success()
    {
        var r = await Result<int, FakeError>.Success(2).MapAsync(async v => { await Task.Yield(); return v * 21; });
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(42);
    }

    [Fact]
    public async Task MapAsync_passes_failure_through()
    {
        var err = new FakeError("X", "x");
        var r = await Result<int, FakeError>.Failure(err).MapAsync(async v => { await Task.Yield(); return v * 2; });
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public async Task BindAsync_chains_into_async_result()
    {
        var r = await Result<int, FakeError>.Success(2)
            .BindAsync(async v => { await Task.Yield(); return Result<string, FakeError>.Success($"v={v}"); });
        r.Value.ShouldBe("v=2");
    }

    [Fact]
    public async Task MapErrorAsync_transforms_failure_error_and_alias_matches()
    {
        var r = await Result<int, FakeError>.Failure(new FakeError("A", "a"))
            .MapErrorAsync(async e => { await Task.Yield(); return new FakeError("B", e.Message + "!"); });
        var alias = await Result<int, FakeError>.Failure(new FakeError("A", "a"))
            .TransformErrorAsync(async e => { await Task.Yield(); return new FakeError("C", e.Message + "?"); });

        r.IsFailure.ShouldBeTrue();
        r.Error.Code.ShouldBe("B");
        r.Error.Message.ShouldBe("a!");
        alias.Error.Code.ShouldBe("C");
    }

    [Fact]
    public async Task MapErrorAsync_passes_success_through_without_invoking_selector()
    {
        var calls = 0;

        var r = await Result<int, FakeError>.Success(7)
            .MapErrorAsync(async e => { await Task.Yield(); calls++; return new FakeError("B", e.Message); });
        var extension = await Task.FromResult(Result<int, FakeError>.Success(9))
            .MapError(async e => { await Task.Yield(); calls++; return new FakeError("C", e.Message); });

        r.Value.ShouldBe(7);
        extension.Value.ShouldBe(9);
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task MapError_extension_on_Task_accepts_sync_and_async_selectors()
    {
        var sync = await Task.FromResult(Result<int, FakeError>.Failure(new FakeError("A", "a")))
            .MapError(e => new FakeError("B", e.Message + "!"));
        var asyncResult = await Task.FromResult(Result<int, FakeError>.Failure(new FakeError("A", "a")))
            .TransformError(async e => { await Task.Yield(); return new FakeError("C", e.Message + "?"); });

        sync.Error.Code.ShouldBe("B");
        asyncResult.Error.Code.ShouldBe("C");
    }

    [Fact]
    public async Task MapError_extension_validates_selector_before_source_task()
    {
        var srcTask = Task.FromException<Result<int, FakeError>>(new InvalidOperationException("source failed"));

        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.MapError((Func<FakeError, FakeError>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.MapError((Func<FakeError, Task<FakeError>>)null!));
    }

    [Fact]
    public async Task RecoverAsync_converts_failure_to_success_and_alias_matches()
    {
        var r = await Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .RecoverAsync(async _ => { await Task.Yield(); return 9; });
        var alias = await Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .OrElseAsync(async _ => { await Task.Yield(); return 11; });

        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(9);
        alias.Value.ShouldBe(11);
    }

    [Fact]
    public async Task RecoverAsync_passes_success_through_without_invoking_fallback()
    {
        var calls = 0;

        var r = await Result<int, FakeError>.Success(7)
            .RecoverAsync(async _ => { await Task.Yield(); calls++; return 9; });
        var extension = await Task.FromResult(Result<int, FakeError>.Success(11))
            .Recover(async _ => { await Task.Yield(); calls++; return 13; });

        r.Value.ShouldBe(7);
        extension.Value.ShouldBe(11);
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task Recover_extension_on_Task_accepts_sync_and_async_fallbacks()
    {
        var sync = await Task.FromResult(Result<int, FakeError>.Failure(new FakeError("X", "x")))
            .Recover(_ => 9);
        var asyncResult = await Task.FromResult(Result<int, FakeError>.Failure(new FakeError("X", "x")))
            .OrElse(async _ => { await Task.Yield(); return 11; });

        sync.Value.ShouldBe(9);
        asyncResult.Value.ShouldBe(11);
    }

    [Fact]
    public async Task Recover_extension_validates_fallback_before_source_task()
    {
        var srcTask = Task.FromException<Result<int, FakeError>>(new InvalidOperationException("source failed"));

        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Recover((Func<FakeError, int>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Recover((Func<FakeError, Task<int>>)null!));
    }

    [Fact]
    public async Task RecoverWithAsync_replaces_failure_with_result_and_alias_matches()
    {
        var r = await Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .RecoverWithAsync(async _ => { await Task.Yield(); return Result<int, FakeError>.Success(9); });
        var alias = await Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .OrElseThenAsync(async _ => { await Task.Yield(); return Result<int, FakeError>.Success(11); });

        r.Value.ShouldBe(9);
        alias.Value.ShouldBe(11);
    }

    [Fact]
    public async Task RecoverWithAsync_passes_success_through_without_invoking_fallback()
    {
        var calls = 0;

        var r = await Result<int, FakeError>.Success(7)
            .RecoverWithAsync(async _ => { await Task.Yield(); calls++; return Result<int, FakeError>.Success(9); });
        var extension = await Task.FromResult(Result<int, FakeError>.Success(11))
            .RecoverWith(async _ => { await Task.Yield(); calls++; return Result<int, FakeError>.Success(13); });

        r.Value.ShouldBe(7);
        extension.Value.ShouldBe(11);
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task RecoverWith_extension_on_Task_accepts_sync_and_async_fallbacks()
    {
        var sync = await Task.FromResult(Result<int, FakeError>.Failure(new FakeError("X", "x")))
            .RecoverWith(_ => Result<int, FakeError>.Success(9));
        var asyncResult = await Task.FromResult(Result<int, FakeError>.Failure(new FakeError("X", "x")))
            .OrElseThen(async _ => { await Task.Yield(); return Result<int, FakeError>.Success(11); });

        sync.Value.ShouldBe(9);
        asyncResult.Value.ShouldBe(11);
    }

    [Fact]
    public async Task RecoverWith_extension_validates_fallback_before_source_task()
    {
        var srcTask = Task.FromException<Result<int, FakeError>>(new InvalidOperationException("source failed"));

        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.RecoverWith((Func<FakeError, Result<int, FakeError>>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.RecoverWith((Func<FakeError, Task<Result<int, FakeError>>>)null!));
    }

    [Fact]
    public async Task OnSuccessAsync_runs_action_on_success_and_aliases_match()
    {
        var counts = new int[3];

        var r = await Result<int, FakeError>.Success(7)
            .OnSuccessAsync(async v => { await Task.Yield(); counts[0] = v; });
        await Result<int, FakeError>.Success(1)
            .TapAsync(async _ => { await Task.Yield(); counts[1]++; });
        await Result<int, FakeError>.Success(1)
            .IfSuccessfulAsync(async _ => { await Task.Yield(); counts[2]++; });

        r.Value.ShouldBe(7);
        counts.ShouldBe([7, 1, 1]);
    }

    [Fact]
    public async Task OnSuccessAsync_passes_failure_through_without_invoking_action()
    {
        var err = new FakeError("X", "x");
        var calls = 0;

        var r = await Result<int, FakeError>.Failure(err)
            .OnSuccessAsync(async _ => { await Task.Yield(); calls++; });
        var extension = await Task.FromResult(Result<int, FakeError>.Failure(err))
            .OnSuccess(async _ => { await Task.Yield(); calls++; });

        r.Error.ShouldBe(err);
        extension.Error.ShouldBe(err);
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task OnFailureAsync_runs_action_on_failure_and_aliases_match()
    {
        var err = new FakeError("X", "x");
        var seen = new FakeError?[3];

        var r = await Result<int, FakeError>.Failure(err)
            .OnFailureAsync(async e => { await Task.Yield(); seen[0] = e; });
        await Result<int, FakeError>.Failure(err)
            .TapErrorAsync(async e => { await Task.Yield(); seen[1] = e; });
        await Result<int, FakeError>.Failure(err)
            .IfFailedAsync(async e => { await Task.Yield(); seen[2] = e; });

        r.Error.ShouldBe(err);
        seen.ShouldBe([err, err, err]);
    }

    [Fact]
    public async Task OnFailureAsync_passes_success_through_without_invoking_action()
    {
        var calls = 0;

        var r = await Result<int, FakeError>.Success(7)
            .OnFailureAsync(async _ => { await Task.Yield(); calls++; });
        var extension = await Task.FromResult(Result<int, FakeError>.Success(11))
            .OnFailure(async _ => { await Task.Yield(); calls++; });

        r.Value.ShouldBe(7);
        extension.Value.ShouldBe(11);
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task Tap_extensions_on_Task_accept_sync_and_async_actions()
    {
        var counts = new int[2];
        var err = new FakeError("X", "x");

        var success = await Task.FromResult(Result<int, FakeError>.Success(7))
            .OnSuccess(v => counts[0] = v)
            .Tap(async v => { await Task.Yield(); counts[1] = v; });
        var failure = await Task.FromResult(Result<int, FakeError>.Failure(err))
            .OnFailure(_ => counts[0]++)
            .TapError(async _ => { await Task.Yield(); counts[1]++; });

        success.Value.ShouldBe(7);
        failure.Error.ShouldBe(err);
        counts.ShouldBe([8, 8]);
    }

    [Fact]
    public async Task Tap_extensions_validate_action_before_source_task()
    {
        var srcTask = Task.FromException<Result<int, FakeError>>(new InvalidOperationException("source failed"));

        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.OnSuccess((Action<int>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.OnSuccess((Func<int, Task>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.OnFailure((Action<FakeError>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.OnFailure((Func<FakeError, Task>)null!));
    }

    [Fact]
    public async Task EnsureAsync_converts_success_to_failure_when_predicate_fails_and_alias_matches()
    {
        var err = new FakeError("Bad", "bad");

        var r = await Result<int, FakeError>.Success(-1)
            .EnsureAsync(async v => { await Task.Yield(); return v > 0; }, err);
        var alias = await Result<int, FakeError>.Success(-2)
            .ValidateAsync(async v => { await Task.Yield(); return v > 0; }, err);

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
        alias.Error.ShouldBe(err);
    }

    [Fact]
    public async Task EnsureAsync_passes_failure_through_without_invoking_predicate()
    {
        var err = new FakeError("Original", "original");
        var replacement = new FakeError("Bad", "bad");
        var calls = 0;

        var r = await Result<int, FakeError>.Failure(err)
            .EnsureAsync(async _ => { await Task.Yield(); calls++; return false; }, replacement);
        var extension = await Task.FromResult(Result<int, FakeError>.Failure(err))
            .Ensure(async _ => { await Task.Yield(); calls++; return false; }, replacement);

        r.Error.ShouldBe(err);
        extension.Error.ShouldBe(err);
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task Ensure_extension_on_Task_accepts_sync_and_async_predicates()
    {
        var err = new FakeError("Bad", "bad");

        var sync = await Task.FromResult(Result<int, FakeError>.Success(-1))
            .Ensure(v => v > 0, err);
        var asyncResult = await Task.FromResult(Result<int, FakeError>.Success(-2))
            .Validate(async v => { await Task.Yield(); return v > 0; }, err);

        sync.Error.ShouldBe(err);
        asyncResult.Error.ShouldBe(err);
    }

    [Fact]
    public async Task Ensure_extension_validates_predicate_before_source_task()
    {
        var srcTask = Task.FromException<Result<int, FakeError>>(new InvalidOperationException("source failed"));
        var err = new FakeError("Bad", "bad");

        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Ensure((Func<int, bool>)null!, err));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Ensure((Func<int, Task<bool>>)null!, err));
    }

    [Fact]
    public async Task MatchAsync_runs_matching_async_branch()
    {
        var success = await Result<int, FakeError>.Success(7)
            .MatchAsync(
                async v => { await Task.Yield(); return $"ok={v}"; },
                async e => { await Task.Yield(); return $"err={e.Code}"; });
        var failure = await Result<int, FakeError>.Failure(new FakeError("X", "x"))
            .MatchAsync(
                async v => { await Task.Yield(); return $"ok={v}"; },
                async e => { await Task.Yield(); return $"err={e.Code}"; });

        success.ShouldBe("ok=7");
        failure.ShouldBe("err=X");
    }

    [Fact]
    public async Task Match_extension_on_Task_accepts_sync_and_async_branches()
    {
        var sync = await Task.FromResult(Result<int, FakeError>.Success(7))
            .Match(v => $"ok={v}", e => $"err={e.Code}");
        var asyncResult = await Task.FromResult(Result<int, FakeError>.Failure(new FakeError("X", "x")))
            .Match(
                async v => { await Task.Yield(); return $"ok={v}"; },
                async e => { await Task.Yield(); return $"err={e.Code}"; });

        sync.ShouldBe("ok=7");
        asyncResult.ShouldBe("err=X");
    }

    [Fact]
    public async Task Match_extension_validates_branches_before_source_task()
    {
        var srcTask = Task.FromException<Result<int, FakeError>>(new InvalidOperationException("source failed"));

        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Match((Func<int, string>)null!, e => e.Code));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Match(_ => "ok", (Func<FakeError, string>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Match((Func<int, Task<string>>)null!, e => Task.FromResult(e.Code)));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Match(_ => Task.FromResult("ok"), (Func<FakeError, Task<string>>)null!));
    }

    [Fact]
    public async Task Map_extension_on_Task_chains_without_inline_await()
    {
        // Phase 2: Task<Result<...>> extension methods make chains flow top-to-bottom.
        var srcTask = Task.FromResult(Result<int, FakeError>.Success(2));
        var r = await srcTask.Map(v => v + 40);
        r.Value.ShouldBe(42);
    }

    [Fact]
    public async Task Bind_extension_on_Task_chains_without_inline_await()
    {
        var srcTask = Task.FromResult(Result<int, FakeError>.Success(2));
        var r = await srcTask.Bind(v => Result<string, FakeError>.Success($"v={v}"));
        r.Value.ShouldBe("v=2");
    }

    [Fact]
    public async Task Map_extension_on_Task_accepts_async_selector()
    {
        var srcTask = Task.FromResult(Result<int, FakeError>.Success(2));
        var r = await srcTask.Map(async v => { await Task.Yield(); return v + 40; });
        r.Value.ShouldBe(42);
    }

    [Fact]
    public async Task Bind_extension_on_Task_accepts_async_next()
    {
        var srcTask = Task.FromResult(Result<int, FakeError>.Success(2));
        var r = await srcTask.Bind(async v => { await Task.Yield(); return Result<string, FakeError>.Success($"v={v}"); });
        r.Value.ShouldBe("v=2");
    }

    [Fact]
    public async Task Task_extension_aliases_chain_without_inline_await()
    {
        var srcTask = Task.FromResult(Result<int, FakeError>.Success(1));
        var r = await srcTask
            .Transform(v => v + 1)
            .Then(v => Result<string, FakeError>.Success($"v={v}"));

        r.Value.ShouldBe("v=2");
    }

    [Fact]
    public async Task Task_extension_validates_selector_before_source_task()
    {
        var srcTask = Task.FromException<Result<int, FakeError>>(new InvalidOperationException("source failed"));

        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Map((Func<int, int>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Map((Func<int, Task<int>>)null!));
    }

    [Fact]
    public async Task Task_extension_validates_next_before_source_task()
    {
        var srcTask = Task.FromException<Result<int, FakeError>>(new InvalidOperationException("source failed"));

        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Bind((Func<int, Result<string, FakeError>>)null!));
        await Should.ThrowAsync<ArgumentNullException>(
            () => srcTask.Bind((Func<int, Task<Result<string, FakeError>>>)null!));
    }

    [Fact]
    public async Task MapAsync_throws_for_uninitialized_result()
    {
        await Should.ThrowAsync<ResultException>(
            () => default(Result<int, FakeError>).MapAsync(async v => { await Task.Yield(); return v + 1; }));
    }

    [Fact]
    public async Task BindAsync_throws_for_uninitialized_result()
    {
        await Should.ThrowAsync<ResultException>(
            () => default(Result<int, FakeError>).BindAsync(async v => { await Task.Yield(); return Result<string, FakeError>.Success($"v={v}"); }));
    }
}
