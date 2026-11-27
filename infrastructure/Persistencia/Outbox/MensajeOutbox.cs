using System.Text.Json;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Salientes;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Outbox;

public sealed class MensajeOutbox
{
    public const int LongitudMaximaError = 2000;

    private MensajeOutbox() { }

    public Guid Id { get; private set; }
    public string Tipo { get; private set; } = default!;
    public string Contenido { get; private set; } = default!;
    public DateTimeOffset OcurridoEn { get; private set; }
    public DateTimeOffset? ProcesadoEn { get; private set; }
    /// <summary>Envíos fallidos (reintentables o no) acumulados.</summary>
    public int Intentos { get; private set; }
    /// <summary>Antes de este instante no se vuelve a intentar el envío.</summary>
    public DateTimeOffset? SiguienteIntentoEn { get; private set; }
    /// <summary>El destino rechazó el mensaje de forma definitiva; ya no se reintenta.</summary>
    public DateTimeOffset? FallidoEn { get; private set; }
    public string? UltimoError { get; private set; }

    public static MensajeOutbox Crear(IEventoDeIntegracion evento) => new()
    {
        Id = evento.IdEvento,
        Tipo = evento.GetType().Name,
        Contenido = JsonSerializer.Serialize(evento, evento.GetType()),
        OcurridoEn = evento.OcurridoEn
    };

    public bool ListoParaEnviar(DateTimeOffset ahora) => SiguienteIntentoEn is null || SiguienteIntentoEn <= ahora;

    public void MarcarProcesado(DateTimeOffset ahora)
    {
        ProcesadoEn = ahora;
        SiguienteIntentoEn = null;
    }

    public void ProgramarReintento(DateTimeOffset ahora, string error)
    {
        Intentos++;
        SiguienteIntentoEn = ahora + PoliticaDeReintentos.Espera(Intentos);
        UltimoError = Recortar(error);
    }

    public void MarcarFallido(DateTimeOffset ahora, string error)
    {
        Intentos++;
        FallidoEn = ahora;
        SiguienteIntentoEn = null;
        UltimoError = Recortar(error);
    }

    private static string Recortar(string error) =>
        error.Length <= LongitudMaximaError ? error : error[..LongitudMaximaError];
}

/// <summary>Espera exponencial entre reintentos: 5 s, 10 s, 20 s… hasta un máximo de 5 minutos.</summary>
public static class PoliticaDeReintentos
{
    public static readonly TimeSpan EsperaBase = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan EsperaMaxima = TimeSpan.FromMinutes(5);

    public static TimeSpan Espera(int intentos)
    {
        if (intentos <= 0) return TimeSpan.Zero;
        // A partir de 2^7 * 5 s ya se supera el máximo; se limita el exponente para no desbordar.
        var factor = Math.Pow(2, Math.Min(intentos - 1, 16));
        var espera = TimeSpan.FromTicks((long)(EsperaBase.Ticks * factor));
        return espera < EsperaMaxima ? espera : EsperaMaxima;
    }
}
