namespace FuncyTown;

/// <summary>Provides combinators for working with multiple results.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1715:Identifiers should have correct prefix",
    Justification = "The public combinator API intentionally uses E for the shared error type parameter, and T/U for value-position type parameters.")]
public static class Result
{
    /// <summary>Combines two successful results into a tuple, or returns the first failure.</summary>
    /// <typeparam name="T1">The first success value type.</typeparam>
    /// <typeparam name="T2">The second success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    /// <returns>A successful tuple when both inputs succeed; otherwise the first failure.</returns>
    public static Result<(T1, T2), E> All<T1, T2, E>(Result<T1, E> r1, Result<T2, E> r2)
    {
        if (TryGetFailure(in r1, out var e1)) { return Result<(T1, T2), E>.Failure(e1!); }
        if (TryGetFailure(in r2, out var e2)) { return Result<(T1, T2), E>.Failure(e2!); }
        return Result<(T1, T2), E>.Success((r1.Value, r2.Value));
    }

    /// <summary>Combines three successful results into a tuple, or returns the first failure.</summary>
    /// <typeparam name="T1">The first success value type.</typeparam>
    /// <typeparam name="T2">The second success value type.</typeparam>
    /// <typeparam name="T3">The third success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    /// <param name="r3">The third result.</param>
    /// <returns>A successful tuple when all inputs succeed; otherwise the first failure.</returns>
    public static Result<(T1, T2, T3), E> All<T1, T2, T3, E>(
        Result<T1, E> r1,
        Result<T2, E> r2,
        Result<T3, E> r3)
    {
        if (TryGetFailure(in r1, out var e1)) { return Result<(T1, T2, T3), E>.Failure(e1!); }
        if (TryGetFailure(in r2, out var e2)) { return Result<(T1, T2, T3), E>.Failure(e2!); }
        if (TryGetFailure(in r3, out var e3)) { return Result<(T1, T2, T3), E>.Failure(e3!); }
        return Result<(T1, T2, T3), E>.Success((r1.Value, r2.Value, r3.Value));
    }

    /// <summary>Combines four successful results into a tuple, or returns the first failure.</summary>
    /// <typeparam name="T1">The first success value type.</typeparam>
    /// <typeparam name="T2">The second success value type.</typeparam>
    /// <typeparam name="T3">The third success value type.</typeparam>
    /// <typeparam name="T4">The fourth success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    /// <param name="r3">The third result.</param>
    /// <param name="r4">The fourth result.</param>
    /// <returns>A successful tuple when all inputs succeed; otherwise the first failure.</returns>
    public static Result<(T1, T2, T3, T4), E> All<T1, T2, T3, T4, E>(
        Result<T1, E> r1,
        Result<T2, E> r2,
        Result<T3, E> r3,
        Result<T4, E> r4)
    {
        if (TryGetFailure(in r1, out var e1)) { return Result<(T1, T2, T3, T4), E>.Failure(e1!); }
        if (TryGetFailure(in r2, out var e2)) { return Result<(T1, T2, T3, T4), E>.Failure(e2!); }
        if (TryGetFailure(in r3, out var e3)) { return Result<(T1, T2, T3, T4), E>.Failure(e3!); }
        if (TryGetFailure(in r4, out var e4)) { return Result<(T1, T2, T3, T4), E>.Failure(e4!); }
        return Result<(T1, T2, T3, T4), E>.Success((r1.Value, r2.Value, r3.Value, r4.Value));
    }

    /// <summary>Combines five successful results into a tuple, or returns the first failure.</summary>
    /// <typeparam name="T1">The first success value type.</typeparam>
    /// <typeparam name="T2">The second success value type.</typeparam>
    /// <typeparam name="T3">The third success value type.</typeparam>
    /// <typeparam name="T4">The fourth success value type.</typeparam>
    /// <typeparam name="T5">The fifth success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    /// <param name="r3">The third result.</param>
    /// <param name="r4">The fourth result.</param>
    /// <param name="r5">The fifth result.</param>
    /// <returns>A successful tuple when all inputs succeed; otherwise the first failure.</returns>
    public static Result<(T1, T2, T3, T4, T5), E> All<T1, T2, T3, T4, T5, E>(
        Result<T1, E> r1,
        Result<T2, E> r2,
        Result<T3, E> r3,
        Result<T4, E> r4,
        Result<T5, E> r5)
    {
        if (TryGetFailure(in r1, out var e1)) { return Result<(T1, T2, T3, T4, T5), E>.Failure(e1!); }
        if (TryGetFailure(in r2, out var e2)) { return Result<(T1, T2, T3, T4, T5), E>.Failure(e2!); }
        if (TryGetFailure(in r3, out var e3)) { return Result<(T1, T2, T3, T4, T5), E>.Failure(e3!); }
        if (TryGetFailure(in r4, out var e4)) { return Result<(T1, T2, T3, T4, T5), E>.Failure(e4!); }
        if (TryGetFailure(in r5, out var e5)) { return Result<(T1, T2, T3, T4, T5), E>.Failure(e5!); }
        return Result<(T1, T2, T3, T4, T5), E>.Success((r1.Value, r2.Value, r3.Value, r4.Value, r5.Value));
    }

    /// <summary>Combines six successful results into a tuple, or returns the first failure.</summary>
    /// <typeparam name="T1">The first success value type.</typeparam>
    /// <typeparam name="T2">The second success value type.</typeparam>
    /// <typeparam name="T3">The third success value type.</typeparam>
    /// <typeparam name="T4">The fourth success value type.</typeparam>
    /// <typeparam name="T5">The fifth success value type.</typeparam>
    /// <typeparam name="T6">The sixth success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    /// <param name="r3">The third result.</param>
    /// <param name="r4">The fourth result.</param>
    /// <param name="r5">The fifth result.</param>
    /// <param name="r6">The sixth result.</param>
    /// <returns>A successful tuple when all inputs succeed; otherwise the first failure.</returns>
    public static Result<(T1, T2, T3, T4, T5, T6), E> All<T1, T2, T3, T4, T5, T6, E>(
        Result<T1, E> r1,
        Result<T2, E> r2,
        Result<T3, E> r3,
        Result<T4, E> r4,
        Result<T5, E> r5,
        Result<T6, E> r6)
    {
        if (TryGetFailure(in r1, out var e1)) { return Result<(T1, T2, T3, T4, T5, T6), E>.Failure(e1!); }
        if (TryGetFailure(in r2, out var e2)) { return Result<(T1, T2, T3, T4, T5, T6), E>.Failure(e2!); }
        if (TryGetFailure(in r3, out var e3)) { return Result<(T1, T2, T3, T4, T5, T6), E>.Failure(e3!); }
        if (TryGetFailure(in r4, out var e4)) { return Result<(T1, T2, T3, T4, T5, T6), E>.Failure(e4!); }
        if (TryGetFailure(in r5, out var e5)) { return Result<(T1, T2, T3, T4, T5, T6), E>.Failure(e5!); }
        if (TryGetFailure(in r6, out var e6)) { return Result<(T1, T2, T3, T4, T5, T6), E>.Failure(e6!); }
        return Result<(T1, T2, T3, T4, T5, T6), E>.Success((r1.Value, r2.Value, r3.Value, r4.Value, r5.Value, r6.Value));
    }

    /// <summary>Combines seven successful results into a tuple, or returns the first failure.</summary>
    /// <typeparam name="T1">The first success value type.</typeparam>
    /// <typeparam name="T2">The second success value type.</typeparam>
    /// <typeparam name="T3">The third success value type.</typeparam>
    /// <typeparam name="T4">The fourth success value type.</typeparam>
    /// <typeparam name="T5">The fifth success value type.</typeparam>
    /// <typeparam name="T6">The sixth success value type.</typeparam>
    /// <typeparam name="T7">The seventh success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    /// <param name="r3">The third result.</param>
    /// <param name="r4">The fourth result.</param>
    /// <param name="r5">The fifth result.</param>
    /// <param name="r6">The sixth result.</param>
    /// <param name="r7">The seventh result.</param>
    /// <returns>A successful tuple when all inputs succeed; otherwise the first failure.</returns>
    public static Result<(T1, T2, T3, T4, T5, T6, T7), E> All<T1, T2, T3, T4, T5, T6, T7, E>(
        Result<T1, E> r1,
        Result<T2, E> r2,
        Result<T3, E> r3,
        Result<T4, E> r4,
        Result<T5, E> r5,
        Result<T6, E> r6,
        Result<T7, E> r7)
    {
        if (TryGetFailure(in r1, out var e1)) { return Result<(T1, T2, T3, T4, T5, T6, T7), E>.Failure(e1!); }
        if (TryGetFailure(in r2, out var e2)) { return Result<(T1, T2, T3, T4, T5, T6, T7), E>.Failure(e2!); }
        if (TryGetFailure(in r3, out var e3)) { return Result<(T1, T2, T3, T4, T5, T6, T7), E>.Failure(e3!); }
        if (TryGetFailure(in r4, out var e4)) { return Result<(T1, T2, T3, T4, T5, T6, T7), E>.Failure(e4!); }
        if (TryGetFailure(in r5, out var e5)) { return Result<(T1, T2, T3, T4, T5, T6, T7), E>.Failure(e5!); }
        if (TryGetFailure(in r6, out var e6)) { return Result<(T1, T2, T3, T4, T5, T6, T7), E>.Failure(e6!); }
        if (TryGetFailure(in r7, out var e7)) { return Result<(T1, T2, T3, T4, T5, T6, T7), E>.Failure(e7!); }
        return Result<(T1, T2, T3, T4, T5, T6, T7), E>.Success((r1.Value, r2.Value, r3.Value, r4.Value, r5.Value, r6.Value, r7.Value));
    }

    /// <summary>Combines eight successful results into a tuple, or returns the first failure.</summary>
    /// <typeparam name="T1">The first success value type.</typeparam>
    /// <typeparam name="T2">The second success value type.</typeparam>
    /// <typeparam name="T3">The third success value type.</typeparam>
    /// <typeparam name="T4">The fourth success value type.</typeparam>
    /// <typeparam name="T5">The fifth success value type.</typeparam>
    /// <typeparam name="T6">The sixth success value type.</typeparam>
    /// <typeparam name="T7">The seventh success value type.</typeparam>
    /// <typeparam name="T8">The eighth success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    /// <param name="r3">The third result.</param>
    /// <param name="r4">The fourth result.</param>
    /// <param name="r5">The fifth result.</param>
    /// <param name="r6">The sixth result.</param>
    /// <param name="r7">The seventh result.</param>
    /// <param name="r8">The eighth result.</param>
    /// <returns>A successful tuple when all inputs succeed; otherwise the first failure.</returns>
    public static Result<(T1, T2, T3, T4, T5, T6, T7, T8), E> All<T1, T2, T3, T4, T5, T6, T7, T8, E>(
        Result<T1, E> r1,
        Result<T2, E> r2,
        Result<T3, E> r3,
        Result<T4, E> r4,
        Result<T5, E> r5,
        Result<T6, E> r6,
        Result<T7, E> r7,
        Result<T8, E> r8)
    {
        if (TryGetFailure(in r1, out var e1)) { return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Failure(e1!); }
        if (TryGetFailure(in r2, out var e2)) { return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Failure(e2!); }
        if (TryGetFailure(in r3, out var e3)) { return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Failure(e3!); }
        if (TryGetFailure(in r4, out var e4)) { return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Failure(e4!); }
        if (TryGetFailure(in r5, out var e5)) { return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Failure(e5!); }
        if (TryGetFailure(in r6, out var e6)) { return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Failure(e6!); }
        if (TryGetFailure(in r7, out var e7)) { return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Failure(e7!); }
        if (TryGetFailure(in r8, out var e8)) { return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Failure(e8!); }
        return Result<(T1, T2, T3, T4, T5, T6, T7, T8), E>.Success((r1.Value, r2.Value, r3.Value, r4.Value, r5.Value, r6.Value, r7.Value, r8.Value));
    }

    /// <summary>Combines homogeneous results, accumulating every failure through the error type.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="E">The combinable failure error type.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>A successful list of values when all inputs succeed; otherwise a combined failure.</returns>
    public static Result<IReadOnlyList<T>, E> Combine<T, E>(params ReadOnlySpan<Result<T, E>> results)
        where E : ICombinableError<E>
    {
        var values = new List<T>(results.Length);
        var errors = new List<E>();

        foreach (var result in results)
        {
            CollectValueOrError(result, values, errors, "Combine");
        }

        return errors.Count == 0
            ? Result<IReadOnlyList<T>, E>.Success(values)
            : Result<IReadOnlyList<T>, E>.Failure(E.Combine(errors));
    }

    /// <summary>Combines homogeneous result arrays, accumulating every failure through the error type.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="E">The combinable failure error type.</typeparam>
    /// <param name="results">The result array to combine.</param>
    /// <returns>A successful list of values when all inputs succeed; otherwise a combined failure.</returns>
    public static Result<IReadOnlyList<T>, E> Combine<T, E>(Result<T, E>[] results)
        where E : ICombinableError<E>
    {
        ArgumentNullException.ThrowIfNull(results);

        return Combine<T, E>(results.AsSpan());
    }

    /// <summary>Transforms each source item into a result, returning all success values or the first failure.</summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <typeparam name="U">The success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="source">The source items to traverse.</param>
    /// <param name="selector">The result-producing selector.</param>
    /// <returns>A successful list of transformed values when every selector result succeeds; otherwise the first failure.</returns>
    public static Result<IReadOnlyList<U>, E> Traverse<T, U, E>(IEnumerable<T> source, Func<T, Result<U, E>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<U>();

        foreach (var item in source)
        {
            if (CollectFirstFailure(selector(item), values, "Traverse") is { } failure)
            {
                return failure;
            }
        }

        return Result<IReadOnlyList<U>, E>.Success(values);
    }

    /// <summary>Asynchronously transforms each source item into a result, returning all success values or the first failure.</summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <typeparam name="U">The success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="source">The source items to traverse.</param>
    /// <param name="selector">The asynchronous result-producing selector.</param>
    /// <returns>A task producing a successful list of transformed values when every selector result succeeds; otherwise the first failure.</returns>
    public static Task<Result<IReadOnlyList<U>, E>> TraverseAsync<T, U, E>(
        IEnumerable<T> source,
        Func<T, Task<Result<U, E>>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseAsyncCore<T, U, E>(source, selector);
    }

    private static async Task<Result<IReadOnlyList<U>, E>> TraverseAsyncCore<T, U, E>(
        IEnumerable<T> source,
        Func<T, Task<Result<U, E>>> selector)
    {
        var values = new List<U>();

        foreach (var item in source)
        {
            var result = await selector(item).ConfigureAwait(false);
            if (CollectFirstFailure(result, values, "Traverse") is { } failure)
            {
                return failure;
            }
        }

        return Result<IReadOnlyList<U>, E>.Success(values);
    }

    /// <summary>Flips a sequence of results into a result containing every success value, or the first failure.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="source">The result sequence to flip.</param>
    /// <returns>A successful list of values when every input succeeds; otherwise the first failure.</returns>
    public static Result<IReadOnlyList<T>, E> Sequence<T, E>(IEnumerable<Result<T, E>> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var values = new List<T>();

        foreach (var result in source)
        {
            if (CollectFirstFailure(result, values, "Sequence") is { } failure)
            {
                return failure;
            }
        }

        return Result<IReadOnlyList<T>, E>.Success(values);
    }

    /// <summary>Flips a sequence of result tasks into a task producing a result containing every success value, or the first failure.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="E">The failure error type.</typeparam>
    /// <param name="source">The result task sequence to flip.</param>
    /// <returns>A task producing a successful list of values when every input task succeeds; otherwise the first failure.</returns>
    public static Task<Result<IReadOnlyList<T>, E>> SequenceAsync<T, E>(IEnumerable<Task<Result<T, E>>> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return SequenceAsyncCore<T, E>(source);
    }

    private static async Task<Result<IReadOnlyList<T>, E>> SequenceAsyncCore<T, E>(
        IEnumerable<Task<Result<T, E>>> source)
    {
        var values = new List<T>();

        foreach (var resultTask in source)
        {
            var result = await resultTask.ConfigureAwait(false);
            if (CollectFirstFailure(result, values, "Sequence") is { } failure)
            {
                return failure;
            }
        }

        return Result<IReadOnlyList<T>, E>.Success(values);
    }

    private static bool TryGetFailure<T, E>(in Result<T, E> result, out E? error)
    {
        if (result.IsFailure)
        {
            error = result.Error;
            return true;
        }

        if (!result.IsSuccess)
        {
            throw new ResultException("Cannot All an uninitialized Result.");
        }

        error = default;
        return false;
    }

    private static void CollectValueOrError<T, E>(in Result<T, E> result, List<T> values, List<E> errors, string operation)
    {
        if (result.IsFailure)
        {
            errors.Add(result.Error);
            return;
        }

        if (result.IsSuccess)
        {
            values.Add(result.Value);
            return;
        }

        throw new ResultException($"Cannot {operation} an uninitialized Result.");
    }

    private static Result<IReadOnlyList<T>, E>? CollectFirstFailure<T, E>(in Result<T, E> result, List<T> values, string operation)
    {
        if (result.IsFailure)
        {
            return Result<IReadOnlyList<T>, E>.Failure(result.Error);
        }

        if (result.IsSuccess)
        {
            values.Add(result.Value);
            return null;
        }

        throw new ResultException($"Cannot {operation} an uninitialized Result.");
    }
}
