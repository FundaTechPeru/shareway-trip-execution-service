using System.ComponentModel.DataAnnotations;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Api.Contratos.Solicitudes;

public sealed record CrearViajeSolicitud(
    Guid GrupoId,
    Guid ConductorId,
    IReadOnlyList<PasajeroConfirmadoDto> Pasajeros,
    RutaViajeSolicitud? Ruta = null,
    Guid? VehiculoId = null);

public sealed record RutaViajeSolicitud(
    [param: Required] DateTimeOffset? SalidaProgramada,
    [param: Required, StringLength(120)] string Origen,
    [param: Required, StringLength(120)] string Destino,
    [param: Required, MinLength(2)] IReadOnlyList<PuntoRutaSolicitud> Puntos,
    bool EsPrueba = false);

public sealed record PuntoRutaSolicitud(
    [param: Required, StringLength(120)] string Etiqueta,
    [param: Range(-90, 90)] double Latitud,
    [param: Range(-180, 180)] double Longitud,
    [param: Required, RegularExpression("^(ORIGEN|PARADA|DESTINO)$")] string Tipo,
    Guid? ReservaId = null);

public sealed record IniciarViajeSolicitud;
public sealed record ValidarAbordajeSolicitud(Guid ReservaId, [param: Required][param: StringLength(100)] string Codigo, MetodoAbordaje Metodo);
public sealed record FinalizarViajeSolicitud;
public sealed record CancelarViajeSolicitud([param: Required][param: StringLength(550)] string Motivo);
public sealed record CancelarParadaSolicitud(
    Guid ReservaId,
    [param: Required][param: StringLength(550)] string Motivo,
    [param: StringLength(120)] string? Etiqueta = null);

public sealed record RegistrarAlertaSolicitud(
    [param: Required][param: StringLength(60)] string Tipo,
    [param: Required][param: StringLength(1000)] string Descripcion);
