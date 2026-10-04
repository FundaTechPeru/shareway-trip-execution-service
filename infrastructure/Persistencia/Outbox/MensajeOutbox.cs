using System.Text.Json;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Salientes;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Outbox;

public sealed class MensajeOutbox
{
    private MensajeOutbox() { } 

    public Guid Id { get; private set; }
    public string Tipo { get; private set; } = default!;
    public string Contenido { get; private set; } = default!;
    public DateTimeOffset OcurridoEn { get; private set; }
    public DateTimeOffset? ProcesadoEn { get; private set; }

    public static MensajeOutbox Crear(IEventoDeIntegracion evento) => new()
    {
        Id = evento.IdEvento,
        Tipo = evento.GetType().Name,
        Contenido = JsonSerializer.Serialize(evento, evento.GetType()),
        OcurridoEn = evento.OcurridoEn
    };

    public void MarcarProcesado(DateTimeOffset ahora) => ProcesadoEn = ahora;
}
