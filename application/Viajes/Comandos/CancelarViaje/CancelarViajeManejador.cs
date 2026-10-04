using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.CancelarViaje;

public sealed class CancelarViajeManejador(IViajeRepositorio viajes, IUnidadDeTrabajo udt, TimeProvider reloj) : IManejadorDeComando<CancelarViajeComando>
{
    public async Task ManejarAsync(CancelarViajeComando comando, CancellationToken ct)
    {
        var viaje = await viajes.ObtenerOFallarAsync(comando.ViajeId, ct);
        viaje.Cancelar(comando.Motivo, reloj.GetUtcNow());
        await udt.GuardarCambiosAsync(ct);
    }
}
