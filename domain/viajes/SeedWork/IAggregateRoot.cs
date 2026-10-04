namespace ShareWay.TripExecution.Domain.SeedWork;

public interface IAggregateRoot
{
    IReadOnlyCollection<IEventoDeDominio> EventosDeDominio { get; }
    void LimpiarEventos();
}