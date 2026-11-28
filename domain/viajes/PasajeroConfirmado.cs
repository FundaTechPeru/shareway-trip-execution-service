using ShareWay.TripExecution.Domain.Identificadores;

namespace ShareWay.TripExecution.Domain.Viajes;

/// <summary>
/// Pasajero del manifiesto. <see cref="Codigo"/> es el hash del token QR y <see cref="Pin"/>,
/// el hash del PIN de 6 dígitos (opcional: los viajes antiguos no lo traen).
/// </summary>
public sealed record PasajeroConfirmado(ReservaId ReservaId, PasajeroId PasajeroId, CodigoAbordaje Codigo, CodigoAbordaje? Pin = null);
