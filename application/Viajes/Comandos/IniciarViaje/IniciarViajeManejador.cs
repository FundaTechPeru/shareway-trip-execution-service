using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.IniciarViaje;

public sealed class IniciarViajeManejador(IViajeRepositorio viajes, IUnidadDeTrabajo udt, TimeProvider reloj) : IManejadorDeComando<IniciarViajeComando>
{
    public async Task ManejarAsync(IniciarViajeComando comando, CancellationToken ct)
    {
        var viaje = await viajes.ObtenerOFallarAsync(comando.ViajeId, ct);
        viaje.Iniciar(new ConductorId(comando.ConductorId), reloj.GetUtcNow());
        await udt.GuardarCambiosAsync(ct);
    }
}
