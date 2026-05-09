namespace FuncyTown;

/// <summary>
/// Optional contract enabling an error type to convert exceptions into failures for Try-style helpers.
/// </summary>
/// <typeparam name="TSelf">The concrete error type that can be created from exceptions.</typeparam>
public interface IExceptionalError<TSelf> : IError
    where TSelf : IExceptionalError<TSelf>
{
    /// <summary>Creates an error instance from an exception.</summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="code">Optional error code override.</param>
    /// <param name="message">Optional error message override.</param>
    /// <returns>An error instance representing the exception.</returns>
    static abstract TSelf FromException(System.Exception exception, string? code = null, string? message = null);
}
