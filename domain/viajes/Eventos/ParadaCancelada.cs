using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record ParadaCancelada(ViajeId ViajeId, ReservaId ReservaId, string Motivo, DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);
