using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes.Eventos;

namespace ShareWay.TripExecution.Application.EventosDeIntegracion.Salientes;

public static class MapeadorDeEventos
{
    public static IEventoDeIntegracion? ADeIntegracion(IEventoDeDominio evento) => evento switch
    {
        ViajeCreado e => new ViajeCreadoIntegracion(e.IdEvento, e.OcurridoEn, e.ViajeId.Valor, e.GrupoId.Valor, e.ConductorId.Valor),
        ViajeIniciado e => new ViajeIniciadoIntegracion(e.IdEvento, e.OcurridoEn, e.ViajeId.Valor, e.ConductorId.Valor),
        PasajeroAbordo e => new PasajeroAbordoIntegracion(e.IdEvento, e.OcurridoEn, e.ViajeId.Valor, e.ReservaId.Valor, e.PasajeroId.Valor),
        AbordajeFallido e => new AbordajeFallidoIntegracion(e.IdEvento, e.OcurridoEn, e.ViajeId.Valor, e.ReservaId.Valor, e.PasajeroId.Valor, e.IntentosFallidos, e.Bloqueado),
        ViajeFinalizado e => new ViajeFinalizadoIntegracion(e.IdEvento, e.OcurridoEn, e.ViajeId.Valor, e.GrupoId.Valor, e.ConductorId.Valor,e.InicioReal, e.FinReal, e.ReservasAbordadas.Select(r => r.Valor).ToArray()),
        ViajeCancelado e => new ViajeCanceladoIntegracion(e.IdEvento, e.OcurridoEn, e.ViajeId.Valor, e.GrupoId.Valor, e.Motivo),
        _ => null
    };
}
