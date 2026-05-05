using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace FuncyTown.Generators.Internal;

/// <summary>
/// Wraps an <see cref="ImmutableArray{T}"/> with structural equality so it can be used as the
/// value type of an <c>IIncrementalGenerator</c> pipeline node without invalidating the cache
/// on every rebuild.
///
/// <see cref="ImmutableArray{T}"/> uses reference equality on its underlying array; equating two
/// instances with identical contents returns <see langword="false"/> when the underlying arrays
/// are distinct, which the Roslyn incremental cache relies on for invalidation. This wrapper
/// compares element-by-element via <see cref="EqualityComparer{T}.Default"/>, restoring the
/// "same content → same identity" contract that downstream pipeline stages need.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    private readonly ImmutableArray<T> _array;

    public EquatableArray(ImmutableArray<T> array)
    {
        _array = array.IsDefault ? ImmutableArray<T>.Empty : array;
    }

    public int Length => _array.Length;

    public T this[int index] => _array[index];

    public ImmutableArray<T> AsImmutableArray() => _array;

    public bool Equals(EquatableArray<T> other)
    {
        if (_array.Length != other._array.Length)
        {
            return false;
        }

        for (var i = 0; i < _array.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(_array[i], other._array[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in _array)
            {
                hash = (hash * 31) + (item is null ? 0 : item.GetHashCode());
            }

            return hash;
        }
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    public ImmutableArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_array).GetEnumerator();
}

internal static class EquatableArrayExtensions
{
    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> array)
        where T : IEquatable<T>
    {
        return new EquatableArray<T>(array);
    }
}
