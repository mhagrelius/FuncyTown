namespace FuncyTown;

/// <summary>
/// Selects the underlying type kind for a generated Result alias.
/// </summary>
public enum ResultKind
{
    /// <summary>Generate as a <c>readonly partial record struct</c> (default).</summary>
    Struct = 0,

    /// <summary>Generate as a <c>partial class</c> (use for inheritance scenarios).</summary>
    Class = 1,
}
