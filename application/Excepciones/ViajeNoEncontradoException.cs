namespace ShareWay.TripExecution.Application.Excepciones;

public sealed class ViajeNoEncontradoException(Guid viajeId) : Exception($"No existe el viaje {viajeId}.");
