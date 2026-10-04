using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record ViajeFinalizado( ViajeId ViajeId, GrupoId GrupoId, ConductorId ConductorId, DateTimeOffset InicioReal, DateTimeOffset FinReal, IReadOnlyCollection<ReservaId> ReservasAbordadas) : EventoDeDominio(FinReal);
