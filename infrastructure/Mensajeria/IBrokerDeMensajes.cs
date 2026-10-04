namespace ShareWay.TripExecution.Infrastructure.Mensajeria;

public interface IBrokerDeMensajes
{
    Task PublicarAsync(string tipo, string contenidoJson, CancellationToken ct);
}
