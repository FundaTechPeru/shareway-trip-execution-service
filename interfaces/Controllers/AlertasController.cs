using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareWay.TripExecution.Api.Contratos.Solicitudes;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Viajes.Comandos.AtenderAlerta;
using ShareWay.TripExecution.Application.Viajes.Comandos.RegistrarAlerta;
using ShareWay.TripExecution.Application.Viajes.Consultas.ListarAlertas;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Api.Controllers;

/// <summary>
/// Alertas de emergencia. Las abre el conductor del viaje o un pasajero con abordaje confirmado;
/// las resuelve el personal admin. La lista admin es la que lee la pantalla "Alertas" del panel.
/// </summary>
[ApiController]
public sealed class AlertasController : ControllerBase
{
    [HttpPost("api/viajes/{id:guid}/alertas")]
    [Authorize(Policy = "DriverOrPassenger")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Registrar(
        Guid id,
        [FromBody] RegistrarAlertaSolicitud solicitud,
        [FromServices] IManejadorDeComando<RegistrarAlertaComando, Guid> manejador,
        CancellationToken cancellationToken)
    {
        var rol = User.FindFirstValue("role") ?? string.Empty;
        var sub = Guid.Parse(User.FindFirstValue("sub")!);
        var alertaId = await manejador.ManejarAsync(
            new RegistrarAlertaComando(id, sub, rol, solicitud.Tipo, solicitud.Descripcion),
            cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { alertaId }, new { id = alertaId });
    }

    [HttpGet("api/viajes/alertas")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PaginaAlertasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginaAlertasDto>> Listar(
        [FromQuery] EstadoAlerta? estado,
        [FromQuery] int pagina = 0,
        [FromQuery] int tamano = 20,
        [FromServices] IManejadorDeConsulta<ListarAlertasConsulta, PaginaAlertasDto> manejador = null!,
        CancellationToken cancellationToken = default)
    {
        var pagina1 = await manejador.ManejarAsync(new ListarAlertasConsulta(estado, pagina, tamano), cancellationToken);
        return Ok(pagina1);
    }

    [HttpGet("api/viajes/alertas/{alertaId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult Obtener(Guid alertaId) => Ok(new { id = alertaId });

    [HttpPatch("api/viajes/alertas/{alertaId:guid}/atender")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atender(
        Guid alertaId,
        [FromServices] IManejadorDeComando<AtenderAlertaComando> manejador,
        CancellationToken cancellationToken)
    {
        var sub = Guid.Parse(User.FindFirstValue("sub")!);
        await manejador.ManejarAsync(new AtenderAlertaComando(alertaId, sub), cancellationToken);
        return NoContent();
    }
}
