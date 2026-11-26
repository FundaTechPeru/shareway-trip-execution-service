using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Excepciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.AtenderAlerta;

public sealed class AtenderAlertaManejador(
    IAlertaEmergenciaRepositorio alertas,
    IUnidadDeTrabajo udt,
    TimeProvider reloj) : IManejadorDeComando<AtenderAlertaComando>
{
    public async Task ManejarAsync(AtenderAlertaComando comando, CancellationToken ct)
    {
        var alerta = await alertas.ObtenerPorIdAsync(new AlertaEmergenciaId(comando.AlertaId), ct)
                     ?? throw new AlertaNoEncontradaException(comando.AlertaId);
        alerta.Atender(comando.AdministradorId, reloj.GetUtcNow());
        await udt.GuardarCambiosAsync(ct);
    }
}

public sealed class AlertaNoEncontradaException(Guid alertaId) : Exception($"No existe la alerta {alertaId}.");
