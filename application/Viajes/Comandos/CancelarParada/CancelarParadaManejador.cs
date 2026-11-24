using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.CancelarParada;

public sealed class CancelarParadaManejador(IViajeRepositorio viajes, IUnidadDeTrabajo udt, TimeProvider reloj)
    : IManejadorDeComando<CancelarParadaComando>
{
    public async Task ManejarAsync(CancelarParadaComando comando, CancellationToken ct)
    {
        var viaje = await viajes.ObtenerOFallarAsync(comando.ViajeId, ct);
        viaje.CancelarParada(new ReservaId(comando.ReservaId), comando.Motivo, reloj.GetUtcNow(), comando.Etiqueta);
        await udt.GuardarCambiosAsync(ct);
    }
}
