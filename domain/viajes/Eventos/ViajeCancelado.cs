using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Eventos;

public sealed record ViajeCancelado(ViajeId ViajeId, GrupoId GrupoId, string Motivo, DateTimeOffset OcurridoEn) : EventoDeDominio(OcurridoEn);
