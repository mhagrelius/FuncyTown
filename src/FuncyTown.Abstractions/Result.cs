namespace FuncyTown;

/// <summary>
/// Represents the outcome of an operation that can either produce a value of type
/// <typeparamref name="T"/> or fail with an error of type <typeparamref name="TError"/>.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
/// <typeparam name="TError">The failure error type.</typeparam>
[System.Text.Json.Serialization.JsonConverter(typeof(ResultJsonConverterFactory))]
public readonly partial record struct Result<T, TError>
{
    private readonly T? _value;
    private readonly TError? _error;
    private readonly ResultState _state;

    private Result(T? value, TError? error, ResultState state)
    {
        _value = value;
        _error = error;
        _state = state;
    }

    /// <summary>Creates a successful result.</summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful result containing <paramref name="value"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Static factories are the designed construction API for Result<T, TError>.")]
    public static Result<T, TError> Success(T value) =>
        new(value, default, ResultState.Success);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The failure error.</param>
    /// <returns>A failed result containing <paramref name="error"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Static factories are the designed construction API for Result<T, TError>.")]
    public static Result<T, TError> Failure(TError error) =>
        new(default, error, ResultState.Failure);

    /// <summary>Gets a value indicating whether this result is successful.</summary>
    public bool IsSuccess => _state == ResultState.Success;

    /// <summary>Gets a value indicating whether this result is failed.</summary>
    public bool IsFailure => _state == ResultState.Failure;

    /// <summary>
    /// Gets a value indicating whether this result is uninitialized — that is, equal to
    /// <c>default(Result&lt;T, TError&gt;)</c>. Uninitialized results throw <see cref="ResultException"/>
    /// on every chain method, so this accessor is the safe way to detect them in defensive code paths
    /// such as deserialization or interop boundaries.
    /// </summary>
    public bool IsUninitialized => _state == ResultState.Uninitialized;

    /// <summary>Gets the success value, or throws if this result is not successful.</summary>
    public T Value => _state == ResultState.Success
        ? _value!
        : throw new ResultException(_state == ResultState.Failure
            ? "Cannot access Value on a failed Result."
            : "Cannot access Value on an uninitialized Result.");

    /// <summary>Gets the failure error, or throws if this result is not failed.</summary>
    public TError Error => _state == ResultState.Failure
        ? _error!
        : throw new ResultException(_state == ResultState.Success
            ? "Cannot access Error on a successful Result."
            : "Cannot access Error on an uninitialized Result.");

    /// <inheritdoc />
    public override string ToString() => _state switch
    {
        ResultState.Success => string.Concat("Success(", _value, ")"),
        ResultState.Failure => string.Concat("Failure(", _error, ")"),
        _ => "Uninitialized",
    };

    /// <summary>Transforms the success value while passing failures through unchanged.</summary>
    /// <typeparam name="TNew">The transformed success value type.</typeparam>
    /// <param name="selector">The success value transformation.</param>
    /// <returns>A result containing the transformed success value, or the original error.</returns>
    public Result<TNew, TError> Map<TNew>(Func<T, TNew> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return _state switch
        {
            ResultState.Success => Result<TNew, TError>.Success(selector(_value!)),
            ResultState.Failure => Result<TNew, TError>.Failure(_error!),
            _ => throw new ResultException("Cannot Map an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="Map{TNew}"/>.</summary>
    /// <typeparam name="TNew">The transformed success value type.</typeparam>
    /// <param name="selector">The success value transformation.</param>
    /// <returns>A result containing the transformed success value, or the original error.</returns>
    public Result<TNew, TError> Transform<TNew>(Func<T, TNew> selector) => Map(selector);

    /// <summary>Alias for <see cref="Map{TNew}"/>.</summary>
    /// <typeparam name="TNew">The transformed success value type.</typeparam>
    /// <param name="selector">The success value transformation.</param>
    /// <returns>A result containing the transformed success value, or the original error.</returns>
    public Result<TNew, TError> Select<TNew>(Func<T, TNew> selector) => Map(selector);

    /// <summary>Chains a successful result into another result while passing failures through unchanged.</summary>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="next">The next result-producing operation.</param>
    /// <returns>The next result for success, or the original error.</returns>
    public Result<TNew, TError> Bind<TNew>(Func<T, Result<TNew, TError>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return _state switch
        {
            ResultState.Success => next(_value!),
            ResultState.Failure => Result<TNew, TError>.Failure(_error!),
            _ => throw new ResultException("Cannot Bind an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="Bind{TNew}"/>.</summary>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="next">The next result-producing operation.</param>
    /// <returns>The next result for success, or the original error.</returns>
    public Result<TNew, TError> Then<TNew>(Func<T, Result<TNew, TError>> next) => Bind(next);

    /// <summary>Alias for <see cref="Bind{TNew}"/>.</summary>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="next">The next result-producing operation.</param>
    /// <returns>The next result for success, or the original error.</returns>
    public Result<TNew, TError> AndThen<TNew>(Func<T, Result<TNew, TError>> next) => Bind(next);

    /// <summary>Alias for <see cref="Bind{TNew}"/>.</summary>
    /// <typeparam name="TNew">The next success value type.</typeparam>
    /// <param name="next">The next result-producing operation.</param>
    /// <returns>The next result for success, or the original error.</returns>
    public Result<TNew, TError> SelectMany<TNew>(Func<T, Result<TNew, TError>> next) => Bind(next);

    /// <summary>Transforms the failure error while passing successes through unchanged.</summary>
    /// <typeparam name="TNewError">The transformed error type.</typeparam>
    /// <param name="selector">The failure error transformation.</param>
    /// <returns>A result containing the original success value, or the transformed error.</returns>
    public Result<T, TNewError> MapError<TNewError>(Func<TError, TNewError> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return _state switch
        {
            ResultState.Success => Result<T, TNewError>.Success(_value!),
            ResultState.Failure => Result<T, TNewError>.Failure(selector(_error!)),
            _ => throw new ResultException("Cannot MapError an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="MapError{TNewError}"/>.</summary>
    /// <typeparam name="TNewError">The transformed error type.</typeparam>
    /// <param name="selector">The failure error transformation.</param>
    /// <returns>A result containing the original success value, or the transformed error.</returns>
    public Result<T, TNewError> TransformError<TNewError>(Func<TError, TNewError> selector) => MapError(selector);

    /// <summary>Converts a failure into a success value while leaving successes unchanged.</summary>
    /// <param name="fallback">The fallback value factory for failures.</param>
    /// <returns>The original success result, or a successful result containing the fallback value.</returns>
    public Result<T, TError> Recover(Func<TError, T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return _state switch
        {
            ResultState.Success => this,
            ResultState.Failure => Success(fallback(_error!)),
            _ => throw new ResultException("Cannot Recover an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="Recover"/>.</summary>
    /// <param name="fallback">The fallback value factory for failures.</param>
    /// <returns>The original success result, or a successful result containing the fallback value.</returns>
    public Result<T, TError> OrElse(Func<TError, T> fallback) => Recover(fallback);

    /// <summary>Replaces a failure with another result while leaving successes unchanged.</summary>
    /// <param name="fallback">The fallback result factory for failures.</param>
    /// <returns>The original success result, or the result returned by <paramref name="fallback"/>.</returns>
    public Result<T, TError> RecoverWith(Func<TError, Result<T, TError>> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return _state switch
        {
            ResultState.Success => this,
            ResultState.Failure => fallback(_error!),
            _ => throw new ResultException("Cannot RecoverWith on an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="RecoverWith"/>.</summary>
    /// <param name="fallback">The fallback result factory for failures.</param>
    /// <returns>The original success result, or the result returned by <paramref name="fallback"/>.</returns>
    public Result<T, TError> OrElseThen(Func<TError, Result<T, TError>> fallback) => RecoverWith(fallback);

    /// <summary>Runs an action when this result is successful and returns the same result.</summary>
    /// <param name="action">The action to run with the success value.</param>
    /// <returns>The same result.</returns>
    public Result<T, TError> OnSuccess(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_state == ResultState.Uninitialized)
        {
            throw new ResultException("Cannot OnSuccess on an uninitialized Result.");
        }

        if (_state == ResultState.Success)
        {
            action(_value!);
        }

        return this;
    }

    /// <summary>Runs an action when this result is failed and returns the same result.</summary>
    /// <param name="action">The action to run with the failure error.</param>
    /// <returns>The same result.</returns>
    public Result<T, TError> OnFailure(Action<TError> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_state == ResultState.Uninitialized)
        {
            throw new ResultException("Cannot OnFailure on an uninitialized Result.");
        }

        if (_state == ResultState.Failure)
        {
            action(_error!);
        }

        return this;
    }

    /// <summary>Alias for <see cref="OnSuccess"/>.</summary>
    /// <param name="action">The action to run with the success value.</param>
    /// <returns>The same result.</returns>
    public Result<T, TError> Tap(Action<T> action) => OnSuccess(action);

    /// <summary>Alias for <see cref="OnSuccess"/>.</summary>
    /// <param name="action">The action to run with the success value.</param>
    /// <returns>The same result.</returns>
    public Result<T, TError> IfSuccessful(Action<T> action) => OnSuccess(action);

    /// <summary>Alias for <see cref="OnFailure"/>.</summary>
    /// <param name="action">The action to run with the failure error.</param>
    /// <returns>The same result.</returns>
    public Result<T, TError> TapError(Action<TError> action) => OnFailure(action);

    /// <summary>Alias for <see cref="OnFailure"/>.</summary>
    /// <param name="action">The action to run with the failure error.</param>
    /// <returns>The same result.</returns>
    public Result<T, TError> IfFailed(Action<TError> action) => OnFailure(action);

    /// <summary>Requires a successful value to satisfy a predicate, otherwise returns the provided error.</summary>
    /// <param name="predicate">The predicate that the success value must satisfy.</param>
    /// <param name="error">The error to use when the predicate fails.</param>
    /// <returns>The original result when already failed or when the predicate passes; otherwise a failed result.</returns>
    public Result<T, TError> Ensure(Func<T, bool> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _state switch
        {
            ResultState.Success when !predicate(_value!) => Failure(error),
            ResultState.Success => this,
            ResultState.Failure => this,
            _ => throw new ResultException("Cannot Ensure on an uninitialized Result."),
        };
    }

    /// <summary>Alias for <see cref="Ensure"/>.</summary>
    /// <param name="predicate">The predicate that the success value must satisfy.</param>
    /// <param name="error">The error to use when the predicate fails.</param>
    /// <returns>The original result when already failed or when the predicate passes; otherwise a failed result.</returns>
    public Result<T, TError> Validate(Func<T, bool> predicate, TError error) => Ensure(predicate, error);

    /// <summary>Collapses this result into a non-result value.</summary>
    /// <typeparam name="TResult">The returned value type.</typeparam>
    /// <param name="onSuccess">The function to run for a successful result.</param>
    /// <param name="onFailure">The function to run for a failed result.</param>
    /// <returns>The value returned by the matching branch.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return _state switch
        {
            ResultState.Success => onSuccess(_value!),
            ResultState.Failure => onFailure(_error!),
            _ => throw new ResultException("Cannot Match on an uninitialized Result."),
        };
    }
}
