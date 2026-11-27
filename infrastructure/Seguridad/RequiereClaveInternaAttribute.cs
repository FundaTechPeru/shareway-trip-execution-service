using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ShareWay.TripExecution.Infrastructure.Seguridad;

/// <summary>
/// Protege un endpoint servicio-a-servicio: exige <c>X-Internal-Api-Key</c> y responde 401 si falta
/// o no coincide. Corre como filtro de autorización, antes del model binding y la validación.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiereClaveInternaAttribute() : TypeFilterAttribute(typeof(FiltroClaveInterna));

internal sealed class FiltroClaveInterna(ClaveInterna clave, ILogger<FiltroClaveInterna> log) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext contexto)
    {
        var encabezados = contexto.HttpContext.Request.Headers[ClaveInterna.Encabezado];
        if (encabezados.Count == 1 && clave.Coincide(encabezados[0]))
            return;

        log.LogWarning("Llamada interna rechazada a {Ruta}: {Encabezado} ausente o inválido.",
            contexto.HttpContext.Request.Path, ClaveInterna.Encabezado);
        contexto.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Clave interna ausente o inválida"
        })
        { StatusCode = StatusCodes.Status401Unauthorized };
    }
}
