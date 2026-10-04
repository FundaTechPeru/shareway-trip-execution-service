namespace ShareWay.TripExecution.Infrastructure.Mensajeria;

internal sealed class BrokerEnLog(ILogger<BrokerEnLog> log) : IBrokerDeMensajes
{
    public Task PublicarAsync(string tipo, string contenidoJson, CancellationToken ct)
    {
        log.LogInformation("[BROKER] {Tipo}: {Contenido}", tipo, contenidoJson);
        return Task.CompletedTask;
    }
}
