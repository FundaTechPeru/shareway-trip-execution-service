using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes;

public sealed class Abordaje : Entidad<ReservaId>
{
    private Abordaje() { } 

    private Abordaje(ReservaId reservaId, PasajeroId pasajeroId, CodigoAbordaje codigo)
    {
        Id = reservaId;
        PasajeroId = pasajeroId;
        Codigo = codigo;
    }

    public PasajeroId PasajeroId { get; private set; }
    public CodigoAbordaje Codigo { get; private set; } = default!;
    public int IntentosFallidos { get; private set; }
    public MetodoAbordaje? Metodo { get; private set; }
    public DateTimeOffset? AbordadoEn { get; private set; }

    public bool EstaAbordado => AbordadoEn.HasValue;
    public bool EstaBloqueado => !EstaAbordado && IntentosFallidos >= PoliticaDeAbordaje.MaximoIntentosFallidos;
    internal static Abordaje Pendiente(ReservaId reservaId, PasajeroId pasajeroId, CodigoAbordaje codigo) => new(reservaId, pasajeroId, codigo);
    internal ResultadoAbordaje Validar(string codigo, MetodoAbordaje metodo, DateTimeOffset ahora)
    {
        if (EstaAbordado) return ResultadoAbordaje.YaAbordado;
        if (EstaBloqueado) return ResultadoAbordaje.Bloqueado;
        if (!Codigo.Coincide(codigo))
        {
            IntentosFallidos++;
            return ResultadoAbordaje.CodigoInvalido;
        }
        Metodo = metodo;
        AbordadoEn = ahora;
        return ResultadoAbordaje.Abordado;
    }
}
