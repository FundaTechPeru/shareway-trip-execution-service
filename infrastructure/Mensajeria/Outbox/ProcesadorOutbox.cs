using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Infrastructure.Persistencia;

namespace ShareWay.TripExecution.Infrastructure.Mensajeria.Outbox;

internal sealed class ProcesadorOutbox(IServiceScopeFactory scopes, TimeProvider reloj, ILogger<ProcesadorOutbox> log) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(2);
    private const int TamanoLote = 20;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var temporizador = new PeriodicTimer(Intervalo);
        while (await temporizador.WaitForNextTickAsync(ct))
        {
            try
            {
                await ProcesarLoteAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Error procesando el outbox; se reintentará.");
            }
        }
    }

    private async Task ProcesarLoteAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var contexto = scope.ServiceProvider.GetRequiredService<TripExecutionDbContext>();
        var broker = scope.ServiceProvider.GetRequiredService<IBrokerDeMensajes>();

        var pendientes = await contexto.Outbox
            .Where(m => m.ProcesadoEn == null)
            .OrderBy(m => m.OcurridoEn)
            .Take(TamanoLote)
            .ToListAsync(ct);

        foreach (var mensaje in pendientes)
        {
            try
            {
                await broker.PublicarAsync(mensaje.Tipo, mensaje.Contenido, ct);
                mensaje.MarcarProcesado(reloj.GetUtcNow());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogWarning(ex, "No se pudo publicar {Tipo} ({Id}); se detiene el lote para conservar el orden.", mensaje.Tipo, mensaje.Id);
                break;
            }
        }

        await contexto.SaveChangesAsync(ct);
    }
}
