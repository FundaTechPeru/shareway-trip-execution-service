namespace ShareWay.TripExecution.Infrastructure.Mensajeria;

public interface IBrokerDeMensajes
{
    /// <param name="tipo">Nombre del evento de integración (columna <c>Tipo</c> del outbox).</param>
    /// <param name="contenidoJson">Evento de integración serializado (columna <c>Contenido</c>).</param>
    Task<ResultadoPublicacion> PublicarAsync(string tipo, string contenidoJson, CancellationToken ct);
}

public enum EstadoPublicacion
{
    /// <summary>El destino aceptó el evento.</summary>
    Publicado,
    /// <summary>El evento no se envía a ningún destino; se da por procesado.</summary>
    Omitido,
    /// <summary>Fallo transitorio: se reintenta más tarde respetando el orden.</summary>
    Reintentar,
    /// <summary>El destino rechazó el evento de forma definitiva.</summary>
    Fallido
}

public sealed record ResultadoPublicacion(EstadoPublicacion Estado, string? Error = null)
{
    public static readonly ResultadoPublicacion Publicado = new(EstadoPublicacion.Publicado);
    public static readonly ResultadoPublicacion Omitido = new(EstadoPublicacion.Omitido);
    public static ResultadoPublicacion Reintentar(string error) => new(EstadoPublicacion.Reintentar, error);
    public static ResultadoPublicacion Fallido(string error) => new(EstadoPublicacion.Fallido, error);
}
