using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShareWay.TripExecution.Infrastructure.Seguridad;

namespace ShareWay.TripExecution.Infrastructure.Mensajeria.Booking;

/// <summary>Publica los eventos del outbox en Booking &amp; Pricing por HTTP.</summary>
internal sealed class BrokerHttpBooking(HttpClient http, ClaveInterna clave, ILogger<BrokerHttpBooking> log) : IBrokerDeMensajes
{
    public const string RutaEventos = "api/v1/internal/events";
    public const string UrlPorDefecto = "https://shareway-booking-pricing.onrender.com";
    /// <summary>Los servicios gratuitos de Render tardan hasta un minuto en despertar.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);
    private const int MaximoCuerpoEnError = 500;

    public static Uri DireccionBase(string? url)
    {
        var valor = string.IsNullOrWhiteSpace(url) ? UrlPorDefecto : url.Trim();
        // Con barra final, la ruta relativa se agrega a la base en vez de reemplazar su último segmento.
        return new Uri(valor.EndsWith('/') ? valor : valor + "/", UriKind.Absolute);
    }

    public async Task<ResultadoPublicacion> PublicarAsync(string tipo, string contenidoJson, CancellationToken ct)
    {
        string? cuerpo;
        try
        {
            cuerpo = ContratoEventosBooking.Construir(tipo, contenidoJson)?.ToJsonString();
        }
        catch (JsonException ex)
        {
            return ResultadoPublicacion.Fallido($"Contenido de {tipo} ilegible: {ex.Message}");
        }

        if (cuerpo is null)
            return ResultadoPublicacion.Omitido;

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, RutaEventos)
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
        };
        solicitud.Headers.Add(ClaveInterna.Encabezado, clave.Valor);

        try
        {
            using var respuesta = await http.SendAsync(solicitud, ct);
            var estado = ClasificadorDeErroresHttp.Clasificar(respuesta.StatusCode);
            if (estado == EstadoPublicacion.Publicado)
            {
                log.LogInformation("Evento {Tipo} enviado a Booking ({Codigo}).", tipo, (int)respuesta.StatusCode);
                return ResultadoPublicacion.Publicado;
            }

            var detalle = await LeerDetalleAsync(respuesta, ct);
            var error = $"Booking respondió {(int)respuesta.StatusCode} {respuesta.ReasonPhrase}: {detalle}";
            return estado == EstadoPublicacion.Reintentar
                ? ResultadoPublicacion.Reintentar(error)
                : ResultadoPublicacion.Fallido(error);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ResultadoPublicacion.Reintentar($"Timeout de {Timeout.TotalSeconds:0} s esperando a Booking.");
        }
        catch (HttpRequestException ex)
        {
            return ResultadoPublicacion.Reintentar($"Error de conexión con Booking: {ex.Message}");
        }
    }

    private static async Task<string> LeerDetalleAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        try
        {
            var texto = await respuesta.Content.ReadAsStringAsync(ct);
            return texto.Length <= MaximoCuerpoEnError ? texto : texto[..MaximoCuerpoEnError];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return "(sin cuerpo)";
        }
    }
}
