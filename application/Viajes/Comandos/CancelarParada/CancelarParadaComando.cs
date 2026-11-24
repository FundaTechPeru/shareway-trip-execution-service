namespace ShareWay.TripExecution.Application.Viajes.Comandos.CancelarParada;

public sealed record CancelarParadaComando(Guid ViajeId, Guid ReservaId, string Motivo, string? Etiqueta = null);
