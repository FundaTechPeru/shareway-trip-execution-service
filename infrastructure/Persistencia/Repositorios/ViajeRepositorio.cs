using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Repositorios;

internal sealed class ViajeRepositorio(TripExecutionDbContext contexto) : IViajeRepositorio
{
    public Task<Viaje?> ObtenerPorIdAsync(ViajeId id, CancellationToken ct) => contexto.Viajes.Include(v => v.Abordajes).FirstOrDefaultAsync(v => v.Id == id, ct);
    public Task<Viaje?> ObtenerPorGrupoAsync(GrupoId grupoId, CancellationToken ct) => contexto.Viajes.Include(v => v.Abordajes).FirstOrDefaultAsync(v => v.GrupoId == grupoId, ct);
    public async Task AgregarAsync(Viaje viaje, CancellationToken ct) => await contexto.Viajes.AddAsync(viaje, ct);
}
