using System.Text.Json;
using System.Text.Json.Nodes;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Salientes;

namespace ShareWay.TripExecution.Infrastructure.Mensajeria.Booking;

/// <summary>
/// Traduce los eventos de integración guardados en el outbox al contrato de
/// <c>POST /api/v1/internal/events</c> de Booking &amp; Pricing:
/// <c>{"eventId", "occurredAt", "type", ...campos}</c>.
/// </summary>
public static class ContratoEventosBooking
{
    /// <returns>El cuerpo JSON a enviar, o <c>null</c> si Booking no consume ese evento.</returns>
    /// <exception cref="JsonException">El contenido guardado no corresponde al tipo.</exception>
    public static JsonObject? Construir(string tipo, string contenidoJson)
    {
        switch (tipo)
        {
            case nameof(ViajeIniciadoIntegracion):
            {
                var e = Leer<ViajeIniciadoIntegracion>(contenidoJson);
                return Cuerpo(e, "TripStarted",
                    ("groupId", JsonValue.Create(e.GrupoId)),
                    ("tripId", JsonValue.Create(e.ViajeId)));
            }
            case nameof(PasajeroAbordoIntegracion):
            {
                var e = Leer<PasajeroAbordoIntegracion>(contenidoJson);
                return Cuerpo(e, "BoardingValidated",
                    ("bookingId", JsonValue.Create(e.ReservaId)),
                    ("tripId", JsonValue.Create(e.ViajeId)));
            }
            case nameof(ViajeFinalizadoIntegracion):
            {
                var e = Leer<ViajeFinalizadoIntegracion>(contenidoJson);
                var abordadas = new JsonArray((e.ReservasAbordadas ?? []).Select(id => (JsonNode?)JsonValue.Create(id)).ToArray());
                return Cuerpo(e, "TripCompleted",
                    ("groupId", JsonValue.Create(e.GrupoId)),
                    ("tripId", JsonValue.Create(e.ViajeId)),
                    ("driverId", JsonValue.Create(e.ConductorId)),
                    ("completedAt", JsonValue.Create(e.FinReal)),
                    ("distanceKm", JsonValue.Create(e.DistanciaKm)),
                    ("boardedBookingIds", abordadas));
            }
            case nameof(ViajeCanceladoIntegracion):
            {
                var e = Leer<ViajeCanceladoIntegracion>(contenidoJson);
                return Cuerpo(e, "TripCancelled",
                    ("groupId", JsonValue.Create(e.GrupoId)),
                    ("tripId", JsonValue.Create(e.ViajeId)),
                    ("reason", JsonValue.Create(e.Motivo)));
            }
            default:
                // ViajeCreado, AbordajeFallido y alertas: Booking no los consume.
                return null;
        }
    }

    private static T Leer<T>(string json) =>
        JsonSerializer.Deserialize<T>(json) ?? throw new JsonException($"El contenido de {typeof(T).Name} está vacío.");

    private static JsonObject Cuerpo(IEventoDeIntegracion evento, string tipo, params (string Clave, JsonNode? Valor)[] campos)
    {
        var cuerpo = new JsonObject
        {
            ["eventId"] = JsonValue.Create(evento.IdEvento),
            ["occurredAt"] = JsonValue.Create(evento.OcurridoEn),
            ["type"] = tipo
        };
        foreach (var (clave, valor) in campos)
            cuerpo[clave] = valor;
        return cuerpo;
    }
}
