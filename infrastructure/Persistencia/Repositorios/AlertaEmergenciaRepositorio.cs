using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Repositorios;

internal sealed class AlertaEmergenciaRepositorio(TripExecutionDbContext contexto) : IAlertaEmergenciaRepositorio
{
    public async Task AgregarAsync(AlertaEmergencia alerta, CancellationToken ct) =>
        await contexto.Alertas.AddAsync(alerta, ct);

    public Task<AlertaEmergencia?> ObtenerPorIdAsync(AlertaEmergenciaId id, CancellationToken ct) =>
        contexto.Alertas.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<PaginaAlertas> ListarAsync(EstadoAlerta? estado, int pagina, int tamano, CancellationToken ct)
    {
        pagina = Math.Max(0, pagina);
        tamano = Math.Clamp(tamano <= 0 ? 20 : tamano, 1, 100);
        var consulta = contexto.Alertas.AsQueryable();
        if (estado is { } e) consulta = consulta.Where(a => a.Estado == e);
        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(a => a.CreadaEn)
            .Skip(pagina * tamano).Take(tamano)
            .ToListAsync(ct);
        return new PaginaAlertas(items, total);
    }
}
