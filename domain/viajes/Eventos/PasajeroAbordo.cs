using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record PasajeroAbordo(ViajeId ViajeId, ReservaId ReservaId, PasajeroId PasajeroId, MetodoAbordaje Metodo, DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);
