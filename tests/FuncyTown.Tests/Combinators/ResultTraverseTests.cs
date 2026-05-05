using Shouldly;
using Xunit;

namespace FuncyTown.Tests.Combinators;

public class ResultTraverseTests
{
    private sealed record Err(string Code, string Message) : IError;

    [Fact]
    public void Traverse_returns_all_transformed_values_when_all_succeed()
    {
        var r = Result.Traverse(
            [1, 2, 3],
            static value => Result<string, Err>.Success($"v{value}"));

        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(["v1", "v2", "v3"]);
    }

    [Fact]
    public void Traverse_short_circuits_on_first_failure_without_enumerating_or_calling_later_items()
    {
        var err = new Err("X", "x");
        var calls = new List<int>();

        var r = Result.Traverse(SourceThatThrowsAfterSecondItem(), value =>
        {
            calls.Add(value);

            return value == 2
                ? Result<string, Err>.Failure(err)
                : Result<string, Err>.Success($"v{value}");
        });

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
        calls.ShouldBe([1, 2]);
    }

    [Fact]
    public async Task TraverseAsync_awaits_each_selector_and_short_circuits_on_failure()
    {
        var err = new Err("X", "x");
        var calls = new List<int>();

        var r = await Result.TraverseAsync(SourceThatThrowsAfterSecondItem(), async value =>
        {
            await Task.Yield();
            calls.Add(value);

            return value == 2
                ? Result<string, Err>.Failure(err)
                : Result<string, Err>.Success($"v{value}");
        });

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
        calls.ShouldBe([1, 2]);
    }

    [Fact]
    public void Sequence_flips_results_to_result_of_read_only_list()
    {
        Result<int, Err>[] results =
        [
            Result<int, Err>.Success(1),
            Result<int, Err>.Success(2),
            Result<int, Err>.Success(3),
        ];

        var r = Result.Sequence(results);

        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Sequence_short_circuits_on_first_failure_without_enumerating_later_items()
    {
        var err = new Err("X", "x");

        var r = Result.Sequence(SourceResultsThatThrowsAfterSecondItem(err));

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public async Task SequenceAsync_flips_tasks_to_task_of_result_of_read_only_list()
    {
        Task<Result<int, Err>>[] results =
        [
            Task.FromResult(Result<int, Err>.Success(1)),
            Task.FromResult(Result<int, Err>.Success(2)),
            Task.FromResult(Result<int, Err>.Success(3)),
        ];

        var r = await Result.SequenceAsync(results);

        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task SequenceAsync_short_circuits_on_first_failure_without_enumerating_later_items()
    {
        var err = new Err("X", "x");

        var r = await Result.SequenceAsync(SourceResultTasksThatThrowsAfterSecondItem(err));

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public void Traverse_throws_when_source_is_null()
    {
        IEnumerable<int>? source = null;

        var ex = Should.Throw<ArgumentNullException>(() =>
            Result.Traverse(source!, static value => Result<int, Err>.Success(value)));

        ex.ParamName.ShouldBe("source");
    }

    [Fact]
    public void Traverse_throws_when_selector_is_null()
    {
        Func<int, Result<int, Err>>? selector = null;

        var ex = Should.Throw<ArgumentNullException>(() => Result.Traverse([1], selector!));

        ex.ParamName.ShouldBe("selector");
    }

    [Fact]
    public void TraverseAsync_throws_when_source_is_null()
    {
        IEnumerable<int>? source = null;

        var ex = Should.Throw<ArgumentNullException>(() =>
        {
            _ = Result.TraverseAsync(source!, static value => Task.FromResult(Result<int, Err>.Success(value)));
        });

        ex.ParamName.ShouldBe("source");
    }

    [Fact]
    public void TraverseAsync_throws_when_selector_is_null()
    {
        Func<int, Task<Result<int, Err>>>? selector = null;

        var ex = Should.Throw<ArgumentNullException>(() =>
        {
            _ = Result.TraverseAsync([1], selector!);
        });

        ex.ParamName.ShouldBe("selector");
    }

    [Fact]
    public void Sequence_throws_when_source_is_null()
    {
        IEnumerable<Result<int, Err>>? source = null;

        var ex = Should.Throw<ArgumentNullException>(() => Result.Sequence(source!));

        ex.ParamName.ShouldBe("source");
    }

    [Fact]
    public void SequenceAsync_throws_when_source_is_null()
    {
        IEnumerable<Task<Result<int, Err>>>? source = null;

        var ex = Should.Throw<ArgumentNullException>(() =>
        {
            _ = Result.SequenceAsync(source!);
        });

        ex.ParamName.ShouldBe("source");
    }

    [Fact]
    public void Traverse_throws_result_exception_for_uninitialized_selector_result()
    {
        var ex = Should.Throw<ResultException>(() =>
            Result.Traverse([1], static _ => default(Result<int, Err>)));

        ex.Message.ShouldBe("Cannot Traverse an uninitialized Result.");
    }

    [Fact]
    public void Sequence_throws_result_exception_for_uninitialized_source_result()
    {
        var ex = Should.Throw<ResultException>(() =>
            Result.Sequence(
            [
                Result<int, Err>.Success(1),
                default,
            ]));

        ex.Message.ShouldBe("Cannot Sequence an uninitialized Result.");
    }

    private static IEnumerable<int> SourceThatThrowsAfterSecondItem()
    {
        yield return 1;
        yield return 2;

        throw new InvalidOperationException("The sequence should have short-circuited.");
    }

    private static IEnumerable<Result<int, Err>> SourceResultsThatThrowsAfterSecondItem(Err err)
    {
        yield return Result<int, Err>.Success(1);
        yield return Result<int, Err>.Failure(err);

        throw new InvalidOperationException("The sequence should have short-circuited.");
    }

    private static IEnumerable<Task<Result<int, Err>>> SourceResultTasksThatThrowsAfterSecondItem(Err err)
    {
        yield return Task.FromResult(Result<int, Err>.Success(1));
        yield return Task.FromResult(Result<int, Err>.Failure(err));

        throw new InvalidOperationException("The sequence should have short-circuited.");
    }
}
