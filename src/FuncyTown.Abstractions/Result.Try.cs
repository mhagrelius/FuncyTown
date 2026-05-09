namespace FuncyTown;

/// <summary>
/// Provides Try-style chain helpers that catch exceptions and convert them to Result failures.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Try-style helpers intentionally catch exceptions and convert them into Result failures.")]
public static class ResultTryExtensions
{
    /// <summary>
    /// Transforms a successful value with an exception-throwing function, catching exceptions and converting them to failures.
    /// </summary>
    /// <typeparam name="T">The source success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TNew">The transformed success value type.</typeparam>
    /// <param name="source">The source result.</param>
    /// <param name="selector">The exception-throwing success value transformation.</param>
    /// <param name="code">Optional error code override when an exception is caught.</param>
    /// <param name="message">Optional error message override when an exception is caught.</param>
    /// <returns>A result containing the transformed value, the original failure, or a failure created from a caught exception.</returns>
    public static Result<TNew, TError> MapTry<T, TError, TNew>(
        this Result<T, TError> source,
        Func<T, TNew> selector,
        string? code = null,
        string? message = null)
        where TError : IExceptionalError<TError>
    {
        ArgumentNullException.ThrowIfNull(selector);
        return source.Match(
            value =>
            {
                try
                {
                    return Result<TNew, TError>.Success(selector(value));
                }
                catch (Exception exception)
                {
                    return Result<TNew, TError>.Failure(TError.FromException(exception, code, message));
                }
            },
            Result<TNew, TError>.Failure);
    }

    /// <summary>
    /// Chains a successful value into an exception-throwing Result-returning function, catching exceptions and converting them to failures.
    /// </summary>
    /// <typeparam name="T">The source success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="source">The source result.</param>
    /// <param name="next">The exception-throwing next Result-producing operation.</param>
    /// <param name="code">Optional error code override when an exception is caught.</param>
    /// <param name="message">Optional error message override when an exception is caught.</param>
    /// <returns>The next result, the original failure, or a failure created from a caught exception.</returns>
    public static Result<TNew, TError> ThenTry<T, TError, TNew>(
        this Result<T, TError> source,
        Func<T, Result<TNew, TError>> next,
        string? code = null,
        string? message = null)
        where TError : IExceptionalError<TError>
    {
        ArgumentNullException.ThrowIfNull(next);
        return source.Match(
            value =>
            {
                try
                {
                    return next(value);
                }
                catch (Exception exception)
                {
                    return Result<TNew, TError>.Failure(TError.FromException(exception, code, message));
                }
            },
            Result<TNew, TError>.Failure);
    }

    /// <summary>
    /// Asynchronously transforms a successful value with an exception-throwing function, catching exceptions and converting them to failures.
    /// </summary>
    /// <typeparam name="T">The source success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TNew">The transformed success value type.</typeparam>
    /// <param name="source">The source result.</param>
    /// <param name="selector">The asynchronous exception-throwing success value transformation.</param>
    /// <param name="code">Optional error code override when an exception is caught.</param>
    /// <param name="message">Optional error message override when an exception is caught.</param>
    /// <returns>A task producing a result containing the transformed value, the original failure, or a failure created from a caught exception.</returns>
    public static Task<Result<TNew, TError>> MapTryAsync<T, TError, TNew>(
        this Result<T, TError> source,
        Func<T, Task<TNew>> selector,
        string? code = null,
        string? message = null)
        where TError : IExceptionalError<TError>
    {
        ArgumentNullException.ThrowIfNull(selector);
        return source.Match(
            async value =>
            {
                try
                {
                    return Result<TNew, TError>.Success(await selector(value).ConfigureAwait(false));
                }
                catch (Exception exception)
                {
                    return Result<TNew, TError>.Failure(TError.FromException(exception, code, message));
                }
            },
            error => Task.FromResult(Result<TNew, TError>.Failure(error)));
    }

    /// <summary>
    /// Asynchronously chains a successful value into an exception-throwing Result-returning function, catching exceptions and converting them to failures.
    /// </summary>
    /// <typeparam name="T">The source success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="source">The source result.</param>
    /// <param name="next">The asynchronous exception-throwing next Result-producing operation.</param>
    /// <param name="code">Optional error code override when an exception is caught.</param>
    /// <param name="message">Optional error message override when an exception is caught.</param>
    /// <returns>A task producing the next result, the original failure, or a failure created from a caught exception.</returns>
    public static Task<Result<TNew, TError>> ThenTryAsync<T, TError, TNew>(
        this Result<T, TError> source,
        Func<T, Task<Result<TNew, TError>>> next,
        string? code = null,
        string? message = null)
        where TError : IExceptionalError<TError>
    {
        ArgumentNullException.ThrowIfNull(next);
        return source.Match(
            async value =>
            {
                try
                {
                    return await next(value).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    return Result<TNew, TError>.Failure(TError.FromException(exception, code, message));
                }
            },
            error => Task.FromResult(Result<TNew, TError>.Failure(error)));
    }
}
