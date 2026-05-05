namespace FuncyTown;

/// <summary>
/// The unit type - a single inhabitant used to represent the absence of a meaningful value
/// (e.g. for void-success Results). All instances are equal.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>The single canonical Unit value.</summary>
    public static Unit Value => default;

    /// <summary>Returns true because every Unit value is equal to every other Unit value.</summary>
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "()";

    /// <summary>Returns true because every Unit value is equal to every other Unit value.</summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>Returns false because every Unit value is equal to every other Unit value.</summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
