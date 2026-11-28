using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Entrantes;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Consumidores;
using ShareWay.TripExecution.Infrastructure.Seguridad;

namespace ShareWay.TripExecution.Api.Controllers;

/// <summary>
/// Endpoints servicio-a-servicio. Fuera de <c>api/viajes</c> a propósito: el Gateway solo
/// reenvía <c>/api/v1/trips</c> → <c>/api/viajes</c>, así que estas rutas no son públicas.
/// Se autentican solo con <c>X-Internal-Api-Key</c> (sin JWT).
/// </summary>
[ApiController]
[Route("api/internal")]
[AllowAnonymous]
[RequiereClaveInterna]
public sealed partial class IntegracionInternaController : ControllerBase
{
    /// <summary>Booking &amp; Pricing confirma un grupo: crea su viaje (201) o devuelve el existente (200).</summary>
    [HttpPost("grupos-confirmados")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecibirGrupoConfirmado(
        [FromBody] GrupoConfirmadoEvento evento,
        [FromServices] GrupoConfirmadoConsumidor consumidor,
        CancellationToken cancellationToken)
    {
        Validar(evento);
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var resultado = await consumidor.ProcesarAsync(evento, cancellationToken);
        var cuerpo = new { id = resultado.Id };
        return resultado.Creado ? StatusCode(StatusCodes.Status201Created, cuerpo) : Ok(cuerpo);
    }

    private void Validar(GrupoConfirmadoEvento evento)
    {
        if (evento.GrupoId == Guid.Empty) ModelState.AddModelError("grupoId", "grupoId es obligatorio.");
        if (evento.ConductorId == Guid.Empty) ModelState.AddModelError("conductorId", "conductorId es obligatorio.");
        if (evento.Pasajeros is not { Count: > 0 })
        {
            ModelState.AddModelError("pasajeros", "Se requiere al menos un pasajero.");
            return;
        }

        for (var i = 0; i < evento.Pasajeros.Count; i++)
        {
            var p = evento.Pasajeros[i];
            var prefijo = $"pasajeros[{i}]";
            if (p is null)
            {
                ModelState.AddModelError(prefijo, "El pasajero no puede ser nulo.");
                continue;
            }
            if (p.ReservaId == Guid.Empty) ModelState.AddModelError($"{prefijo}.reservaId", "reservaId es obligatorio.");
            if (p.PasajeroId == Guid.Empty) ModelState.AddModelError($"{prefijo}.pasajeroId", "pasajeroId es obligatorio.");
            if (p.CodigoHash is null || !HashSha256().IsMatch(p.CodigoHash))
                ModelState.AddModelError($"{prefijo}.codigoHash", "codigoHash debe ser un SHA-256 en hexadecimal (64 caracteres).");
            if (p.PinHash is not null && !HashSha256().IsMatch(p.PinHash))
                ModelState.AddModelError($"{prefijo}.pinHash", "pinHash debe ser un SHA-256 en hexadecimal (64 caracteres).");
        }
    }

    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    private static partial Regex HashSha256();
}
