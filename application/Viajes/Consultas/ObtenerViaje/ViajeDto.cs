using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;

public sealed record AbordajeDto(Guid ReservaId, Guid PasajeroId, bool Abordo, DateTimeOffset? AbordadoEn, int IntentosFallidos, bool Bloqueado);
public sealed record ViajeDto(Guid Id, Guid GrupoId, Guid ConductorId, string Estado, DateTimeOffset? InicioReal, DateTimeOffset? FinReal, IReadOnlyList<AbordajeDto> Abordajes)
{
    public static ViajeDto Desde(Viaje v) => new(
        v.Id.Valor,
        v.GrupoId.Valor,
        v.ConductorId.Valor,
        v.Estado.ToString(),
        v.InicioReal,
        v.FinReal,
        v.Abordajes
        .Select(a => new AbordajeDto(a.Id.Valor, a.PasajeroId.Valor, a.EstaAbordado, a.AbordadoEn, a.IntentosFallidos, a.EstaBloqueado))
        .ToList());
}
