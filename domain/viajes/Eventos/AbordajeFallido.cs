using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record AbordajeFallido(ViajeId ViajeId, ReservaId ReservaId, PasajeroId PasajeroId, int IntentosFallidos, bool Bloqueado, DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);
