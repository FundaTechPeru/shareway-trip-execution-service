using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Consultas.ListarViajesAdmin;

public sealed class ListarViajesAdminManejador(IViajeRepositorio viajes)
    : IManejadorDeConsulta<ListarViajesAdminConsulta, IReadOnlyList<ViajeDto>>
{
    public async Task<IReadOnlyList<ViajeDto>> ManejarAsync(ListarViajesAdminConsulta consulta, CancellationToken ct)
    {
        var encontrados = await viajes.ListarAdminAsync(
            consulta.ConductorId is { } conductorId ? new ConductorId(conductorId) : null,
            consulta.PasajeroId is { } pasajeroId ? new PasajeroId(pasajeroId) : null,
            ct);

        return encontrados
            .Select(ViajeDto.Desde)
            .OrderBy(v => v.Ruta?.SalidaProgramada ?? DateTimeOffset.MaxValue)
            .ToArray();
    }
}
