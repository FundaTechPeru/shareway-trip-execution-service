namespace ShareWay.TripExecution.Domain.SeedWork;

public abstract record EventoDeDominio(DateTimeOffset OcurridoEn) : IEventoDeDominio
{
    public Guid IdEvento { get; } = Guid.NewGuid();
}