using System.Text.Json;
using System.Text.Json.Nodes;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Salientes;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Booking;
using ShareWay.TripExecution.Infrastructure.Persistencia.Outbox;

namespace ShareWay.TripExecution.Tests.Integracion;

/// <summary>Cada evento del outbox se traduce al contrato de <c>POST /api/v1/internal/events</c> de Booking.</summary>
public sealed class ContratoEventosBookingTests
{
    private static readonly DateTimeOffset Ocurrido = new(2026, 11, 27, 13, 30, 0, TimeSpan.Zero);
    private static readonly Guid IdEvento = Guid.NewGuid();
    private static readonly Guid ViajeId = Guid.NewGuid();
    private static readonly Guid GrupoId = Guid.NewGuid();
    private static readonly Guid ConductorId = Guid.NewGuid();

    /// <summary>
    /// Pasa por el outbox real (serialización incluida), como en producción, y devuelve
    /// el JSON tal como sale por la red.
    /// </summary>
    private static JsonObject? Traducir(IEventoDeIntegracion evento)
    {
        var mensaje = MensajeOutbox.Crear(evento);
        var cuerpo = ContratoEventosBooking.Construir(mensaje.Tipo, mensaje.Contenido);
        return cuerpo is null ? null : JsonNode.Parse(cuerpo.ToJsonString())!.AsObject();
    }

    private static void AfirmarCabecera(JsonObject cuerpo, string tipo)
    {
        Assert.Equal(IdEvento, cuerpo["eventId"]!.GetValue<Guid>());
        Assert.Equal("2026-11-27T13:30:00+00:00", cuerpo["occurredAt"]!.GetValue<string>());
        Assert.Equal(tipo, cuerpo["type"]!.GetValue<string>());
    }

    private static string[] Claves(JsonObject cuerpo) => cuerpo.Select(p => p.Key).Order().ToArray();

    [Fact]
    public void ViajeIniciado_EsTripStarted()
    {
        var cuerpo = Traducir(new ViajeIniciadoIntegracion(IdEvento, Ocurrido, ViajeId, GrupoId, ConductorId))!;

        AfirmarCabecera(cuerpo, "TripStarted");
        Assert.Equal(GrupoId, cuerpo["groupId"]!.GetValue<Guid>());
        Assert.Equal(ViajeId, cuerpo["tripId"]!.GetValue<Guid>());
        Assert.Equal(["eventId", "groupId", "occurredAt", "tripId", "type"], Claves(cuerpo));
    }

    [Fact]
    public void PasajeroAbordo_EsBoardingValidated()
    {
        var reserva = Guid.NewGuid();

        var cuerpo = Traducir(new PasajeroAbordoIntegracion(IdEvento, Ocurrido, ViajeId, reserva, Guid.NewGuid()))!;

        AfirmarCabecera(cuerpo, "BoardingValidated");
        Assert.Equal(reserva, cuerpo["bookingId"]!.GetValue<Guid>());
        Assert.Equal(ViajeId, cuerpo["tripId"]!.GetValue<Guid>());
        Assert.Equal(["bookingId", "eventId", "occurredAt", "tripId", "type"], Claves(cuerpo));
    }

    [Fact]
    public void ViajeFinalizado_EsTripCompleted()
    {
        var fin = Ocurrido.AddMinutes(-1);
        Guid[] abordadas = [Guid.NewGuid(), Guid.NewGuid()];

        var cuerpo = Traducir(new ViajeFinalizadoIntegracion(
            IdEvento, Ocurrido, ViajeId, GrupoId, ConductorId, Ocurrido.AddMinutes(-30), fin, abordadas, 12.34))!;

        AfirmarCabecera(cuerpo, "TripCompleted");
        Assert.Equal(GrupoId, cuerpo["groupId"]!.GetValue<Guid>());
        Assert.Equal(ViajeId, cuerpo["tripId"]!.GetValue<Guid>());
        Assert.Equal(ConductorId, cuerpo["driverId"]!.GetValue<Guid>());
        Assert.Equal(fin, DateTimeOffset.Parse(cuerpo["completedAt"]!.GetValue<string>()));
        Assert.Equal(12.34, cuerpo["distanceKm"]!.GetValue<double>());
        Assert.Equal(abordadas, cuerpo["boardedBookingIds"]!.AsArray().Select(n => n!.GetValue<Guid>()));
        Assert.Equal(
            ["boardedBookingIds", "completedAt", "distanceKm", "driverId", "eventId", "groupId", "occurredAt", "tripId", "type"],
            Claves(cuerpo));
    }

    [Fact]
    public void ViajeFinalizado_SinRuta_EnviaDistanceKmNull()
    {
        var cuerpo = Traducir(new ViajeFinalizadoIntegracion(
            IdEvento, Ocurrido, ViajeId, GrupoId, ConductorId, Ocurrido, Ocurrido, [], DistanciaKm: null))!;

        Assert.True(cuerpo.ContainsKey("distanceKm"));
        Assert.Null(cuerpo["distanceKm"]);
        Assert.Contains("\"distanceKm\":null", cuerpo.ToJsonString());
    }

    [Fact]
    public void ViajeCancelado_EsTripCancelled()
    {
        var cuerpo = Traducir(new ViajeCanceladoIntegracion(IdEvento, Ocurrido, ViajeId, GrupoId, "Conductor sin vehículo"))!;

        AfirmarCabecera(cuerpo, "TripCancelled");
        Assert.Equal(GrupoId, cuerpo["groupId"]!.GetValue<Guid>());
        Assert.Equal(ViajeId, cuerpo["tripId"]!.GetValue<Guid>());
        Assert.Equal("Conductor sin vehículo", cuerpo["reason"]!.GetValue<string>());
        Assert.Equal(["eventId", "groupId", "occurredAt", "reason", "tripId", "type"], Claves(cuerpo));
    }

    public static TheoryData<IEventoDeIntegracion> EventosQueNoSeEnvian => new()
    {
        new ViajeCreadoIntegracion(IdEvento, Ocurrido, ViajeId, GrupoId, ConductorId),
        new AbordajeFallidoIntegracion(IdEvento, Ocurrido, ViajeId, Guid.NewGuid(), Guid.NewGuid(), 1, false),
        new AlertaEmergenciaRegistradaIntegracion(IdEvento, Ocurrido, Guid.NewGuid(), ViajeId, Guid.NewGuid(), "DRIVER", "Accidente"),
        new AlertaEmergenciaAtendidaIntegracion(IdEvento, Ocurrido, Guid.NewGuid(), ViajeId, Guid.NewGuid())
    };

    [Theory]
    [MemberData(nameof(EventosQueNoSeEnvian))]
    public void EventosQueBookingNoConsume_NoGeneranCuerpo(IEventoDeIntegracion evento)
    {
        Assert.Null(Traducir(evento));
    }

    [Fact]
    public void ContenidoIlegible_LanzaJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => ContratoEventosBooking.Construir(nameof(ViajeCanceladoIntegracion), "{no es json"));
    }
}
