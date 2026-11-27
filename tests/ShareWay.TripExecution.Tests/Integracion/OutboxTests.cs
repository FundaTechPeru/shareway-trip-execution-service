using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Salientes;
using ShareWay.TripExecution.Infrastructure.Mensajeria;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Booking;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Outbox;
using ShareWay.TripExecution.Infrastructure.Persistencia;
using ShareWay.TripExecution.Infrastructure.Persistencia.Outbox;
using ShareWay.TripExecution.Infrastructure.Seguridad;

namespace ShareWay.TripExecution.Tests.Integracion;

public sealed class OutboxTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 11, 27, 13, 0, 0, TimeSpan.Zero);

    // ---------- Clasificación de respuestas HTTP ----------

    [Theory]
    [InlineData(200, EstadoPublicacion.Publicado)]
    [InlineData(201, EstadoPublicacion.Publicado)]
    [InlineData(202, EstadoPublicacion.Publicado)]
    [InlineData(204, EstadoPublicacion.Publicado)]
    [InlineData(500, EstadoPublicacion.Reintentar)]
    [InlineData(502, EstadoPublicacion.Reintentar)]
    [InlineData(503, EstadoPublicacion.Reintentar)]
    [InlineData(504, EstadoPublicacion.Reintentar)]
    [InlineData(401, EstadoPublicacion.Reintentar)]
    [InlineData(408, EstadoPublicacion.Reintentar)]
    [InlineData(429, EstadoPublicacion.Reintentar)]
    [InlineData(400, EstadoPublicacion.Fallido)]
    [InlineData(403, EstadoPublicacion.Fallido)]
    [InlineData(404, EstadoPublicacion.Fallido)]
    [InlineData(409, EstadoPublicacion.Fallido)]
    [InlineData(422, EstadoPublicacion.Fallido)]
    public void ClasificaLosCodigosHttp(int codigo, EstadoPublicacion esperado)
    {
        Assert.Equal(esperado, ClasificadorDeErroresHttp.Clasificar((HttpStatusCode)codigo));
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    [InlineData(6, 160)]
    [InlineData(7, 300)]
    [InlineData(50, 300)]
    [InlineData(int.MaxValue, 300)]
    public void EsperaExponencial_ConTopeDeCincoMinutos(int intentos, int segundos)
    {
        Assert.Equal(TimeSpan.FromSeconds(segundos), PoliticaDeReintentos.Espera(intentos));
    }

    // ---------- Broker HTTP hacia Booking ----------

    private sealed class ManejadorFalso(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Solicitud, string Cuerpo)> Recibidas { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken ct)
        {
            Recibidas.Add((solicitud, solicitud.Content is null ? "" : await solicitud.Content.ReadAsStringAsync(ct)));
            return await responder(solicitud);
        }
    }

    private static (BrokerHttpBooking Broker, ManejadorFalso Manejador) Broker(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var manejador = new ManejadorFalso(responder);
        var http = new HttpClient(manejador) { BaseAddress = BrokerHttpBooking.DireccionBase("https://booking.test/base") };
        return (new BrokerHttpBooking(http, new ClaveInterna("clave-de-prueba"), NullLogger<BrokerHttpBooking>.Instance), manejador);
    }

    private static Func<HttpRequestMessage, Task<HttpResponseMessage>> Responder(HttpStatusCode codigo) =>
        _ => Task.FromResult(new HttpResponseMessage(codigo) { Content = new StringContent("detalle del error") });

    private static MensajeOutbox Cancelado(DateTimeOffset ocurrido) =>
        MensajeOutbox.Crear(new ViajeCanceladoIntegracion(Guid.NewGuid(), ocurrido, Guid.NewGuid(), Guid.NewGuid(), "motivo"));

    [Fact]
    public async Task Broker_HacePostAlEndpointDeEventosConLaClaveInterna()
    {
        var (broker, manejador) = Broker(Responder(HttpStatusCode.Accepted));
        var mensaje = Cancelado(Inicio);

        var resultado = await broker.PublicarAsync(mensaje.Tipo, mensaje.Contenido, CancellationToken.None);

        Assert.Equal(EstadoPublicacion.Publicado, resultado.Estado);
        var (solicitud, cuerpo) = Assert.Single(manejador.Recibidas);
        Assert.Equal(HttpMethod.Post, solicitud.Method);
        Assert.Equal("https://booking.test/base/api/v1/internal/events", solicitud.RequestUri!.ToString());
        Assert.Equal("clave-de-prueba", Assert.Single(solicitud.Headers.GetValues("X-Internal-Api-Key")));
        Assert.Equal("application/json", solicitud.Content!.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal("TripCancelled", json.RootElement.GetProperty("type").GetString());
        Assert.Equal(mensaje.Id, json.RootElement.GetProperty("eventId").GetGuid());
    }

    [Fact]
    public async Task Broker_EventoNoConsumidoPorBooking_SeOmiteSinLlamar()
    {
        var (broker, manejador) = Broker(Responder(HttpStatusCode.OK));
        var mensaje = MensajeOutbox.Crear(new ViajeCreadoIntegracion(Guid.NewGuid(), Inicio, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        var resultado = await broker.PublicarAsync(mensaje.Tipo, mensaje.Contenido, CancellationToken.None);

        Assert.Equal(EstadoPublicacion.Omitido, resultado.Estado);
        Assert.Empty(manejador.Recibidas);
    }

    [Theory]
    [InlineData(503, EstadoPublicacion.Reintentar)]
    [InlineData(401, EstadoPublicacion.Reintentar)]
    [InlineData(429, EstadoPublicacion.Reintentar)]
    [InlineData(400, EstadoPublicacion.Fallido)]
    [InlineData(404, EstadoPublicacion.Fallido)]
    public async Task Broker_RespuestaDeError_SeClasificaYConservaElDetalle(int codigo, EstadoPublicacion esperado)
    {
        var (broker, _) = Broker(Responder((HttpStatusCode)codigo));
        var mensaje = Cancelado(Inicio);

        var resultado = await broker.PublicarAsync(mensaje.Tipo, mensaje.Contenido, CancellationToken.None);

        Assert.Equal(esperado, resultado.Estado);
        Assert.Contains(codigo.ToString(), resultado.Error);
        Assert.Contains("detalle del error", resultado.Error);
    }

    [Fact]
    public async Task Broker_Timeout_SeReintenta()
    {
        var (broker, _) = Broker(_ => throw new TaskCanceledException("timeout", new TimeoutException()));
        var mensaje = Cancelado(Inicio);

        var resultado = await broker.PublicarAsync(mensaje.Tipo, mensaje.Contenido, CancellationToken.None);

        Assert.Equal(EstadoPublicacion.Reintentar, resultado.Estado);
        Assert.Contains("Timeout", resultado.Error);
    }

    [Fact]
    public async Task Broker_ErrorDeConexion_SeReintenta()
    {
        var (broker, _) = Broker(_ => throw new HttpRequestException("No se puede establecer conexión"));
        var mensaje = Cancelado(Inicio);

        var resultado = await broker.PublicarAsync(mensaje.Tipo, mensaje.Contenido, CancellationToken.None);

        Assert.Equal(EstadoPublicacion.Reintentar, resultado.Estado);
    }

    [Fact]
    public async Task Broker_CancelacionDelServicio_NoSeConfundeConTimeout()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var (broker, _) = Broker(_ => throw new TaskCanceledException());
        var mensaje = Cancelado(Inicio);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => broker.PublicarAsync(mensaje.Tipo, mensaje.Contenido, cts.Token));
    }

    // ---------- Procesador del outbox ----------

    private sealed class RelojFalso(DateTimeOffset ahora) : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = ahora;
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    /// <summary>Responde según el motivo de cancelación del evento y registra el orden de envío.</summary>
    private sealed class BrokerGuionado(Func<string, ResultadoPublicacion> responder) : IBrokerDeMensajes
    {
        public List<string> Enviados { get; } = [];

        public Task<ResultadoPublicacion> PublicarAsync(string tipo, string contenidoJson, CancellationToken ct)
        {
            var motivo = JsonSerializer.Deserialize<ViajeCanceladoIntegracion>(contenidoJson)!.Motivo;
            Enviados.Add(motivo);
            return Task.FromResult(responder(motivo));
        }
    }

    private static TripExecutionDbContext Contexto(string nombre) =>
        new(new DbContextOptionsBuilder<TripExecutionDbContext>().UseInMemoryDatabase(nombre).Options);

    private static async Task<string> SembrarAsync(params string[] motivos)
    {
        var nombre = $"outbox-{Guid.NewGuid()}";
        await using var contexto = Contexto(nombre);
        for (var i = 0; i < motivos.Length; i++)
            contexto.Outbox.Add(MensajeOutbox.Crear(new ViajeCanceladoIntegracion(Guid.NewGuid(), Inicio.AddSeconds(i), Guid.NewGuid(), Guid.NewGuid(), motivos[i])));
        await contexto.SaveChangesAsync();
        return nombre;
    }

    private static async Task ProcesarAsync(string nombre, IBrokerDeMensajes broker, TimeProvider reloj)
    {
        await using var contexto = Contexto(nombre);
        await ProcesadorOutbox.ProcesarLoteAsync(contexto, broker, reloj, NullLogger.Instance, CancellationToken.None);
    }

    private static async Task<Dictionary<string, MensajeOutbox>> LeerAsync(string nombre)
    {
        await using var contexto = Contexto(nombre);
        var mensajes = await contexto.Outbox.AsNoTracking().ToListAsync();
        return mensajes.ToDictionary(m => JsonSerializer.Deserialize<ViajeCanceladoIntegracion>(m.Contenido)!.Motivo);
    }

    [Fact]
    public async Task Procesador_Exito_MarcaProcesadosEnOrden()
    {
        var nombre = await SembrarAsync("a", "b", "c");
        var broker = new BrokerGuionado(_ => ResultadoPublicacion.Publicado);

        await ProcesarAsync(nombre, broker, new RelojFalso(Inicio));

        Assert.Equal(["a", "b", "c"], broker.Enviados);
        Assert.All((await LeerAsync(nombre)).Values, m => Assert.Equal(Inicio, m.ProcesadoEn));
    }

    [Fact]
    public async Task Procesador_EventoOmitido_SeMarcaProcesado()
    {
        var nombre = await SembrarAsync("a");

        await ProcesarAsync(nombre, new BrokerGuionado(_ => ResultadoPublicacion.Omitido), new RelojFalso(Inicio));

        Assert.NotNull((await LeerAsync(nombre))["a"].ProcesadoEn);
    }

    [Fact]
    public async Task Procesador_FalloTransitorio_ProgramaReintentoYDetieneElLote()
    {
        var nombre = await SembrarAsync("a", "b", "c");
        var broker = new BrokerGuionado(m => m == "b" ? ResultadoPublicacion.Reintentar("503 Service Unavailable") : ResultadoPublicacion.Publicado);

        await ProcesarAsync(nombre, broker, new RelojFalso(Inicio));

        Assert.Equal(["a", "b"], broker.Enviados);
        var mensajes = await LeerAsync(nombre);
        Assert.NotNull(mensajes["a"].ProcesadoEn);
        Assert.Null(mensajes["b"].ProcesadoEn);
        Assert.Null(mensajes["b"].FallidoEn);
        Assert.Equal(1, mensajes["b"].Intentos);
        Assert.Equal(Inicio + PoliticaDeReintentos.Espera(1), mensajes["b"].SiguienteIntentoEn);
        Assert.Equal("503 Service Unavailable", mensajes["b"].UltimoError);
        Assert.Null(mensajes["c"].ProcesadoEn);
    }

    [Fact]
    public async Task Procesador_RespetaLaEsperaYLuegoReintenta()
    {
        var nombre = await SembrarAsync("a", "b");
        var reloj = new RelojFalso(Inicio);
        var falla = true;
        var broker = new BrokerGuionado(_ => falla ? ResultadoPublicacion.Reintentar("timeout") : ResultadoPublicacion.Publicado);

        await ProcesarAsync(nombre, broker, reloj);
        falla = false;
        reloj.Ahora = Inicio + PoliticaDeReintentos.Espera(1) - TimeSpan.FromSeconds(1);
        await ProcesarAsync(nombre, broker, reloj);

        Assert.Equal(["a"], broker.Enviados);

        reloj.Ahora = Inicio + PoliticaDeReintentos.Espera(1);
        await ProcesarAsync(nombre, broker, reloj);

        Assert.Equal(["a", "a", "b"], broker.Enviados);
        var mensajes = await LeerAsync(nombre);
        Assert.NotNull(mensajes["a"].ProcesadoEn);
        Assert.NotNull(mensajes["b"].ProcesadoEn);
        Assert.Equal(1, mensajes["a"].Intentos);
    }

    [Fact]
    public async Task Procesador_FallosSeguidos_DuplicanLaEspera()
    {
        var nombre = await SembrarAsync("a");
        var reloj = new RelojFalso(Inicio);
        var broker = new BrokerGuionado(_ => ResultadoPublicacion.Reintentar("timeout"));

        await ProcesarAsync(nombre, broker, reloj);
        reloj.Ahora = (await LeerAsync(nombre))["a"].SiguienteIntentoEn!.Value;
        await ProcesarAsync(nombre, broker, reloj);

        var mensaje = (await LeerAsync(nombre))["a"];
        Assert.Equal(2, mensaje.Intentos);
        Assert.Equal(reloj.Ahora + PoliticaDeReintentos.Espera(2), mensaje.SiguienteIntentoEn);
    }

    [Fact]
    public async Task Procesador_RechazoDefinitivo_MarcaFallidoYSigueConElSiguiente()
    {
        var nombre = await SembrarAsync("a", "malo", "c");
        var broker = new BrokerGuionado(m => m == "malo" ? ResultadoPublicacion.Fallido("400 Bad Request") : ResultadoPublicacion.Publicado);

        await ProcesarAsync(nombre, broker, new RelojFalso(Inicio));

        Assert.Equal(["a", "malo", "c"], broker.Enviados);
        var mensajes = await LeerAsync(nombre);
        Assert.Equal(Inicio, mensajes["malo"].FallidoEn);
        Assert.Null(mensajes["malo"].ProcesadoEn);
        Assert.Equal("400 Bad Request", mensajes["malo"].UltimoError);
        Assert.NotNull(mensajes["c"].ProcesadoEn);

        // Un mensaje fallido ya no vuelve a enviarse.
        await ProcesarAsync(nombre, broker, new RelojFalso(Inicio.AddHours(1)));
        Assert.Equal(3, broker.Enviados.Count);
    }

    [Fact]
    public async Task Procesador_ExcepcionDelBroker_SeTrataComoReintento()
    {
        var nombre = await SembrarAsync("a", "b");
        var broker = new BrokerGuionado(_ => throw new InvalidOperationException("inesperado"));

        await ProcesarAsync(nombre, broker, new RelojFalso(Inicio));

        var mensajes = await LeerAsync(nombre);
        Assert.Equal(1, mensajes["a"].Intentos);
        Assert.NotNull(mensajes["a"].SiguienteIntentoEn);
        Assert.Equal(0, mensajes["b"].Intentos);
    }
}
