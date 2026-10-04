using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record ViajeCreado(ViajeId ViajeId, GrupoId GrupoId, ConductorId ConductorId, DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);
