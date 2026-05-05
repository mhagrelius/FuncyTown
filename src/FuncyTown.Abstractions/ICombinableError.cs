namespace FuncyTown;

/// <summary>
/// Optional contract enabling errors to be combined via Result.Combine.
/// Generated error unions implement this automatically via a synthetic Many case.
/// </summary>
/// <typeparam name="TSelf">The concrete error type that can combine instances of itself.</typeparam>
public interface ICombinableError<TSelf> : IError
    where TSelf : ICombinableError<TSelf>
{
    /// <summary>Combines multiple errors into a single error instance.</summary>
    /// <param name="errors">The errors to combine.</param>
    /// <returns>The combined error instance.</returns>
    static abstract TSelf Combine(System.Collections.Generic.IReadOnlyList<TSelf> errors);
}
