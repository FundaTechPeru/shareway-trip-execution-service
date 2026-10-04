using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Excepciones;

public sealed class PasajeroNoPerteneceAlViajeException(ViajeId viajeId, ReservaId reservaId) : ExcepcionDeDominio($"La reserva {reservaId} no pertenece al viaje {viajeId}.");
