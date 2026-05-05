namespace FuncyTown;

/// <summary>
/// Marks a class as an error union.
/// The FuncyTown source generator will fill in error metadata, exhaustive matching,
/// switching helpers, and combinable error support.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ErrorUnionAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="ErrorUnionAttribute"/> class.</summary>
    public ErrorUnionAttribute() { }
}
