using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;
public sealed class ObtenerViajeManejador(IViajeRepositorio viajes)
    : IManejadorDeConsulta<ObtenerViajeConsulta, ViajeDto>
{
    public async Task<ViajeDto> ManejarAsync(ObtenerViajeConsulta consulta, CancellationToken ct) =>
        ViajeDto.Desde(await viajes.ObtenerOFallarAsync(consulta.ViajeId, ct));
}
