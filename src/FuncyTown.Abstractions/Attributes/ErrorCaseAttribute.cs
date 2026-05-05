namespace FuncyTown;

/// <summary>
/// Marks a class as a case of an error union.
/// The FuncyTown source generator includes decorated cases in the generated union API.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ErrorCaseAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="ErrorCaseAttribute"/> class.</summary>
    public ErrorCaseAttribute() { }
}
