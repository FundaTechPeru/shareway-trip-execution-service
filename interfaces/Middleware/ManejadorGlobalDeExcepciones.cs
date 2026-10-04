using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Application.Excepciones;
using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes.Excepciones;

namespace ShareWay.TripExecution.Api.Middleware;

public sealed class ManejadorGlobalDeExcepciones(ILogger<ManejadorGlobalDeExcepciones> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        var (estado, titulo) = excepcion switch
        {
            ViajeNoEncontradoException => (StatusCodes.Status404NotFound, "Viaje no encontrado"),
            ConductorNoAsignadoException => (StatusCodes.Status403Forbidden, "Conductor no autorizado para este viaje"),
            ExcepcionDeDominio => (StatusCodes.Status409Conflict, "Regla de negocio violada"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia; reintente"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            _ => (0, string.Empty)
        };

        if (estado == 0)
            return false;

        log.LogWarning(excepcion, "{Titulo}", titulo);
        contexto.Response.StatusCode = estado;
        await contexto.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = estado, Title = titulo, Detail = excepcion.Message }, ct);
        return true;
    }
}
