namespace FuncyTown;

/// <summary>
/// Marks a partial type as a strongly-typed alias for <see cref="Result{T, TError}"/>.
/// The FuncyTown source generator will fill in factory methods, implicit conversions,
/// state accessors, chain methods, and Match.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ResultAttribute<T, TError> : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="ResultAttribute{T, TError}"/> class.</summary>
    public ResultAttribute() { }

    /// <summary>Gets or initializes a value indicating whether to generate implicit conversions.</summary>
    public bool Implicit { get; init; } = true;

    /// <summary>Gets or initializes a value indicating whether to generate System.Text.Json converter support.</summary>
    public bool Json { get; init; } = true;

    /// <summary>Gets or initializes the generated alias type kind.</summary>
    public ResultKind Kind { get; init; } = ResultKind.Struct;
}

/// <summary>
/// Single-arg form: marks a partial type as a void-success Result alias
/// (backed by <see cref="Result{Unit, TError}"/>).
/// </summary>
/// <typeparam name="TError">The error type.</typeparam>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ResultAttribute<TError> : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="ResultAttribute{TError}"/> class.</summary>
    public ResultAttribute() { }

    /// <summary>Gets or initializes a value indicating whether to generate implicit conversions.</summary>
    public bool Implicit { get; init; } = true;

    /// <summary>Gets or initializes a value indicating whether to generate System.Text.Json converter support.</summary>
    public bool Json { get; init; } = true;

    /// <summary>Gets or initializes the generated alias type kind.</summary>
    public ResultKind Kind { get; init; } = ResultKind.Struct;
}
