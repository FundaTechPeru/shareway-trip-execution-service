using System.Text.Json;
using System.Text.Json.Serialization;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;

public sealed record AbordajeDto(Guid ReservaId, Guid PasajeroId, bool Abordo, DateTimeOffset? AbordadoEn, int IntentosFallidos, bool Bloqueado);
public sealed record PuntoRutaDto(
    string Etiqueta,
    double Latitud,
    double Longitud,
    string Tipo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? ReservaId = null);
public sealed record RutaViajeDto(DateTimeOffset SalidaProgramada, string Origen, string Destino, IReadOnlyList<PuntoRutaDto> Puntos, bool EsPrueba = false);
public sealed record ViajeDto(
    Guid Id,
    Guid GrupoId,
    Guid ConductorId,
    Guid? VehiculoId,
    string Estado,
    DateTimeOffset? InicioReal,
    DateTimeOffset? FinReal,
    IReadOnlyList<AbordajeDto> Abordajes,
    RutaViajeDto? Ruta)
{
    public static ViajeDto Desde(Viaje v) => new(
        v.Id.Valor,
        v.GrupoId.Valor,
        v.ConductorId.Valor,
        v.VehiculoId?.Valor,
        v.Estado.ToString(),
        v.InicioReal,
        v.FinReal,
        v.Abordajes
        .Select(a => new AbordajeDto(a.Id.Valor, a.PasajeroId.Valor, a.EstaAbordado, a.AbordadoEn, a.IntentosFallidos, a.EstaBloqueado))
        .ToList(),
        LeerRuta(v.RutaJson));

    private static RutaViajeDto? LeerRuta(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<RutaViajeDto>(json, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
