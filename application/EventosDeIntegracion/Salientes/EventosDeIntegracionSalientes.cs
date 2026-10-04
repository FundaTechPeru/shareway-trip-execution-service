namespace ShareWay.TripExecution.Application.EventosDeIntegracion.Salientes;

public interface IEventoDeIntegracion
{
    Guid IdEvento { get; }
    DateTimeOffset OcurridoEn { get; }
}

public sealed record ViajeCreadoIntegracion(Guid IdEvento, DateTimeOffset OcurridoEn, Guid ViajeId, Guid GrupoId, Guid ConductorId) : IEventoDeIntegracion;
public sealed record ViajeIniciadoIntegracion(Guid IdEvento, DateTimeOffset OcurridoEn, Guid ViajeId, Guid ConductorId) : IEventoDeIntegracion;
public sealed record PasajeroAbordoIntegracion(Guid IdEvento, DateTimeOffset OcurridoEn, Guid ViajeId, Guid ReservaId, Guid PasajeroId) : IEventoDeIntegracion;
public sealed record AbordajeFallidoIntegracion(Guid IdEvento, DateTimeOffset OcurridoEn, Guid ViajeId, Guid ReservaId, Guid PasajeroId, int IntentosFallidos, bool Bloqueado) : IEventoDeIntegracion;
public sealed record ViajeFinalizadoIntegracion(Guid IdEvento, DateTimeOffset OcurridoEn, Guid ViajeId, Guid GrupoId, Guid ConductorId, DateTimeOffset InicioReal, DateTimeOffset FinReal, IReadOnlyList<Guid> ReservasAbordadas) : IEventoDeIntegracion;
public sealed record ViajeCanceladoIntegracion(Guid IdEvento, DateTimeOffset OcurridoEn, Guid ViajeId, Guid GrupoId, string Motivo) : IEventoDeIntegracion;
