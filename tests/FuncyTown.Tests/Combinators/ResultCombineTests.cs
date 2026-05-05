using Shouldly;
using Xunit;

namespace FuncyTown.Tests.Combinators;

[ErrorUnion]
internal partial class CombineTestErr;

[ErrorCase]
internal sealed partial class CombineE1(string m) : CombineTestErr
{
    public string M { get; } = m;
}

[ErrorCase]
internal sealed partial class CombineE2(string m) : CombineTestErr
{
    public string M { get; } = m;
}

public class ResultCombineTests
{
    [Fact]
    public void Combine_returns_all_success_values_when_all_succeed()
    {
        var r = Result.Combine(
            Result<int, CombineTestErr>.Success(1),
            Result<int, CombineTestErr>.Success(2),
            Result<int, CombineTestErr>.Success(3));

        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Combine_accumulates_all_errors_through_generated_union_many_when_any_fail()
    {
        var e1 = new CombineE1("a");
        var e2 = new CombineE2("b");

        var r = Result.Combine(
            Result<int, CombineTestErr>.Failure(e1),
            Result<int, CombineTestErr>.Success(7),
            Result<int, CombineTestErr>.Failure(e2));

        r.IsFailure.ShouldBeTrue();
        var many = r.Error.ShouldBeOfType<CombineTestErr.Many>();
        many.Errors.ShouldBe([e1, e2]);
    }

    [Fact]
    public void Combine_returns_empty_success_list_for_no_inputs()
    {
        var r = Result.Combine<int, CombineTestErr>();

        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBeEmpty();
    }

    [Fact]
    public void Combine_throws_result_exception_for_uninitialized_input()
    {
        var ex = Should.Throw<ResultException>(() =>
            Result.Combine(
                Result<int, CombineTestErr>.Success(1),
                default(Result<int, CombineTestErr>),
                Result<int, CombineTestErr>.Failure(new CombineE1("a"))));

        ex.Message.ShouldBe("Cannot Combine an uninitialized Result.");
    }

    [Fact]
    public void Combine_array_fallback_delegates_to_span_overload()
    {
        Result<int, CombineTestErr>[] results =
        [
            Result<int, CombineTestErr>.Success(4),
            Result<int, CombineTestErr>.Success(5),
        ];

        var r = Result.Combine(results);

        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe([4, 5]);
    }

    [Fact]
    public void Combine_array_fallback_throws_when_array_is_null()
    {
        Result<int, CombineTestErr>[]? results = null;

        var ex = Should.Throw<ArgumentNullException>(() => Result.Combine(results!));

        ex.ParamName.ShouldBe("results");
    }
}
