namespace GastroGestion.Domain.Common;

/// <summary>
/// Base class for all entities. Identity is determined by the <see cref="Id"/> property.
/// Two entities of the same type with the same Id are considered equal regardless
/// of their other property values.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new DomainException("Entity Id cannot be empty.");
        Id = id;
    }

    // EF Core requires a parameterless constructor for owned-entity materialization.
    // Protected to prevent direct instantiation.
#pragma warning disable CS8618
    protected Entity() { }
#pragma warning restore CS8618

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return Equals((Entity)obj);
    }

    public bool Equals(Entity? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
