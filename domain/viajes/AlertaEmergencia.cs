using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes.Eventos;
using ShareWay.TripExecution.Domain.Viajes.Excepciones;

namespace ShareWay.TripExecution.Domain.Viajes;

/// <summary>
/// Alerta de emergencia reportada durante un viaje. Puede abrirla el conductor del viaje o un
/// pasajero con abordaje confirmado en él; sólo un administrador puede marcarla como atendida.
/// La alerta no cambia el estado del viaje: coexiste con él para que operaciones pueda hacer
/// seguimiento sin interrumpir la operación en curso.
/// </summary>
public sealed class AlertaEmergencia : AggregateRoot<AlertaEmergenciaId>
{
    private AlertaEmergencia() { }

    private AlertaEmergencia(
        AlertaEmergenciaId id,
        ViajeId viajeId,
        Guid reportanteId,
        string rolReportante,
        string tipo,
        string descripcion,
        DateTimeOffset creadaEn)
    {
        Id = id;
        ViajeId = viajeId;
        ReportanteId = reportanteId;
        RolReportante = rolReportante;
        Tipo = tipo;
        Descripcion = descripcion;
        Estado = EstadoAlerta.Abierta;
        CreadaEn = creadaEn;
    }

    public ViajeId ViajeId { get; private set; }
    public Guid ReportanteId { get; private set; } = default!;
    public string RolReportante { get; private set; } = default!;
    public string Tipo { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public EstadoAlerta Estado { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset? AtendidaEn { get; private set; }
    public Guid? AtendidaPor { get; private set; }

    public int Version { get; private set; }

    public static AlertaEmergencia Registrar(
        ViajeId viajeId,
        Guid reportanteId,
        string rolReportante,
        string tipo,
        string descripcion,
        DateTimeOffset ahora)
    {
        if (reportanteId == Guid.Empty)
            throw new ReglaDeNegocioException("El reportante de la alerta es obligatorio.");
        if (string.IsNullOrWhiteSpace(rolReportante))
            throw new ReglaDeNegocioException("El rol del reportante es obligatorio.");
        if (string.IsNullOrWhiteSpace(tipo))
            throw new ReglaDeNegocioException("El tipo de alerta es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ReglaDeNegocioException("La descripción de la alerta es obligatoria.");

        var alerta = new AlertaEmergencia(
            AlertaEmergenciaId.Nuevo(),
            viajeId,
            reportanteId,
            rolReportante.Trim(),
            tipo.Trim(),
            descripcion.Trim(),
            ahora);

        alerta.LevantarEvento(new AlertaEmergenciaRegistrada(
            alerta.Id, viajeId, reportanteId, alerta.RolReportante, alerta.Tipo, ahora));
        return alerta;
    }

    public void Atender(Guid administradorId, DateTimeOffset ahora)
    {
        if (administradorId == Guid.Empty)
            throw new ReglaDeNegocioException("El administrador que atiende la alerta es obligatorio.");
        if (Estado == EstadoAlerta.Atendida)
            throw new ReglaDeNegocioException("La alerta ya fue atendida.");

        Estado = EstadoAlerta.Atendida;
        AtendidaEn = ahora;
        AtendidaPor = administradorId;
        Version++;
        LevantarEvento(new AlertaEmergenciaAtendida(Id, ViajeId, administradorId, ahora));
    }
}

public enum EstadoAlerta
{
    Abierta,
    Atendida
}
