namespace ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;

/// <summary>
/// <see cref="CodigoHash"/> = SHA-256 hex (minúsculas) del token QR.
/// <see cref="PinHash"/> = SHA-256 hex (minúsculas) del PIN de 6 dígitos; opcional.
/// </summary>
public sealed record PasajeroConfirmadoDto(Guid ReservaId, Guid PasajeroId, string CodigoHash, string? PinHash = null);
/// <summary><see cref="Creado"/> es falso cuando el grupo ya tenía un viaje (creación idempotente).</summary>
public sealed record CrearViajeResultado(Guid Id, bool Creado);
public sealed record CrearViajeComando(
    Guid GrupoId,
    Guid ConductorId,
    IReadOnlyList<PasajeroConfirmadoDto> Pasajeros,
    string? RutaJson = null,
    Guid? VehiculoId = null);
