using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Excepciones;

public sealed class TransicionDeEstadoInvalidaException(ViajeId viajeId, EstadoViaje actual, string operacion) : ExcepcionDeDominio($"No se puede ejecutar '{operacion}' sobre el viaje {viajeId} en estado {actual}.");
