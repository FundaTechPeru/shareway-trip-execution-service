namespace ShareWay.TripExecution.Domain.SeedWork;

public abstract class AggregateRoot<TId> : Entidad<TId>, IAggregateRoot where TId : notnull
{
    private readonly List<IEventoDeDominio> _eventos = new();

    public IReadOnlyCollection<IEventoDeDominio> EventosDeDominio => _eventos.AsReadOnly();

    public void LimpiarEventos() => _eventos.Clear();

    protected void LevantarEvento(IEventoDeDominio evento) => _eventos.Add(evento);
}
