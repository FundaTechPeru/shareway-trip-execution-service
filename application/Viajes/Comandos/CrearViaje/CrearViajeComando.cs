namespace ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;

public sealed record PasajeroConfirmadoDto(Guid ReservaId, Guid PasajeroId, string CodigoHash);
public sealed record CrearViajeComando(Guid GrupoId, Guid ConductorId, IReadOnlyList<PasajeroConfirmadoDto> Pasajeros);
