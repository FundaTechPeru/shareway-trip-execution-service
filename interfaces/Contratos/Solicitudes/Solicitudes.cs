using System.ComponentModel.DataAnnotations;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Api.Contratos.Solicitudes;

public sealed record CrearViajeSolicitud(Guid GrupoId, Guid ConductorId, IReadOnlyList<PasajeroConfirmadoDto> Pasajeros);
public sealed record IniciarViajeSolicitud(Guid ConductorId);
public sealed record ValidarAbordajeSolicitud(Guid ConductorId, Guid ReservaId, [param: Required][param: StringLength(100)] string Codigo, MetodoAbordaje Metodo);
public sealed record FinalizarViajeSolicitud(Guid ConductorId);
public sealed record CancelarViajeSolicitud([param: Required][param: StringLength(550)] string Motivo);