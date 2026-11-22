using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareWay.TripExecution.Api.Contratos.Solicitudes;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Viajes.Comandos.CancelarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.FinalizarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.IniciarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.ValidarAbordaje;
using ShareWay.TripExecution.Application.Viajes.Consultas.ListarViajesDelConductor;
using ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;

namespace ShareWay.TripExecution.Api.Controllers;

[ApiController]
[Route("api/viajes")]
public sealed class ViajesController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Crear([FromBody] CrearViajeSolicitud solicitud, [FromServices] IManejadorDeComando<CrearViajeComando, Guid> manejador, CancellationToken cancellationToken)
    {
        if (solicitud.Ruta is { } ruta && !RutaValida(ruta, out var error))
            return BadRequest(new { message = error });

        var rutaDto = solicitud.Ruta is null ? null : new RutaViajeDto(
            solicitud.Ruta.SalidaProgramada!.Value,
            solicitud.Ruta.Origen.Trim(),
            solicitud.Ruta.Destino.Trim(),
            solicitud.Ruta.Puntos.Select(p => new PuntoRutaDto(p.Etiqueta.Trim(), p.Latitud, p.Longitud, p.Tipo)).ToArray(),
            solicitud.Ruta.EsPrueba);
        var rutaJson = rutaDto is null ? null : JsonSerializer.Serialize(rutaDto, JsonSerializerOptions.Web);
        var id = await manejador.ManejarAsync(new CrearViajeComando(solicitud.GrupoId, solicitud.ConductorId, solicitud.Pasajeros, rutaJson), cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id }, new { id });
    }

    [HttpGet("mine")]
    [Authorize(Policy = "DriverOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<ViajeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ViajeDto>>> ObtenerMisViajes(
        [FromServices] IManejadorDeConsulta<ListarViajesDelConductorConsulta, IReadOnlyList<ViajeDto>> manejador,
        CancellationToken cancellationToken)
    {
        var viajes = await manejador.ManejarAsync(new ListarViajesDelConductorConsulta(ConductorAutenticado()), cancellationToken);
        return Ok(viajes);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "DriverOnly")]
    [ProducesResponseType(typeof(ViajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ViajeDto>> ObtenerPorId(Guid id, [FromServices] IManejadorDeConsulta<ObtenerViajeConsulta, ViajeDto> manejador, CancellationToken cancellationToken)
    {
        var viaje = await manejador.ManejarAsync(new ObtenerViajeConsulta(id), cancellationToken);
        if (viaje.ConductorId != ConductorAutenticado()) return Forbid();
        return Ok(viaje);
    }

    [HttpPost("{id:guid}/abordajes")]
    [Authorize(Policy = "DriverOnly")]
    [ProducesResponseType(typeof(ValidarAbordajeRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ValidarAbordajeRespuesta>> ValidarAbordaje(Guid id, [FromBody] ValidarAbordajeSolicitud solicitud, [FromServices] IManejadorDeComando<ValidarAbordajeComando, ValidarAbordajeRespuesta> manejador, CancellationToken cancellationToken)
    {
        var respuesta = await manejador.ManejarAsync(new ValidarAbordajeComando(id, ConductorAutenticado(), solicitud.ReservaId, solicitud.Codigo, solicitud.Metodo), cancellationToken);
        return Ok(respuesta);
    }

    [HttpPost("{id:guid}/iniciar")]
    [Authorize(Policy = "DriverOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Iniciar(Guid id, [FromServices] IManejadorDeComando<IniciarViajeComando> manejador, CancellationToken cancellationToken)
    {
        await manejador.ManejarAsync(new IniciarViajeComando(id, ConductorAutenticado()), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/finalizar")]
    [Authorize(Policy = "DriverOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Finalizar(Guid id, [FromServices] IManejadorDeComando<FinalizarViajeComando> manejador, CancellationToken cancellationToken)
    {
        await manejador.ManejarAsync(new FinalizarViajeComando(id, ConductorAutenticado()), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/cancelar")]
    [Authorize(Policy = "DriverOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarViajeSolicitud solicitud,
        [FromServices] IManejadorDeComando<CancelarViajeComando> manejador,
        [FromServices] IManejadorDeConsulta<ObtenerViajeConsulta, ViajeDto> consulta,
        CancellationToken cancellationToken)
    {
        if (User.IsInRole("DRIVER"))
        {
            var viaje = await consulta.ManejarAsync(new ObtenerViajeConsulta(id), cancellationToken);
            if (viaje.ConductorId != ConductorAutenticado()) return Forbid();
        }

        await manejador.ManejarAsync(new CancelarViajeComando(id, solicitud.Motivo), cancellationToken);
        return Ok();
    }

    private Guid ConductorAutenticado() => Guid.Parse(User.FindFirstValue("sub")!);

    private static bool RutaValida(RutaViajeSolicitud ruta, out string? error)
    {
        error = null;
        if (ruta.Puntos.Count < 2)
            error = "La ruta debe contener al menos un origen y un destino.";
        else if (ruta.Puntos.Any(p => !double.IsFinite(p.Latitud) || !double.IsFinite(p.Longitud) || p.Latitud is < -90 or > 90 || p.Longitud is < -180 or > 180))
            error = "La ruta contiene coordenadas fuera de rango.";
        else if (!string.Equals(ruta.Puntos[0].Tipo, "ORIGEN", StringComparison.Ordinal) || !string.Equals(ruta.Puntos[^1].Tipo, "DESTINO", StringComparison.Ordinal))
            error = "El primer punto debe ser ORIGEN y el último DESTINO.";
        else if (!string.Equals(ruta.Puntos[0].Etiqueta.Trim(), ruta.Origen.Trim(), StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(ruta.Puntos[^1].Etiqueta.Trim(), ruta.Destino.Trim(), StringComparison.OrdinalIgnoreCase))
            error = "Las etiquetas de origen y destino deben coincidir con sus puntos de ruta.";

        return error is null;
    }
}
