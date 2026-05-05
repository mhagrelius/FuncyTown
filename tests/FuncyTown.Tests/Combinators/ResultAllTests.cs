using Shouldly;
using Xunit;

namespace FuncyTown.Tests.Combinators;

public class ResultAllTests
{
    private sealed record Err(string Code, string Message) : IError;

    [Fact]
    public void All_arity2_succeeds_when_both_succeed()
    {
        var r = Result.All(Result<int, Err>.Success(1), Result<string, Err>.Success("a"));

        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe((1, "a"));
    }

    [Fact]
    public void All_arity2_short_circuits_on_first_failure()
    {
        var err = new Err("X", "x");
        var r = Result.All(Result<int, Err>.Failure(err), Result<string, Err>.Success("a"));

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Fact]
    public void All_arity2_throws_when_first_operand_is_uninitialized_even_if_second_fails()
    {
        var err = new Err("X", "x");

        var ex = Should.Throw<ResultException>(() =>
            Result.All(default(Result<int, Err>), Result<string, Err>.Failure(err)));

        ex.Message.ShouldBe("Cannot All an uninitialized Result.");
    }

    [Fact]
    public void All_arity2_throws_when_second_operand_is_uninitialized_after_first_succeeds()
    {
        var ex = Should.Throw<ResultException>(() =>
            Result.All(Result<int, Err>.Success(1), default(Result<string, Err>)));

        ex.Message.ShouldBe("Cannot All an uninitialized Result.");
    }

    [Fact]
    public void All_arity2_first_failure_short_circuits_without_reading_uninitialized_second_operand()
    {
        var err = new Err("X", "x");

        var r = Result.All(Result<int, Err>.Failure(err), default(Result<string, Err>));

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(err);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void All_arities3through8_succeed_when_all_operands_succeed(int arity)
    {
        switch (arity)
        {
            case 3:
                var r3 = Result.All(
                    Result<int, Err>.Success(1),
                    Result<string, Err>.Success("a"),
                    Result<bool, Err>.Success(true));
                r3.IsSuccess.ShouldBeTrue();
                r3.Value.ShouldBe((1, "a", true));
                break;

            case 4:
                var r4 = Result.All(
                    Result<int, Err>.Success(1),
                    Result<string, Err>.Success("a"),
                    Result<bool, Err>.Success(true),
                    Result<decimal, Err>.Success(2.5m));
                r4.IsSuccess.ShouldBeTrue();
                r4.Value.ShouldBe((1, "a", true, 2.5m));
                break;

            case 5:
                var r5 = Result.All(
                    Result<int, Err>.Success(1),
                    Result<string, Err>.Success("a"),
                    Result<bool, Err>.Success(true),
                    Result<decimal, Err>.Success(2.5m),
                    Result<char, Err>.Success('z'));
                r5.IsSuccess.ShouldBeTrue();
                r5.Value.ShouldBe((1, "a", true, 2.5m, 'z'));
                break;

            case 6:
                var r6 = Result.All(
                    Result<int, Err>.Success(1),
                    Result<string, Err>.Success("a"),
                    Result<bool, Err>.Success(true),
                    Result<decimal, Err>.Success(2.5m),
                    Result<char, Err>.Success('z'),
                    Result<long, Err>.Success(6L));
                r6.IsSuccess.ShouldBeTrue();
                r6.Value.ShouldBe((1, "a", true, 2.5m, 'z', 6L));
                break;

            case 7:
                var r7 = Result.All(
                    Result<int, Err>.Success(1),
                    Result<string, Err>.Success("a"),
                    Result<bool, Err>.Success(true),
                    Result<decimal, Err>.Success(2.5m),
                    Result<char, Err>.Success('z'),
                    Result<long, Err>.Success(6L),
                    Result<Guid, Err>.Success(Guid.Empty));
                r7.IsSuccess.ShouldBeTrue();
                r7.Value.ShouldBe((1, "a", true, 2.5m, 'z', 6L, Guid.Empty));
                break;

            case 8:
                var r8 = Result.All(
                    Result<int, Err>.Success(1),
                    Result<string, Err>.Success("a"),
                    Result<bool, Err>.Success(true),
                    Result<decimal, Err>.Success(2.5m),
                    Result<char, Err>.Success('z'),
                    Result<long, Err>.Success(6L),
                    Result<Guid, Err>.Success(Guid.Empty),
                    Result<DateOnly, Err>.Success(new DateOnly(2026, 5, 3)));
                r8.IsSuccess.ShouldBeTrue();
                r8.Value.ShouldBe((1, "a", true, 2.5m, 'z', 6L, Guid.Empty, new DateOnly(2026, 5, 3)));
                break;
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void All_arities3through8_short_circuit_on_first_failure_without_reading_later_operands(int arity)
    {
        var err = new Err("X", "x");

        switch (arity)
        {
            case 3:
                var r3 = Result.All(
                    Result<int, Err>.Failure(err),
                    default(Result<string, Err>),
                    default(Result<bool, Err>));
                r3.IsFailure.ShouldBeTrue();
                r3.Error.ShouldBe(err);
                break;

            case 4:
                var r4 = Result.All(
                    Result<int, Err>.Failure(err),
                    default(Result<string, Err>),
                    default(Result<bool, Err>),
                    default(Result<decimal, Err>));
                r4.IsFailure.ShouldBeTrue();
                r4.Error.ShouldBe(err);
                break;

            case 5:
                var r5 = Result.All(
                    Result<int, Err>.Failure(err),
                    default(Result<string, Err>),
                    default(Result<bool, Err>),
                    default(Result<decimal, Err>),
                    default(Result<char, Err>));
                r5.IsFailure.ShouldBeTrue();
                r5.Error.ShouldBe(err);
                break;

            case 6:
                var r6 = Result.All(
                    Result<int, Err>.Failure(err),
                    default(Result<string, Err>),
                    default(Result<bool, Err>),
                    default(Result<decimal, Err>),
                    default(Result<char, Err>),
                    default(Result<long, Err>));
                r6.IsFailure.ShouldBeTrue();
                r6.Error.ShouldBe(err);
                break;

            case 7:
                var r7 = Result.All(
                    Result<int, Err>.Failure(err),
                    default(Result<string, Err>),
                    default(Result<bool, Err>),
                    default(Result<decimal, Err>),
                    default(Result<char, Err>),
                    default(Result<long, Err>),
                    default(Result<Guid, Err>));
                r7.IsFailure.ShouldBeTrue();
                r7.Error.ShouldBe(err);
                break;

            case 8:
                var r8 = Result.All(
                    Result<int, Err>.Failure(err),
                    default(Result<string, Err>),
                    default(Result<bool, Err>),
                    default(Result<decimal, Err>),
                    default(Result<char, Err>),
                    default(Result<long, Err>),
                    default(Result<Guid, Err>),
                    default(Result<DateOnly, Err>));
                r8.IsFailure.ShouldBeTrue();
                r8.Error.ShouldBe(err);
                break;
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void All_arities3through8_throw_when_later_operand_is_uninitialized_after_previous_successes(int arity)
    {
        var ex = Should.Throw<ResultException>(() =>
        {
            switch (arity)
            {
                case 3:
                    Result.All(
                        Result<int, Err>.Success(1),
                        Result<string, Err>.Success("a"),
                        default(Result<bool, Err>));
                    break;

                case 4:
                    Result.All(
                        Result<int, Err>.Success(1),
                        Result<string, Err>.Success("a"),
                        Result<bool, Err>.Success(true),
                        default(Result<decimal, Err>));
                    break;

                case 5:
                    Result.All(
                        Result<int, Err>.Success(1),
                        Result<string, Err>.Success("a"),
                        Result<bool, Err>.Success(true),
                        Result<decimal, Err>.Success(2.5m),
                        default(Result<char, Err>));
                    break;

                case 6:
                    Result.All(
                        Result<int, Err>.Success(1),
                        Result<string, Err>.Success("a"),
                        Result<bool, Err>.Success(true),
                        Result<decimal, Err>.Success(2.5m),
                        Result<char, Err>.Success('z'),
                        default(Result<long, Err>));
                    break;

                case 7:
                    Result.All(
                        Result<int, Err>.Success(1),
                        Result<string, Err>.Success("a"),
                        Result<bool, Err>.Success(true),
                        Result<decimal, Err>.Success(2.5m),
                        Result<char, Err>.Success('z'),
                        Result<long, Err>.Success(6L),
                        default(Result<Guid, Err>));
                    break;

                case 8:
                    Result.All(
                        Result<int, Err>.Success(1),
                        Result<string, Err>.Success("a"),
                        Result<bool, Err>.Success(true),
                        Result<decimal, Err>.Success(2.5m),
                        Result<char, Err>.Success('z'),
                        Result<long, Err>.Success(6L),
                        Result<Guid, Err>.Success(Guid.Empty),
                        default(Result<DateOnly, Err>));
                    break;
            }
        });

        ex.Message.ShouldBe("Cannot All an uninitialized Result.");
    }
}
