using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;

namespace ShareWay.TripExecution.Application.EventosDeIntegracion.Entrantes;

public sealed record GrupoConfirmadoEvento(Guid IdEvento, Guid GrupoId, Guid ConductorId, IReadOnlyList<PasajeroConfirmadoDto> Pasajeros);
