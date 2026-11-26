using Reqnroll;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Domain.Viajes.Excepciones;

namespace ShareWay.TripExecution.Tests.Steps;

/// <summary>
/// Pasos compartidos por <c>Boarding.feature</c> y <c>TripLifecycle.feature</c>.
/// Reqnroll crea una instancia por escenario, así que el estado vive en los campos.
/// </summary>
[Binding]
public sealed class PasosDeViaje
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private Viaje _viaje = default!;
    private ConductorId _conductor;
    private ReservaId _reserva;
    private ResultadoAbordaje? _ultimoResultado;
    private Exception? _ultimoError;
    private readonly Dictionary<string, ReservaId> _reservasPorNombre = new();

    // ---------- Dado ----------

    [Given("un viaje confirmado con un pasajero cuyo código es {string}")]
    public void DadoUnViajeConfirmado(string codigo)
    {
        _reserva = ReservaId.Nuevo();
        var pasajero = new PasajeroConfirmado(_reserva, PasajeroId.Nuevo(), CodigoAbordaje.DesdeTextoPlano(codigo));
        _viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { pasajero }, Ahora);
        _conductor = _viaje.ConductorId;
    }

    [Given("un viaje en curso con paradas para {string} y {string}")]
    public void DadoUnViajeEnCursoConParadas(string primero, string segundo)
    {
        var r1 = ReservaId.Nuevo();
        var r2 = ReservaId.Nuevo();
        _reservasPorNombre[primero] = r1;
        _reservasPorNombre[segundo] = r2;

        var p1 = new PasajeroConfirmado(r1, PasajeroId.Nuevo(), CodigoAbordaje.DesdeTextoPlano("1111"));
        var p2 = new PasajeroConfirmado(r2, PasajeroId.Nuevo(), CodigoAbordaje.DesdeTextoPlano("2222"));

        var ruta = new RutaViaje(
            Ahora.AddHours(1),
            "Origen",
            "Destino",
            new[]
            {
                new PuntoRuta("Origen", -12.00, -77.00, "ORIGEN"),
                new PuntoRuta(primero, -12.05, -77.02, "PARADA", r1.Valor),
                new PuntoRuta(segundo, -12.08, -77.05, "PARADA", r2.Valor),
                new PuntoRuta("Destino", -12.12, -77.03, "DESTINO"),
            },
            EsPrueba: true);

        _viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { p1, p2 }, Ahora, ruta.AJson());
        _conductor = _viaje.ConductorId;
        _viaje.RegistrarAbordaje(_conductor, r1, "1111", MetodoAbordaje.Pin, Ahora);
        _viaje.Iniciar(_conductor, Ahora);
    }

    // ---------- Cuando ----------

    [When("el conductor valida el abordaje con el código {string}")]
    public void CuandoElConductorValidaElAbordaje(string codigo) =>
        _ultimoResultado = _viaje.RegistrarAbordaje(_conductor, _reserva, codigo, MetodoAbordaje.Pin, Ahora);

    [When("el conductor valida el abordaje con el código {string} {int} veces")]
    public void CuandoElConductorValidaVariasVeces(string codigo, int veces)
    {
        for (var i = 0; i < veces; i++)
            _ultimoResultado = _viaje.RegistrarAbordaje(_conductor, _reserva, codigo, MetodoAbordaje.Pin, Ahora);
    }

    [When("el pasajero aborda con el código {string}")]
    public void CuandoElPasajeroAborda(string codigo) =>
        _ultimoResultado = _viaje.RegistrarAbordaje(_conductor, _reserva, codigo, MetodoAbordaje.Pin, Ahora);

    [When("el conductor inicia el viaje")]
    public void CuandoElConductorInicia() => _viaje.Iniciar(_conductor, Ahora);

    [When("el conductor finaliza el viaje")]
    [Given("el conductor finaliza el viaje")]
    public void CuandoElConductorFinaliza() => _viaje.Finalizar(_conductor, Ahora.AddMinutes(45));

    [When("se cancela la parada de {string} por motivo {string}")]
    public void CuandoSeCancelaLaParada(string nombre, string motivo) =>
        _viaje.CancelarParada(_reservasPorNombre[nombre], motivo, Ahora.AddMinutes(10), nombre);

    [When("se intenta cancelar la parada de {string} por motivo {string}")]
    public void CuandoSeIntentaCancelarLaParada(string nombre, string motivo)
    {
        try { _viaje.CancelarParada(_reservasPorNombre[nombre], motivo, Ahora.AddMinutes(10), nombre); }
        catch (Exception e) { _ultimoError = e; }
    }

    [When("se intenta cancelar una parada inexistente")]
    public void CuandoSeIntentaCancelarParadaInexistente()
    {
        try { _viaje.CancelarParada(ReservaId.Nuevo(), "no existe", Ahora.AddMinutes(10), "X"); }
        catch (Exception e) { _ultimoError = e; }
    }

    // ---------- Entonces ----------

    [Then("el resultado del abordaje es {string}")]
    public void EntoncesElResultadoEs(string esperado) =>
        Assert.Equal(Enum.Parse<ResultadoAbordaje>(esperado), _ultimoResultado);

    [Then("el pasajero figura como abordado")]
    public void EntoncesElPasajeroFiguraAbordado() =>
        Assert.True(_viaje.Abordajes.Single().EstaAbordado);

    [Then("el pasajero no figura como abordado")]
    public void EntoncesElPasajeroNoFiguraAbordado() =>
        Assert.False(_viaje.Abordajes.Single().EstaAbordado);

    [Then("el pasajero queda bloqueado")]
    public void EntoncesElPasajeroQuedaBloqueado() =>
        Assert.True(_viaje.Abordajes.Single().EstaBloqueado);

    [Then("el estado del viaje es {string}")]
    public void EntoncesElEstadoEs(string esperado) =>
        Assert.Equal(Enum.Parse<EstadoViaje>(esperado), _viaje.Estado);

    [Then("la ruta ya no contiene la parada de {string}")]
    public void EntoncesLaRutaYaNoContiene(string nombre) =>
        Assert.DoesNotContain(RutaActual().Puntos, p => p.Tipo == "PARADA" && p.ReservaId == _reservasPorNombre[nombre].Valor);

    [Then("la ruta todavía contiene la parada de {string}")]
    public void EntoncesLaRutaTodaviaContiene(string nombre) =>
        Assert.Contains(RutaActual().Puntos, p => p.Tipo == "PARADA" && p.ReservaId == _reservasPorNombre[nombre].Valor);

    [Then("la operación falla con una transición inválida")]
    public void EntoncesFallaConTransicionInvalida() =>
        Assert.IsType<TransicionDeEstadoInvalidaException>(_ultimoError);

    [Then("la operación falla porque el pasajero no pertenece al viaje")]
    public void EntoncesFallaPorPasajeroInexistente() =>
        Assert.IsType<PasajeroNoPerteneceAlViajeException>(_ultimoError);

    private RutaViaje RutaActual() =>
        RutaViaje.DesdeJson(_viaje.RutaJson) ?? throw new InvalidOperationException("El viaje no tiene ruta persistida.");
}
