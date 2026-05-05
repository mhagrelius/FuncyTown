namespace FuncyTown;

/// <summary>
/// Thrown when a Result is accessed in a way that is incompatible with its current state
/// (e.g. reading <c>Value</c> on a failure, or any property on a default-initialized Result).
/// </summary>
public sealed class ResultException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="ResultException"/> class.</summary>
    public ResultException() { }

    /// <summary>Initializes a new instance of the <see cref="ResultException"/> class with a message.</summary>
    public ResultException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="ResultException"/> class with a message and inner exception.</summary>
    public ResultException(string message, Exception innerException) : base(message, innerException) { }
}
