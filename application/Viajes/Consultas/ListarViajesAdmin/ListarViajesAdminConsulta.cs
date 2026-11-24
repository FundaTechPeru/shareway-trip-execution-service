namespace ShareWay.TripExecution.Application.Viajes.Consultas.ListarViajesAdmin;

/// <summary>Filtros opcionales; sin ninguno se listan todos los viajes.</summary>
public sealed record ListarViajesAdminConsulta(Guid? ConductorId, Guid? PasajeroId);
