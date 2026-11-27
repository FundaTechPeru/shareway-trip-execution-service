using System.Net;

namespace ShareWay.TripExecution.Infrastructure.Mensajeria.Booking;

public static class ClasificadorDeErroresHttp
{
    /// <summary>
    /// 2xx: publicado. 5xx, 401 (clave aún no configurada en el destino), 408 y 429: reintentable.
    /// Cualquier otro código: rechazo definitivo del mensaje.
    /// </summary>
    public static EstadoPublicacion Clasificar(HttpStatusCode codigo) => (int)codigo switch
    {
        >= 200 and < 300 => EstadoPublicacion.Publicado,
        >= 500 => EstadoPublicacion.Reintentar,
        401 or 408 or 429 => EstadoPublicacion.Reintentar,
        _ => EstadoPublicacion.Fallido
    };
}
