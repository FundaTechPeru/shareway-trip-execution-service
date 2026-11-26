using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Excepciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Domain.Viajes.Excepciones;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.RegistrarAlerta;

public sealed class RegistrarAlertaManejador(
    IViajeRepositorio viajes,
    IAlertaEmergenciaRepositorio alertas,
    IUnidadDeTrabajo udt,
    TimeProvider reloj) : IManejadorDeComando<RegistrarAlertaComando, Guid>
{
    public async Task<Guid> ManejarAsync(RegistrarAlertaComando comando, CancellationToken ct)
    {
        var viaje = await viajes.ObtenerPorIdAsync(new ViajeId(comando.ViajeId), ct)
                    ?? throw new ViajeNoEncontradoException(comando.ViajeId);

        var rol = comando.RolReportante.Trim().ToUpperInvariant();
        if (rol is not ("DRIVER" or "PASSENGER"))
            throw new ReglaDeNegocioException("El rol del reportante debe ser DRIVER o PASSENGER.");

        if (rol == "DRIVER" && viaje.ConductorId.Valor != comando.ReportanteId)
            throw new ConductorNoAsignadoException(viaje.Id, new ConductorId(comando.ReportanteId));

        if (rol == "PASSENGER" &&
            !viaje.Abordajes.Any(a => a.PasajeroId.Valor == comando.ReportanteId))
            throw new ReglaDeNegocioException("El pasajero no pertenece a este viaje.");

        var alerta = AlertaEmergencia.Registrar(
            viaje.Id, comando.ReportanteId, rol, comando.Tipo, comando.Descripcion, reloj.GetUtcNow());

        await alertas.AgregarAsync(alerta, ct);
        await udt.GuardarCambiosAsync(ct);
        return alerta.Id.Valor;
    }
}
