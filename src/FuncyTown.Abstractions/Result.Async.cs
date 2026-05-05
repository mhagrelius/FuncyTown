namespace FuncyTown;

public readonly partial record struct Result<T, TError>
{
    /// <summary>Asynchronously transforms the success value while passing failures through unchanged.</summary>
    /// <typeparam name="TNew">The transformed success value type.</typeparam>
    /// <param name="selector">The asynchronous success value transformation.</param>
    /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
    public async Task<Result<TNew, TError>> MapAsync<TNew>(Func<T, Task<TNew>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return _state switch
        {
            ResultState.Success => Result<TNew, TError>.Success(await selector(_value!).ConfigureAwait(false)),
            ResultState.Failure => Result<TNew, TError>.Failure(_error!),
            _ => throw new ResultException("Cannot MapAsync an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="MapAsync{TNew}"/>.</summary>
    /// <typeparam name="TNew">The transformed success value type.</typeparam>
    /// <param name="selector">The asynchronous success value transformation.</param>
    /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
    public Task<Result<TNew, TError>> TransformAsync<TNew>(Func<T, Task<TNew>> selector) => MapAsync(selector);

    /// <summary>Alias for <see cref="MapAsync{TNew}"/>.</summary>
    /// <typeparam name="TNew">The transformed success value type.</typeparam>
    /// <param name="selector">The asynchronous success value transformation.</param>
    /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
    public Task<Result<TNew, TError>> SelectAsync<TNew>(Func<T, Task<TNew>> selector) => MapAsync(selector);

    /// <summary>Asynchronously chains a successful result into another result while passing failures through unchanged.</summary>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="next">The asynchronous next result-producing operation.</param>
    /// <returns>A task producing the next result for success, or the original error.</returns>
    public async Task<Result<TNew, TError>> BindAsync<TNew>(Func<T, Task<Result<TNew, TError>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return _state switch
        {
            ResultState.Success => await next(_value!).ConfigureAwait(false),
            ResultState.Failure => Result<TNew, TError>.Failure(_error!),
            _ => throw new ResultException("Cannot BindAsync an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="BindAsync{TNew}"/>.</summary>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="next">The asynchronous next result-producing operation.</param>
    /// <returns>A task producing the next result for success, or the original error.</returns>
    public Task<Result<TNew, TError>> ThenAsync<TNew>(Func<T, Task<Result<TNew, TError>>> next) => BindAsync(next);

    /// <summary>Alias for <see cref="BindAsync{TNew}"/>.</summary>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="next">The asynchronous next result-producing operation.</param>
    /// <returns>A task producing the next result for success, or the original error.</returns>
    public Task<Result<TNew, TError>> AndThenAsync<TNew>(Func<T, Task<Result<TNew, TError>>> next) => BindAsync(next);

    /// <summary>Alias for <see cref="BindAsync{TNew}"/>.</summary>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="next">The asynchronous next result-producing operation.</param>
    /// <returns>A task producing the next result for success, or the original error.</returns>
    public Task<Result<TNew, TError>> SelectManyAsync<TNew>(Func<T, Task<Result<TNew, TError>>> next) => BindAsync(next);

    /// <summary>Asynchronously transforms the failure error while passing successes through unchanged.</summary>
    /// <typeparam name="TNewError">The transformed error type.</typeparam>
    /// <param name="selector">The asynchronous failure error transformation.</param>
    /// <returns>A task producing a result containing the original success value, or the transformed error.</returns>
    public async Task<Result<T, TNewError>> MapErrorAsync<TNewError>(Func<TError, Task<TNewError>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return _state switch
        {
            ResultState.Success => Result<T, TNewError>.Success(_value!),
            ResultState.Failure => Result<T, TNewError>.Failure(await selector(_error!).ConfigureAwait(false)),
            _ => throw new ResultException("Cannot MapErrorAsync an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="MapErrorAsync{TNewError}"/>.</summary>
    /// <typeparam name="TNewError">The transformed error type.</typeparam>
    /// <param name="selector">The asynchronous failure error transformation.</param>
    /// <returns>A task producing a result containing the original success value, or the transformed error.</returns>
    public Task<Result<T, TNewError>> TransformErrorAsync<TNewError>(Func<TError, Task<TNewError>> selector) => MapErrorAsync(selector);

    /// <summary>Asynchronously converts a failure into a success value while leaving successes unchanged.</summary>
    /// <param name="fallback">The asynchronous fallback value factory for failures.</param>
    /// <returns>A task producing the original success result, or a successful result containing the fallback value.</returns>
    public async Task<Result<T, TError>> RecoverAsync(Func<TError, Task<T>> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return _state switch
        {
            ResultState.Success => this,
            ResultState.Failure => Success(await fallback(_error!).ConfigureAwait(false)),
            _ => throw new ResultException("Cannot RecoverAsync an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="RecoverAsync"/>.</summary>
    /// <param name="fallback">The asynchronous fallback value factory for failures.</param>
    /// <returns>A task producing the original success result, or a successful result containing the fallback value.</returns>
    public Task<Result<T, TError>> OrElseAsync(Func<TError, Task<T>> fallback) => RecoverAsync(fallback);

    /// <summary>Asynchronously replaces a failure with another result while leaving successes unchanged.</summary>
    /// <param name="fallback">The asynchronous fallback result factory for failures.</param>
    /// <returns>A task producing the original success result, or the result returned by <paramref name="fallback"/>.</returns>
    public async Task<Result<T, TError>> RecoverWithAsync(Func<TError, Task<Result<T, TError>>> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return _state switch
        {
            ResultState.Success => this,
            ResultState.Failure => await fallback(_error!).ConfigureAwait(false),
            _ => throw new ResultException("Cannot RecoverWithAsync on an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="RecoverWithAsync"/>.</summary>
    /// <param name="fallback">The asynchronous fallback result factory for failures.</param>
    /// <returns>A task producing the original success result, or the result returned by <paramref name="fallback"/>.</returns>
    public Task<Result<T, TError>> OrElseThenAsync(Func<TError, Task<Result<T, TError>>> fallback) => RecoverWithAsync(fallback);

    /// <summary>Asynchronously runs an action when this result is successful and returns the same result.</summary>
    /// <param name="action">The asynchronous action to run with the success value.</param>
    /// <returns>A task producing the same result.</returns>
    public async Task<Result<T, TError>> OnSuccessAsync(Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_state == ResultState.Uninitialized)
        {
            throw new ResultException("Cannot OnSuccessAsync on an uninitialized Result.");
        }

        if (_state == ResultState.Success)
        {
            await action(_value!).ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Asynchronously runs an action when this result is failed and returns the same result.</summary>
    /// <param name="action">The asynchronous action to run with the failure error.</param>
    /// <returns>A task producing the same result.</returns>
    public async Task<Result<T, TError>> OnFailureAsync(Func<TError, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_state == ResultState.Uninitialized)
        {
            throw new ResultException("Cannot OnFailureAsync on an uninitialized Result.");
        }

        if (_state == ResultState.Failure)
        {
            await action(_error!).ConfigureAwait(false);
        }

        return this;
    }

    /// <summary>Alias for <see cref="OnSuccessAsync"/>.</summary>
    /// <param name="action">The asynchronous action to run with the success value.</param>
    /// <returns>A task producing the same result.</returns>
    public Task<Result<T, TError>> TapAsync(Func<T, Task> action) => OnSuccessAsync(action);

    /// <summary>Alias for <see cref="OnSuccessAsync"/>.</summary>
    /// <param name="action">The asynchronous action to run with the success value.</param>
    /// <returns>A task producing the same result.</returns>
    public Task<Result<T, TError>> IfSuccessfulAsync(Func<T, Task> action) => OnSuccessAsync(action);

    /// <summary>Alias for <see cref="OnFailureAsync"/>.</summary>
    /// <param name="action">The asynchronous action to run with the failure error.</param>
    /// <returns>A task producing the same result.</returns>
    public Task<Result<T, TError>> TapErrorAsync(Func<TError, Task> action) => OnFailureAsync(action);

    /// <summary>Alias for <see cref="OnFailureAsync"/>.</summary>
    /// <param name="action">The asynchronous action to run with the failure error.</param>
    /// <returns>A task producing the same result.</returns>
    public Task<Result<T, TError>> IfFailedAsync(Func<TError, Task> action) => OnFailureAsync(action);

    /// <summary>Asynchronously requires a successful value to satisfy a predicate, otherwise returns the provided error.</summary>
    /// <param name="predicate">The asynchronous predicate that the success value must satisfy.</param>
    /// <param name="error">The error to use when the predicate fails.</param>
    /// <returns>A task producing the original result when already failed or when the predicate passes; otherwise a failed result.</returns>
    public async Task<Result<T, TError>> EnsureAsync(Func<T, Task<bool>> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _state switch
        {
            ResultState.Success when !await predicate(_value!).ConfigureAwait(false) => Failure(error),
            ResultState.Success => this,
            ResultState.Failure => this,
            _ => throw new ResultException("Cannot EnsureAsync on an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="EnsureAsync"/>.</summary>
    /// <param name="predicate">The asynchronous predicate that the success value must satisfy.</param>
    /// <param name="error">The error to use when the predicate fails.</param>
    /// <returns>A task producing the original result when already failed or when the predicate passes; otherwise a failed result.</returns>
    public Task<Result<T, TError>> ValidateAsync(Func<T, Task<bool>> predicate, TError error) => EnsureAsync(predicate, error);

    /// <summary>Asynchronously collapses this result into a non-result value.</summary>
    /// <typeparam name="TResult">The returned value type.</typeparam>
    /// <param name="onSuccess">The asynchronous function to run for a successful result.</param>
    /// <param name="onFailure">The asynchronous function to run for a failed result.</param>
    /// <returns>A task producing the value returned by the matching branch.</returns>
    public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<TError, Task<TResult>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return _state switch
        {
            ResultState.Success => await onSuccess(_value!).ConfigureAwait(false),
            ResultState.Failure => await onFailure(_error!).ConfigureAwait(false),
            _ => throw new ResultException("Cannot MatchAsync on an uninitialized Result."),
        };
    }
}

/// <summary>
/// Provides chain methods for task-produced <see cref="Result{T, TError}"/> values.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1034:Nested types should not be visible",
    Justification = "C# 14 extension blocks are represented as nested metadata symbols.")]
public static class ResultTaskExtensions
{
    /// <summary>Provides chain methods for a task-produced <see cref="Result{T, TError}"/>.</summary>
    /// <typeparam name="T">The source success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="source">The task producing the source result.</param>
    extension<T, TError>(Task<Result<T, TError>> source)
    {
        /// <summary>Transforms the success value of a task-produced result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The transformed success value type.</typeparam>
        /// <param name="selector">The success value transformation.</param>
        /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
        public async Task<Result<TNew, TError>> Map<TNew>(Func<T, TNew> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);
            var result = await source.ConfigureAwait(false);
            return result.Map(selector);
        }

        /// <summary>Asynchronously transforms the success value of a task-produced result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The transformed success value type.</typeparam>
        /// <param name="selector">The asynchronous success value transformation.</param>
        /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
        public async Task<Result<TNew, TError>> Map<TNew>(Func<T, Task<TNew>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);
            var result = await source.ConfigureAwait(false);
            return await result.MapAsync(selector).ConfigureAwait(false);
        }

        /// <summary>Transforms the success value of a task-produced result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The transformed success value type.</typeparam>
        /// <param name="selector">The success value transformation.</param>
        /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
        public Task<Result<TNew, TError>> Transform<TNew>(Func<T, TNew> selector) => source.Map(selector);

        /// <summary>Asynchronously transforms the success value of a task-produced result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The transformed success value type.</typeparam>
        /// <param name="selector">The asynchronous success value transformation.</param>
        /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
        public Task<Result<TNew, TError>> Transform<TNew>(Func<T, Task<TNew>> selector) => source.Map(selector);

        /// <summary>Transforms the success value of a task-produced result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The transformed success value type.</typeparam>
        /// <param name="selector">The success value transformation.</param>
        /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
        public Task<Result<TNew, TError>> Select<TNew>(Func<T, TNew> selector) => source.Map(selector);

        /// <summary>Asynchronously transforms the success value of a task-produced result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The transformed success value type.</typeparam>
        /// <param name="selector">The asynchronous success value transformation.</param>
        /// <returns>A task producing a result containing the transformed success value, or the original error.</returns>
        public Task<Result<TNew, TError>> Select<TNew>(Func<T, Task<TNew>> selector) => source.Map(selector);

        /// <summary>Chains a task-produced successful result into another result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The next success value type.</typeparam>
        /// <param name="next">The next result-producing operation.</param>
        /// <returns>A task producing the next result for success, or the original error.</returns>
        public async Task<Result<TNew, TError>> Bind<TNew>(Func<T, Result<TNew, TError>> next)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(next);
            var result = await source.ConfigureAwait(false);
            return result.Bind(next);
        }

        /// <summary>Asynchronously chains a task-produced successful result into another result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The next success value type.</typeparam>
        /// <param name="next">The asynchronous next result-producing operation.</param>
        /// <returns>A task producing the next result for success, or the original error.</returns>
        public async Task<Result<TNew, TError>> Bind<TNew>(Func<T, Task<Result<TNew, TError>>> next)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(next);
            var result = await source.ConfigureAwait(false);
            return await result.BindAsync(next).ConfigureAwait(false);
        }

        /// <summary>Chains a task-produced successful result into another result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The next success value type.</typeparam>
        /// <param name="next">The next result-producing operation.</param>
        /// <returns>A task producing the next result for success, or the original error.</returns>
        public Task<Result<TNew, TError>> Then<TNew>(Func<T, Result<TNew, TError>> next) => source.Bind(next);

        /// <summary>Asynchronously chains a task-produced successful result into another result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The next success value type.</typeparam>
        /// <param name="next">The asynchronous next result-producing operation.</param>
        /// <returns>A task producing the next result for success, or the original error.</returns>
        public Task<Result<TNew, TError>> Then<TNew>(Func<T, Task<Result<TNew, TError>>> next) => source.Bind(next);

        /// <summary>Chains a task-produced successful result into another result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The next success value type.</typeparam>
        /// <param name="next">The next result-producing operation.</param>
        /// <returns>A task producing the next result for success, or the original error.</returns>
        public Task<Result<TNew, TError>> AndThen<TNew>(Func<T, Result<TNew, TError>> next) => source.Bind(next);

        /// <summary>Asynchronously chains a task-produced successful result into another result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The next success value type.</typeparam>
        /// <param name="next">The asynchronous next result-producing operation.</param>
        /// <returns>A task producing the next result for success, or the original error.</returns>
        public Task<Result<TNew, TError>> AndThen<TNew>(Func<T, Task<Result<TNew, TError>>> next) => source.Bind(next);

        /// <summary>Chains a task-produced successful result into another result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The next success value type.</typeparam>
        /// <param name="next">The next result-producing operation.</param>
        /// <returns>A task producing the next result for success, or the original error.</returns>
        public Task<Result<TNew, TError>> SelectMany<TNew>(Func<T, Result<TNew, TError>> next) => source.Bind(next);

        /// <summary>Asynchronously chains a task-produced successful result into another result while passing failures through unchanged.</summary>
        /// <typeparam name="TNew">The next success value type.</typeparam>
        /// <param name="next">The asynchronous next result-producing operation.</param>
        /// <returns>A task producing the next result for success, or the original error.</returns>
        public Task<Result<TNew, TError>> SelectMany<TNew>(Func<T, Task<Result<TNew, TError>>> next) => source.Bind(next);

        /// <summary>Transforms the failure error of a task-produced result while passing successes through unchanged.</summary>
        /// <typeparam name="TNewError">The transformed error type.</typeparam>
        /// <param name="selector">The failure error transformation.</param>
        /// <returns>A task producing a result containing the original success value, or the transformed error.</returns>
        public async Task<Result<T, TNewError>> MapError<TNewError>(Func<TError, TNewError> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);
            var result = await source.ConfigureAwait(false);
            return result.MapError(selector);
        }

        /// <summary>Asynchronously transforms the failure error of a task-produced result while passing successes through unchanged.</summary>
        /// <typeparam name="TNewError">The transformed error type.</typeparam>
        /// <param name="selector">The asynchronous failure error transformation.</param>
        /// <returns>A task producing a result containing the original success value, or the transformed error.</returns>
        public async Task<Result<T, TNewError>> MapError<TNewError>(Func<TError, Task<TNewError>> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);
            var result = await source.ConfigureAwait(false);
            return await result.MapErrorAsync(selector).ConfigureAwait(false);
        }

        /// <summary>Transforms the failure error of a task-produced result while passing successes through unchanged.</summary>
        /// <typeparam name="TNewError">The transformed error type.</typeparam>
        /// <param name="selector">The failure error transformation.</param>
        /// <returns>A task producing a result containing the original success value, or the transformed error.</returns>
        public Task<Result<T, TNewError>> TransformError<TNewError>(Func<TError, TNewError> selector) => source.MapError(selector);

        /// <summary>Asynchronously transforms the failure error of a task-produced result while passing successes through unchanged.</summary>
        /// <typeparam name="TNewError">The transformed error type.</typeparam>
        /// <param name="selector">The asynchronous failure error transformation.</param>
        /// <returns>A task producing a result containing the original success value, or the transformed error.</returns>
        public Task<Result<T, TNewError>> TransformError<TNewError>(Func<TError, Task<TNewError>> selector) => source.MapError(selector);

        /// <summary>Converts a task-produced failure into a success value while leaving successes unchanged.</summary>
        /// <param name="fallback">The fallback value factory for failures.</param>
        /// <returns>A task producing the original success result, or a successful result containing the fallback value.</returns>
        public async Task<Result<T, TError>> Recover(Func<TError, T> fallback)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(fallback);
            var result = await source.ConfigureAwait(false);
            return result.Recover(fallback);
        }

        /// <summary>Asynchronously converts a task-produced failure into a success value while leaving successes unchanged.</summary>
        /// <param name="fallback">The asynchronous fallback value factory for failures.</param>
        /// <returns>A task producing the original success result, or a successful result containing the fallback value.</returns>
        public async Task<Result<T, TError>> Recover(Func<TError, Task<T>> fallback)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(fallback);
            var result = await source.ConfigureAwait(false);
            return await result.RecoverAsync(fallback).ConfigureAwait(false);
        }

        /// <summary>Converts a task-produced failure into a success value while leaving successes unchanged.</summary>
        /// <param name="fallback">The fallback value factory for failures.</param>
        /// <returns>A task producing the original success result, or a successful result containing the fallback value.</returns>
        public Task<Result<T, TError>> OrElse(Func<TError, T> fallback) => source.Recover(fallback);

        /// <summary>Asynchronously converts a task-produced failure into a success value while leaving successes unchanged.</summary>
        /// <param name="fallback">The asynchronous fallback value factory for failures.</param>
        /// <returns>A task producing the original success result, or a successful result containing the fallback value.</returns>
        public Task<Result<T, TError>> OrElse(Func<TError, Task<T>> fallback) => source.Recover(fallback);

        /// <summary>Replaces a task-produced failure with another result while leaving successes unchanged.</summary>
        /// <param name="fallback">The fallback result factory for failures.</param>
        /// <returns>A task producing the original success result, or the result returned by <paramref name="fallback"/>.</returns>
        public async Task<Result<T, TError>> RecoverWith(Func<TError, Result<T, TError>> fallback)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(fallback);
            var result = await source.ConfigureAwait(false);
            return result.RecoverWith(fallback);
        }

        /// <summary>Asynchronously replaces a task-produced failure with another result while leaving successes unchanged.</summary>
        /// <param name="fallback">The asynchronous fallback result factory for failures.</param>
        /// <returns>A task producing the original success result, or the result returned by <paramref name="fallback"/>.</returns>
        public async Task<Result<T, TError>> RecoverWith(Func<TError, Task<Result<T, TError>>> fallback)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(fallback);
            var result = await source.ConfigureAwait(false);
            return await result.RecoverWithAsync(fallback).ConfigureAwait(false);
        }

        /// <summary>Replaces a task-produced failure with another result while leaving successes unchanged.</summary>
        /// <param name="fallback">The fallback result factory for failures.</param>
        /// <returns>A task producing the original success result, or the result returned by <paramref name="fallback"/>.</returns>
        public Task<Result<T, TError>> OrElseThen(Func<TError, Result<T, TError>> fallback) => source.RecoverWith(fallback);

        /// <summary>Asynchronously replaces a task-produced failure with another result while leaving successes unchanged.</summary>
        /// <param name="fallback">The asynchronous fallback result factory for failures.</param>
        /// <returns>A task producing the original success result, or the result returned by <paramref name="fallback"/>.</returns>
        public Task<Result<T, TError>> OrElseThen(Func<TError, Task<Result<T, TError>>> fallback) => source.RecoverWith(fallback);

        /// <summary>Runs an action when a task-produced result is successful and returns the same result.</summary>
        /// <param name="action">The action to run with the success value.</param>
        /// <returns>A task producing the same result.</returns>
        public async Task<Result<T, TError>> OnSuccess(Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(action);
            var result = await source.ConfigureAwait(false);
            return result.OnSuccess(action);
        }

        /// <summary>Asynchronously runs an action when a task-produced result is successful and returns the same result.</summary>
        /// <param name="action">The asynchronous action to run with the success value.</param>
        /// <returns>A task producing the same result.</returns>
        public async Task<Result<T, TError>> OnSuccess(Func<T, Task> action)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(action);
            var result = await source.ConfigureAwait(false);
            return await result.OnSuccessAsync(action).ConfigureAwait(false);
        }

        /// <summary>Runs an action when a task-produced result is failed and returns the same result.</summary>
        /// <param name="action">The action to run with the failure error.</param>
        /// <returns>A task producing the same result.</returns>
        public async Task<Result<T, TError>> OnFailure(Action<TError> action)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(action);
            var result = await source.ConfigureAwait(false);
            return result.OnFailure(action);
        }

        /// <summary>Asynchronously runs an action when a task-produced result is failed and returns the same result.</summary>
        /// <param name="action">The asynchronous action to run with the failure error.</param>
        /// <returns>A task producing the same result.</returns>
        public async Task<Result<T, TError>> OnFailure(Func<TError, Task> action)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(action);
            var result = await source.ConfigureAwait(false);
            return await result.OnFailureAsync(action).ConfigureAwait(false);
        }

        /// <summary>Runs an action when a task-produced result is successful and returns the same result.</summary>
        /// <param name="action">The action to run with the success value.</param>
        /// <returns>A task producing the same result.</returns>
        public Task<Result<T, TError>> Tap(Action<T> action) => source.OnSuccess(action);

        /// <summary>Asynchronously runs an action when a task-produced result is successful and returns the same result.</summary>
        /// <param name="action">The asynchronous action to run with the success value.</param>
        /// <returns>A task producing the same result.</returns>
        public Task<Result<T, TError>> Tap(Func<T, Task> action) => source.OnSuccess(action);

        /// <summary>Runs an action when a task-produced result is successful and returns the same result.</summary>
        /// <param name="action">The action to run with the success value.</param>
        /// <returns>A task producing the same result.</returns>
        public Task<Result<T, TError>> IfSuccessful(Action<T> action) => source.OnSuccess(action);

        /// <summary>Asynchronously runs an action when a task-produced result is successful and returns the same result.</summary>
        /// <param name="action">The asynchronous action to run with the success value.</param>
        /// <returns>A task producing the same result.</returns>
        public Task<Result<T, TError>> IfSuccessful(Func<T, Task> action) => source.OnSuccess(action);

        /// <summary>Runs an action when a task-produced result is failed and returns the same result.</summary>
        /// <param name="action">The action to run with the failure error.</param>
        /// <returns>A task producing the same result.</returns>
        public Task<Result<T, TError>> TapError(Action<TError> action) => source.OnFailure(action);

        /// <summary>Asynchronously runs an action when a task-produced result is failed and returns the same result.</summary>
        /// <param name="action">The asynchronous action to run with the failure error.</param>
        /// <returns>A task producing the same result.</returns>
        public Task<Result<T, TError>> TapError(Func<TError, Task> action) => source.OnFailure(action);

        /// <summary>Runs an action when a task-produced result is failed and returns the same result.</summary>
        /// <param name="action">The action to run with the failure error.</param>
        /// <returns>A task producing the same result.</returns>
        public Task<Result<T, TError>> IfFailed(Action<TError> action) => source.OnFailure(action);

        /// <summary>Asynchronously runs an action when a task-produced result is failed and returns the same result.</summary>
        /// <param name="action">The asynchronous action to run with the failure error.</param>
        /// <returns>A task producing the same result.</returns>
        public Task<Result<T, TError>> IfFailed(Func<TError, Task> action) => source.OnFailure(action);

        /// <summary>Requires a successful value of a task-produced result to satisfy a predicate, otherwise returns the provided error.</summary>
        /// <param name="predicate">The predicate that the success value must satisfy.</param>
        /// <param name="error">The error to use when the predicate fails.</param>
        /// <returns>A task producing the original result when already failed or when the predicate passes; otherwise a failed result.</returns>
        public async Task<Result<T, TError>> Ensure(Func<T, bool> predicate, TError error)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);
            var result = await source.ConfigureAwait(false);
            return result.Ensure(predicate, error);
        }

        /// <summary>Asynchronously requires a successful value of a task-produced result to satisfy a predicate, otherwise returns the provided error.</summary>
        /// <param name="predicate">The asynchronous predicate that the success value must satisfy.</param>
        /// <param name="error">The error to use when the predicate fails.</param>
        /// <returns>A task producing the original result when already failed or when the predicate passes; otherwise a failed result.</returns>
        public async Task<Result<T, TError>> Ensure(Func<T, Task<bool>> predicate, TError error)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);
            var result = await source.ConfigureAwait(false);
            return await result.EnsureAsync(predicate, error).ConfigureAwait(false);
        }

        /// <summary>Requires a successful value of a task-produced result to satisfy a predicate, otherwise returns the provided error.</summary>
        /// <param name="predicate">The predicate that the success value must satisfy.</param>
        /// <param name="error">The error to use when the predicate fails.</param>
        /// <returns>A task producing the original result when already failed or when the predicate passes; otherwise a failed result.</returns>
        public Task<Result<T, TError>> Validate(Func<T, bool> predicate, TError error) => source.Ensure(predicate, error);

        /// <summary>Asynchronously requires a successful value of a task-produced result to satisfy a predicate, otherwise returns the provided error.</summary>
        /// <param name="predicate">The asynchronous predicate that the success value must satisfy.</param>
        /// <param name="error">The error to use when the predicate fails.</param>
        /// <returns>A task producing the original result when already failed or when the predicate passes; otherwise a failed result.</returns>
        public Task<Result<T, TError>> Validate(Func<T, Task<bool>> predicate, TError error) => source.Ensure(predicate, error);

        /// <summary>Collapses a task-produced result into a non-result value.</summary>
        /// <typeparam name="TResult">The returned value type.</typeparam>
        /// <param name="onSuccess">The function to run for a successful result.</param>
        /// <param name="onFailure">The function to run for a failed result.</param>
        /// <returns>A task producing the value returned by the matching branch.</returns>
        public async Task<TResult> Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);
            var result = await source.ConfigureAwait(false);
            return result.Match(onSuccess, onFailure);
        }

        /// <summary>Asynchronously collapses a task-produced result into a non-result value.</summary>
        /// <typeparam name="TResult">The returned value type.</typeparam>
        /// <param name="onSuccess">The asynchronous function to run for a successful result.</param>
        /// <param name="onFailure">The asynchronous function to run for a failed result.</param>
        /// <returns>A task producing the value returned by the matching branch.</returns>
        public async Task<TResult> Match<TResult>(Func<T, Task<TResult>> onSuccess, Func<TError, Task<TResult>> onFailure)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);
            var result = await source.ConfigureAwait(false);
            return await result.MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
        }
    }
}
