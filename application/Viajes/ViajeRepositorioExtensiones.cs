using ShareWay.TripExecution.Application.Excepciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes;

internal static class ViajeRepositorioExtensiones
{
    public static async Task<Viaje> ObtenerOFallarAsync(this IViajeRepositorio repo, Guid viajeId, CancellationToken ct) =>
        await repo.ObtenerPorIdAsync(new ViajeId(viajeId), ct) ?? throw new ViajeNoEncontradoException(viajeId);
}
