using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record ViajeIniciado(ViajeId ViajeId, ConductorId ConductorId, DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);
