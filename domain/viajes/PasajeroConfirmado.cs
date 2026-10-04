using ShareWay.TripExecution.Domain.Identificadores;

namespace ShareWay.TripExecution.Domain.Viajes;

public sealed record PasajeroConfirmado(ReservaId ReservaId, PasajeroId PasajeroId, CodigoAbordaje Codigo);
