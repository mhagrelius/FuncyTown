namespace FuncyTown;

/// <summary>
/// Optional contract for error types used with <c>Result&lt;T, TError&gt;</c>.
/// Implementing this is not required - any reference or value type can serve as TError -
/// but doing so unlocks consistent diagnostics, logging, and (in later phases) analyzer
/// awareness of generated error unions.
/// </summary>
public interface IError
{
    /// <summary>A short, stable identifier for this error category (e.g. "NotFound").</summary>
    string Code { get; }

    /// <summary>A human-readable description of the error.</summary>
    string Message { get; }
}
