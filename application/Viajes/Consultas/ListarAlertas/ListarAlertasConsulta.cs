using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Consultas.ListarAlertas;

public sealed record ListarAlertasConsulta(EstadoAlerta? Estado, int Pagina, int Tamano);

public sealed record AlertaEmergenciaDto(
    Guid Id,
    Guid ViajeId,
    Guid ReportanteId,
    string RolReportante,
    string Tipo,
    string Descripcion,
    string Estado,
    DateTimeOffset CreadaEn,
    DateTimeOffset? AtendidaEn,
    Guid? AtendidaPor)
{
    public static AlertaEmergenciaDto Desde(AlertaEmergencia a) => new(
        a.Id.Valor,
        a.ViajeId.Valor,
        a.ReportanteId,
        a.RolReportante,
        a.Tipo,
        a.Descripcion,
        a.Estado.ToString(),
        a.CreadaEn,
        a.AtendidaEn,
        a.AtendidaPor);
}

public sealed record PaginaAlertasDto(IReadOnlyList<AlertaEmergenciaDto> Items, int Pagina, int Tamano, int Total, int TotalPaginas);
