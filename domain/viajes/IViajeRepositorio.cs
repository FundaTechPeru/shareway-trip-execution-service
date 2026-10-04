using ShareWay.TripExecution.Domain.Identificadores;

namespace ShareWay.TripExecution.Domain.Viajes;

public interface IViajeRepositorio
{
    Task<Viaje?> ObtenerPorIdAsync(ViajeId id, CancellationToken ct);
    Task<Viaje?> ObtenerPorGrupoAsync(GrupoId grupoId, CancellationToken ct);
    Task AgregarAsync(Viaje viaje, CancellationToken ct);
}
