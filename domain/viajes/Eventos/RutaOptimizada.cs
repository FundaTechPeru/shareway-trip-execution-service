using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record RutaOptimizada(ViajeId ViajeId, string Motivo, DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);
