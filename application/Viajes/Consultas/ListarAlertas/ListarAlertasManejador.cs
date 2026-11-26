using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Consultas.ListarAlertas;

public sealed class ListarAlertasManejador(IAlertaEmergenciaRepositorio alertas)
    : IManejadorDeConsulta<ListarAlertasConsulta, PaginaAlertasDto>
{
    public async Task<PaginaAlertasDto> ManejarAsync(ListarAlertasConsulta consulta, CancellationToken ct)
    {
        var pagina = await alertas.ListarAsync(consulta.Estado, consulta.Pagina, consulta.Tamano, ct);
        var tamano = consulta.Tamano <= 0 ? 20 : Math.Clamp(consulta.Tamano, 1, 100);
        var totalPaginas = tamano == 0 ? 0 : (int)Math.Ceiling(pagina.Total / (double)tamano);
        return new PaginaAlertasDto(
            pagina.Items.Select(AlertaEmergenciaDto.Desde).ToList(),
            Math.Max(0, consulta.Pagina),
            tamano,
            pagina.Total,
            totalPaginas);
    }
}
