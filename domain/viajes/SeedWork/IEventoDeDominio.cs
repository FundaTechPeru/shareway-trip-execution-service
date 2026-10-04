namespace ShareWay.TripExecution.Domain.SeedWork;

public interface IEventoDeDominio
{
    Guid IdEvento { get; }
    DateTimeOffset OcurridoEn { get; }
}