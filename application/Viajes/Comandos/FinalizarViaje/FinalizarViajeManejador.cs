using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.FinalizarViaje;

public sealed class FinalizarViajeManejador(IViajeRepositorio viajes, IUnidadDeTrabajo udt, TimeProvider reloj) : IManejadorDeComando<FinalizarViajeComando>
{
    public async Task ManejarAsync(FinalizarViajeComando comando, CancellationToken ct)
    {
        var viaje = await viajes.ObtenerOFallarAsync(comando.ViajeId, ct);
        viaje.Finalizar(new ConductorId(comando.ConductorId), reloj.GetUtcNow());
        await udt.GuardarCambiosAsync(ct);
    }
}
