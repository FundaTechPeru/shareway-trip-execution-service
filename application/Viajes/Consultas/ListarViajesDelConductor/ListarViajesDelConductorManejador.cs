using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Consultas.ListarViajesDelConductor;

public sealed class ListarViajesDelConductorManejador(IViajeRepositorio viajes)
    : IManejadorDeConsulta<ListarViajesDelConductorConsulta, IReadOnlyList<ViajeDto>>
{
    public async Task<IReadOnlyList<ViajeDto>> ManejarAsync(ListarViajesDelConductorConsulta consulta, CancellationToken ct)
    {
        var encontrados = await viajes.ListarPorConductorAsync(new ConductorId(consulta.ConductorId), ct);
        return encontrados
            .Select(ViajeDto.Desde)
            .OrderBy(v => v.Ruta?.SalidaProgramada ?? DateTimeOffset.MaxValue)
            .ToArray();
    }
}
