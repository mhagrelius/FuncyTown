namespace FuncyTown;

/// <summary>
/// A basic <see cref="IError"/> implementation for simple failures, prototypes, and adapter boundaries.
/// </summary>
/// <param name="Code">A short, stable identifier for this error category.</param>
/// <param name="Message">A human-readable description of the error.</param>
/// <param name="Exception">Optional local diagnostic context for failures caused by an exception.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Error is the intentional public name for the simple built-in IError implementation.")]
public sealed record Error(
    string Code,
    string Message,
    [property: System.Text.Json.Serialization.JsonIgnore] System.Exception? Exception = null) : IExceptionalError<Error>
{
    /// <summary>Creates an <see cref="Error"/> from an exception.</summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="code">Optional error code. Defaults to the exception type name.</param>
    /// <param name="message">Optional error message. Defaults to the exception message.</param>
    /// <returns>An error containing the exception as local diagnostic context.</returns>
    public static Error FromException(System.Exception exception, string? code = null, string? message = null)
    {
        System.ArgumentNullException.ThrowIfNull(exception);
        return new Error(code ?? exception.GetType().Name, message ?? exception.Message, exception);
    }
}
