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
                await using var scope = scopes.CreateAsyncScope();
                var contexto = scope.ServiceProvider.GetRequiredService<TripExecutionDbContext>();
                var broker = scope.ServiceProvider.GetRequiredService<IBrokerDeMensajes>();
                await ProcesarLoteAsync(contexto, broker, reloj, log, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Error procesando el outbox; se reintentará.");
            }
        }
    }

    /// <summary>
    /// Envía los mensajes pendientes en orden. Un fallo transitorio programa el reintento y detiene
    /// el lote (conserva el orden); un rechazo definitivo marca el mensaje como fallido y sigue.
    /// </summary>
    internal static async Task ProcesarLoteAsync(
        TripExecutionDbContext contexto, IBrokerDeMensajes broker, TimeProvider reloj, ILogger log, CancellationToken ct)
    {
        var pendientes = await contexto.Outbox
            .Where(m => m.ProcesadoEn == null && m.FallidoEn == null)
            .OrderBy(m => m.OcurridoEn)
            .Take(TamanoLote)
            .ToListAsync(ct);

        foreach (var mensaje in pendientes)
        {
            // El primero en espera bloquea a los posteriores: así se conserva el orden.
            if (!mensaje.ListoParaEnviar(reloj.GetUtcNow()))
                break;

            ResultadoPublicacion resultado;
            try
            {
                resultado = await broker.PublicarAsync(mensaje.Tipo, mensaje.Contenido, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                resultado = ResultadoPublicacion.Reintentar(ex.Message);
            }

            var ahora = reloj.GetUtcNow();
            if (resultado.Estado is EstadoPublicacion.Publicado or EstadoPublicacion.Omitido)
            {
                mensaje.MarcarProcesado(ahora);
                continue;
            }

            if (resultado.Estado == EstadoPublicacion.Fallido)
            {
                mensaje.MarcarFallido(ahora, resultado.Error ?? "Rechazado por el destino.");
                log.LogError("Evento {Tipo} ({Id}) rechazado de forma definitiva: {Error}", mensaje.Tipo, mensaje.Id, mensaje.UltimoError);
                continue;
            }

            mensaje.ProgramarReintento(ahora, resultado.Error ?? "Fallo transitorio.");
            log.LogWarning(
                "No se pudo publicar {Tipo} ({Id}), intento {Intentos}; siguiente intento {Siguiente:O}. Se detiene el lote para conservar el orden. {Error}",
                mensaje.Tipo, mensaje.Id, mensaje.Intentos, mensaje.SiguienteIntentoEn, mensaje.UltimoError);
            break;
        }

        await contexto.SaveChangesAsync(ct);
    }
}
