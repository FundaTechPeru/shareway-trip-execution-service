using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.OptimizarRuta;

public sealed class OptimizarRutaManejador(IViajeRepositorio viajes, IUnidadDeTrabajo udt, TimeProvider reloj)
    : IManejadorDeComando<OptimizarRutaComando>
{
    public async Task ManejarAsync(OptimizarRutaComando comando, CancellationToken ct)
    {
        var viaje = await viajes.ObtenerOFallarAsync(comando.ViajeId, ct);
        viaje.OptimizarRuta(reloj.GetUtcNow());
        await udt.GuardarCambiosAsync(ct);
    }
}
