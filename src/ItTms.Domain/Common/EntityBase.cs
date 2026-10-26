namespace ItTms.Domain.Common;

/// <summary>
/// Lớp cơ sở cho thực thể có định danh Id.
/// </summary>
public abstract class EntityBase<TId>
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (EqualityComparer<TId>.Default.Equals(Id, default)) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id!);

    public static bool operator ==(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(EntityBase<TId>? left, EntityBase<TId>? right) => !(left == right);
}
