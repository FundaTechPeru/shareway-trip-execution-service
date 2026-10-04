using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;

namespace ShareWay.TripExecution.Domain.Viajes.Excepciones;

public sealed class ConductorNoAsignadoException(ViajeId viajeId, ConductorId solicitante) : ExcepcionDeDominio($"El conductor {solicitante} no está asignado al viaje {viajeId}.");
