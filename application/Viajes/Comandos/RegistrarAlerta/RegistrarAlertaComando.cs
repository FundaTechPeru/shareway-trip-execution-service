namespace ShareWay.TripExecution.Application.Viajes.Comandos.RegistrarAlerta;

public sealed record RegistrarAlertaComando(
    Guid ViajeId,
    Guid ReportanteId,
    string RolReportante,
    string Tipo,
    string Descripcion);
