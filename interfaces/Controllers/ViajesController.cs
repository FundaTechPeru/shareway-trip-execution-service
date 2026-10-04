using Microsoft.AspNetCore.Mvc;
using ShareWay.TripExecution.Api.Contratos.Solicitudes;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Viajes.Comandos.CancelarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.FinalizarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.IniciarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.ValidarAbordaje;
using ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;

namespace ShareWay.TripExecution.Api.Controllers;

[ApiController]
[Route("api/viajes")]
public sealed class ViajesController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] CrearViajeSolicitud solicitud, [FromServices] IManejadorDeComando<CrearViajeComando, Guid> manejador, CancellationToken cancellationToken)
    {
        var id = await manejador.ManejarAsync(new CrearViajeComando(solicitud.GrupoId, solicitud.ConductorId, solicitud.Pasajeros), cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ViajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ViajeDto>> ObtenerPorId(Guid id, [FromServices] IManejadorDeConsulta<ObtenerViajeConsulta, ViajeDto> manejador, CancellationToken cancellationToken)
    {
        var viaje = await manejador.ManejarAsync(new ObtenerViajeConsulta(id), cancellationToken);
        return Ok(viaje);
    }

    [HttpPost("{id:guid}/abordajes")]
    [ProducesResponseType(typeof(ValidarAbordajeRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ValidarAbordajeRespuesta>> ValidarAbordaje(Guid id, [FromBody] ValidarAbordajeSolicitud solicitud, [FromServices] IManejadorDeComando<ValidarAbordajeComando, ValidarAbordajeRespuesta> manejador, CancellationToken cancellationToken)
    {
        var respuesta = await manejador.ManejarAsync(new ValidarAbordajeComando(id, solicitud.ConductorId, solicitud.ReservaId, solicitud.Codigo, solicitud.Metodo), cancellationToken);
        return Ok(respuesta);
    }

    [HttpPost("{id:guid}/iniciar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Iniciar(Guid id, [FromBody] IniciarViajeSolicitud solicitud, [FromServices] IManejadorDeComando<IniciarViajeComando> manejador, CancellationToken cancellationToken)
    {
        await manejador.ManejarAsync(new IniciarViajeComando(id, solicitud.ConductorId), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/finalizar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Finalizar(Guid id, [FromBody] FinalizarViajeSolicitud solicitud, [FromServices] IManejadorDeComando<FinalizarViajeComando> manejador, CancellationToken cancellationToken)
    {
        await manejador.ManejarAsync(new FinalizarViajeComando(id, solicitud.ConductorId), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarViajeSolicitud solicitud, [FromServices] IManejadorDeComando<CancelarViajeComando> manejador, CancellationToken cancellationToken)
    {
        await manejador.ManejarAsync(new CancelarViajeComando(id, solicitud.Motivo), cancellationToken);
        return Ok();
    }
}