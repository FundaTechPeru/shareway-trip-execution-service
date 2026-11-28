using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes;

public sealed class Abordaje : Entidad<ReservaId>
{
    private Abordaje() { } 

    private Abordaje(ReservaId reservaId, PasajeroId pasajeroId, CodigoAbordaje codigo, CodigoAbordaje? pin)
    {
        Id = reservaId;
        PasajeroId = pasajeroId;
        Codigo = codigo;
        Pin = pin;
    }

    public PasajeroId PasajeroId { get; private set; }
    /// <summary>Hash SHA-256 del token QR.</summary>
    public CodigoAbordaje Codigo { get; private set; } = default!;
    /// <summary>Hash SHA-256 del PIN de 6 dígitos; nulo en viajes creados sin PIN.</summary>
    public CodigoAbordaje? Pin { get; private set; }
    public int IntentosFallidos { get; private set; }
    public MetodoAbordaje? Metodo { get; private set; }
    public DateTimeOffset? AbordadoEn { get; private set; }

    public bool EstaAbordado => AbordadoEn.HasValue;
    public bool EstaBloqueado => !EstaAbordado && IntentosFallidos >= PoliticaDeAbordaje.MaximoIntentosFallidos;
    internal static Abordaje Pendiente(ReservaId reservaId, PasajeroId pasajeroId, CodigoAbordaje codigo, CodigoAbordaje? pin = null) =>
        new(reservaId, pasajeroId, codigo, pin);
    internal ResultadoAbordaje Validar(string codigo, MetodoAbordaje metodo, DateTimeOffset ahora)
    {
        if (EstaAbordado) return ResultadoAbordaje.YaAbordado;
        if (EstaBloqueado) return ResultadoAbordaje.Bloqueado;
        if (!CodigoEsperado(metodo).Coincide(codigo))
        {
            IntentosFallidos++;
            return ResultadoAbordaje.CodigoInvalido;
        }
        Metodo = metodo;
        AbordadoEn = ahora;
        return ResultadoAbordaje.Abordado;
    }

    /// <summary>
    /// QR se compara contra el hash del token; PIN, contra el hash del PIN. Los viajes creados
    /// antes de que existiera el PIN no lo tienen: en ese caso el PIN se compara contra el código.
    /// </summary>
    private CodigoAbordaje CodigoEsperado(MetodoAbordaje metodo) =>
        metodo == MetodoAbordaje.Pin && Pin is not null ? Pin : Codigo;
}
