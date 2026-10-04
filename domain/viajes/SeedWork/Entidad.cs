namespace ShareWay.TripExecution.Domain.SeedWork;

public abstract class Entidad<TId> : IEquatable<Entidad<TId>> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    public bool Equals(Entidad<TId>? otra) =>
        otra is not null
        && GetType() == otra.GetType()
        && EqualityComparer<TId>.Default.Equals(Id, otra.Id);

    public override bool Equals(object? obj) => obj is Entidad<TId> otra && Equals(otra);
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
