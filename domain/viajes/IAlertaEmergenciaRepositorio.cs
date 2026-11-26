using ShareWay.TripExecution.Domain.Identificadores;

namespace ShareWay.TripExecution.Domain.Viajes;

/// <summary>Paginación simple reutilizada por la consulta admin de alertas.</summary>
public sealed record PaginaAlertas(IReadOnlyList<AlertaEmergencia> Items, int Total);

public interface IAlertaEmergenciaRepositorio
{
    Task AgregarAsync(AlertaEmergencia alerta, CancellationToken ct);
    Task<AlertaEmergencia?> ObtenerPorIdAsync(AlertaEmergenciaId id, CancellationToken ct);
    Task<PaginaAlertas> ListarAsync(EstadoAlerta? estado, int pagina, int tamano, CancellationToken ct);
}
