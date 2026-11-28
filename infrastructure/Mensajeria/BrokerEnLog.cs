namespace ShareWay.TripExecution.Infrastructure.Mensajeria;

/// <summary>Broker de pruebas: solo escribe el evento en el log. No se registra en Program.</summary>
internal sealed class BrokerEnLog(ILogger<BrokerEnLog> log) : IBrokerDeMensajes
{
    public Task<ResultadoPublicacion> PublicarAsync(string tipo, string contenidoJson, CancellationToken ct)
    {
        log.LogInformation("[BROKER] {Tipo}: {Contenido}", tipo, contenidoJson);
        return Task.FromResult(ResultadoPublicacion.Publicado);
    }
}
