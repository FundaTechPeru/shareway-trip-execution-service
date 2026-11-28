using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

/// <summary><see cref="DistanciaKm"/> es nula cuando el viaje no tiene ruta registrada.</summary>
public sealed record ViajeFinalizado(
    ViajeId ViajeId,
    GrupoId GrupoId,
    ConductorId ConductorId,
    DateTimeOffset InicioReal,
    DateTimeOffset FinReal,
    IReadOnlyCollection<ReservaId> ReservasAbordadas,
    double? DistanciaKm = null) : EventoDeDominio(FinReal);
