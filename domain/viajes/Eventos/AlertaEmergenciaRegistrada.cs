using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record AlertaEmergenciaRegistrada(
    AlertaEmergenciaId AlertaId,
    ViajeId ViajeId,
    Guid ReportanteId,
    string RolReportante,
    string Tipo,
    DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);

public sealed record AlertaEmergenciaAtendida(
    AlertaEmergenciaId AlertaId,
    ViajeId ViajeId,
    Guid AdministradorId,
    DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);
