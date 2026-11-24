using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Repositorios;

internal sealed class ViajeRepositorio(TripExecutionDbContext contexto) : IViajeRepositorio
{
    public Task<Viaje?> ObtenerPorIdAsync(ViajeId id, CancellationToken ct) => contexto.Viajes.Include(v => v.Abordajes).FirstOrDefaultAsync(v => v.Id == id, ct);
    public Task<Viaje?> ObtenerPorGrupoAsync(GrupoId grupoId, CancellationToken ct) => contexto.Viajes.Include(v => v.Abordajes).FirstOrDefaultAsync(v => v.GrupoId == grupoId, ct);
    public async Task<IReadOnlyList<Viaje>> ListarPorConductorAsync(ConductorId conductorId, CancellationToken ct) =>
        await contexto.Viajes.Include(v => v.Abordajes).Where(v => v.ConductorId == conductorId).ToListAsync(ct);

    public async Task<IReadOnlyList<Viaje>> ListarAdminAsync(ConductorId? conductorId, PasajeroId? pasajeroId, CancellationToken ct)
    {
        var consulta = contexto.Viajes.Include(v => v.Abordajes).AsQueryable();
        if (conductorId is not null) consulta = consulta.Where(v => v.ConductorId == conductorId);
        if (pasajeroId is not null) consulta = consulta.Where(v => v.Abordajes.Any(a => a.PasajeroId == pasajeroId));
        return await consulta.ToListAsync(ct);
    }

    public async Task AgregarAsync(Viaje viaje, CancellationToken ct) => await contexto.Viajes.AddAsync(viaje, ct);
}
