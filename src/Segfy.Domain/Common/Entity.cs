namespace Segfy.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    public override bool Equals(object? obj) => obj is Entity e && Id == e.Id;
    public override int GetHashCode() => Id.GetHashCode();
}
