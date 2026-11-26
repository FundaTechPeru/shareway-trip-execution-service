using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Domain.Viajes.Eventos;
using ShareWay.TripExecution.Domain.Viajes.Excepciones;

namespace ShareWay.TripExecution.Tests.Dominio;

/// <summary>Pruebas unitarias del agregado <see cref="Viaje"/>: ciclo de vida e invariantes.</summary>
public sealed class ViajeTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static PasajeroConfirmado Pasajero(string codigo = "1234", ReservaId? reserva = null) =>
        new(reserva ?? ReservaId.Nuevo(), PasajeroId.Nuevo(), CodigoAbordaje.DesdeTextoPlano(codigo));

    private static Viaje ViajeConfirmado(params PasajeroConfirmado[] pasajeros)
    {
        var lista = pasajeros.Length == 0 ? new[] { Pasajero() } : pasajeros;
        return Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), lista, Ahora);
    }

    private static Viaje ViajeEnCurso(out ConductorId conductor, out ReservaId reserva)
    {
        var pasajero = Pasajero();
        reserva = pasajero.ReservaId;
        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { pasajero }, Ahora);
        conductor = viaje.ConductorId;
        viaje.RegistrarAbordaje(conductor, reserva, "1234", MetodoAbordaje.Pin, Ahora);
        viaje.Iniciar(conductor, Ahora);
        return viaje;
    }

    // ---------- Crear ----------

    [Fact]
    public void Crear_ConPasajeros_QuedaConfirmadoYLevantaViajeCreado()
    {
        var viaje = ViajeConfirmado();

        Assert.Equal(EstadoViaje.Confirmado, viaje.Estado);
        Assert.Single(viaje.Abordajes);
        Assert.Contains(viaje.EventosDeDominio, e => e is ViajeCreado);
    }

    [Fact]
    public void Crear_SinPasajeros_Lanza()
    {
        var ex = Assert.Throws<ReglaDeNegocioException>(() =>
            Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), Array.Empty<PasajeroConfirmado>(), Ahora));

        Assert.Contains("al menos un pasajero", ex.Message);
    }

    [Fact]
    public void Crear_ConReservasDuplicadas_Lanza()
    {
        var reserva = ReservaId.Nuevo();
        var p1 = Pasajero(reserva: reserva);
        var p2 = Pasajero(reserva: reserva);

        var ex = Assert.Throws<ReglaDeNegocioException>(() =>
            Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { p1, p2 }, Ahora));

        Assert.Contains("duplicadas", ex.Message);
    }

    // ---------- Iniciar ----------

    [Fact]
    public void Iniciar_SinPasajerosAbordados_Lanza()
    {
        var viaje = ViajeConfirmado();

        Assert.Throws<ReglaDeNegocioException>(() => viaje.Iniciar(viaje.ConductorId, Ahora));
        Assert.Equal(EstadoViaje.Confirmado, viaje.Estado);
    }

    [Fact]
    public void Iniciar_ConUnAbordado_PasaAEnCursoYLevantaEvento()
    {
        var pasajero = Pasajero();
        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { pasajero }, Ahora);
        viaje.RegistrarAbordaje(viaje.ConductorId, pasajero.ReservaId, "1234", MetodoAbordaje.Pin, Ahora);

        viaje.Iniciar(viaje.ConductorId, Ahora);

        Assert.Equal(EstadoViaje.EnCurso, viaje.Estado);
        Assert.Equal(Ahora, viaje.InicioReal);
        Assert.Contains(viaje.EventosDeDominio, e => e is ViajeIniciado);
    }

    [Fact]
    public void Iniciar_ConConductorNoAsignado_Lanza()
    {
        var pasajero = Pasajero();
        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { pasajero }, Ahora);
        viaje.RegistrarAbordaje(viaje.ConductorId, pasajero.ReservaId, "1234", MetodoAbordaje.Pin, Ahora);

        Assert.Throws<ConductorNoAsignadoException>(() => viaje.Iniciar(ConductorId.Nuevo(), Ahora));
    }

    [Fact]
    public void Iniciar_DosVeces_Lanza()
    {
        var viaje = ViajeEnCurso(out var conductor, out _);

        Assert.Throws<TransicionDeEstadoInvalidaException>(() => viaje.Iniciar(conductor, Ahora));
    }

    // ---------- Finalizar ----------

    [Fact]
    public void Finalizar_DesdeEnCurso_PasaAFinalizadoYLevantaEvento()
    {
        var viaje = ViajeEnCurso(out var conductor, out _);

        viaje.Finalizar(conductor, Ahora.AddMinutes(30));

        Assert.Equal(EstadoViaje.Finalizado, viaje.Estado);
        Assert.Equal(Ahora.AddMinutes(30), viaje.FinReal);
        Assert.Contains(viaje.EventosDeDominio, e => e is ViajeFinalizado);
    }

    [Fact]
    public void Finalizar_DesdeConfirmado_Lanza()
    {
        var viaje = ViajeConfirmado();

        Assert.Throws<TransicionDeEstadoInvalidaException>(() => viaje.Finalizar(viaje.ConductorId, Ahora));
    }

    [Fact]
    public void Finalizar_ConConductorNoAsignado_Lanza()
    {
        var viaje = ViajeEnCurso(out _, out _);

        Assert.Throws<ConductorNoAsignadoException>(() => viaje.Finalizar(ConductorId.Nuevo(), Ahora));
    }

    // ---------- Cancelar ----------

    [Fact]
    public void Cancelar_DesdeConfirmado_PasaACanceladoYLevantaEvento()
    {
        var viaje = ViajeConfirmado();

        viaje.Cancelar("El conductor no se presentó", Ahora);

        Assert.Equal(EstadoViaje.Cancelado, viaje.Estado);
        Assert.Contains(viaje.EventosDeDominio, e => e is ViajeCancelado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancelar_SinMotivo_Lanza(string motivo)
    {
        var viaje = ViajeConfirmado();

        Assert.Throws<ReglaDeNegocioException>(() => viaje.Cancelar(motivo, Ahora));
        Assert.Equal(EstadoViaje.Confirmado, viaje.Estado);
    }

    [Fact]
    public void Cancelar_DesdeEnCurso_Lanza()
    {
        var viaje = ViajeEnCurso(out _, out _);

        Assert.Throws<TransicionDeEstadoInvalidaException>(() => viaje.Cancelar("tarde", Ahora));
    }

    // ---------- Abordaje ----------

    [Fact]
    public void RegistrarAbordaje_CodigoCorrecto_DevuelveAbordado()
    {
        var pasajero = Pasajero();
        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { pasajero }, Ahora);

        var resultado = viaje.RegistrarAbordaje(viaje.ConductorId, pasajero.ReservaId, "1234", MetodoAbordaje.Pin, Ahora);

        Assert.Equal(ResultadoAbordaje.Abordado, resultado);
        Assert.Contains(viaje.EventosDeDominio, e => e is PasajeroAbordo);
    }

    [Fact]
    public void RegistrarAbordaje_CodigoIncorrecto_DevuelveCodigoInvalido()
    {
        var pasajero = Pasajero();
        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { pasajero }, Ahora);

        var resultado = viaje.RegistrarAbordaje(viaje.ConductorId, pasajero.ReservaId, "0000", MetodoAbordaje.Pin, Ahora);

        Assert.Equal(ResultadoAbordaje.CodigoInvalido, resultado);
        Assert.Contains(viaje.EventosDeDominio, e => e is AbordajeFallido);
    }

    [Fact]
    public void RegistrarAbordaje_TrasTresFallos_SeBloquea()
    {
        var pasajero = Pasajero();
        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { pasajero }, Ahora);

        for (var i = 0; i < PoliticaDeAbordaje.MaximoIntentosFallidos; i++)
            viaje.RegistrarAbordaje(viaje.ConductorId, pasajero.ReservaId, "0000", MetodoAbordaje.Pin, Ahora);

        // Cuarto intento, incluso con el código correcto, debe quedar bloqueado.
        var resultado = viaje.RegistrarAbordaje(viaje.ConductorId, pasajero.ReservaId, "1234", MetodoAbordaje.Pin, Ahora);

        Assert.Equal(ResultadoAbordaje.Bloqueado, resultado);
        Assert.True(viaje.Abordajes.Single().EstaBloqueado);
    }

    [Fact]
    public void RegistrarAbordaje_ReservaInexistente_Lanza()
    {
        var viaje = ViajeConfirmado();

        Assert.Throws<PasajeroNoPerteneceAlViajeException>(() =>
            viaje.RegistrarAbordaje(viaje.ConductorId, ReservaId.Nuevo(), "1234", MetodoAbordaje.Pin, Ahora));
    }

    // ---------- VehiculoId ----------

    [Fact]
    public void Crear_SinVehiculo_DejaVehiculoIdEnNulo()
    {
        var viaje = ViajeConfirmado();

        Assert.Null(viaje.VehiculoId);
    }

    [Fact]
    public void Crear_ConVehiculo_GuardaVehiculoIdHistorico()
    {
        var vehiculo = new VehiculoId(Guid.NewGuid());
        var viaje = Viaje.Crear(
            GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { Pasajero() }, Ahora, rutaJson: null, vehiculoId: vehiculo);

        Assert.Equal(vehiculo, viaje.VehiculoId);
    }

    // ---------- CancelarParada: casos extra (US-22) ----------

    private static Viaje ViajeEnCursoConRuta(out ReservaId r1, out ReservaId r2, out ReservaId r3)
    {
        r1 = ReservaId.Nuevo();
        r2 = ReservaId.Nuevo();
        r3 = ReservaId.Nuevo();
        var p1 = new PasajeroConfirmado(r1, PasajeroId.Nuevo(), CodigoAbordaje.DesdeTextoPlano("1111"));
        var p2 = new PasajeroConfirmado(r2, PasajeroId.Nuevo(), CodigoAbordaje.DesdeTextoPlano("2222"));
        var p3 = new PasajeroConfirmado(r3, PasajeroId.Nuevo(), CodigoAbordaje.DesdeTextoPlano("3333"));

        var ruta = new RutaViaje(
            Ahora.AddHours(1), "Origen", "Destino",
            new[]
            {
                new PuntoRuta("Origen", -12.00, -77.00, "ORIGEN"),
                new PuntoRuta("Ana",    -12.05, -77.02, "PARADA", r1.Valor),
                new PuntoRuta("Beto",   -12.08, -77.05, "PARADA", r2.Valor),
                new PuntoRuta("Carla",  -12.10, -77.06, "PARADA", r3.Valor),
                new PuntoRuta("Destino",-12.12, -77.03, "DESTINO"),
            }, EsPrueba: true);

        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { p1, p2, p3 }, Ahora, ruta.AJson());
        viaje.RegistrarAbordaje(viaje.ConductorId, r1, "1111", MetodoAbordaje.Pin, Ahora);
        viaje.Iniciar(viaje.ConductorId, Ahora);
        return viaje;
    }

    [Fact]
    public void CancelarParada_ParadaIntermedia_LaQuitaYRecalculaRuta()
    {
        var viaje = ViajeEnCursoConRuta(out _, out var r2, out _);

        viaje.CancelarParada(r2, "cambio de planes", Ahora, "Beto");

        Assert.DoesNotContain(viaje.RutaJson!, "Beto");
        Assert.Equal(EstadoViaje.EnCurso, viaje.Estado);
        Assert.Contains(viaje.EventosDeDominio, e => e is ParadaCancelada);
    }

    [Fact]
    public void CancelarParada_UltimaParadaRestante_LaQuitaYMantieneElViaje()
    {
        var viaje = ViajeEnCursoConRuta(out var r1, out var r2, out var r3);
        viaje.CancelarParada(r2, "cancela Beto", Ahora, "Beto");
        viaje.CancelarParada(r3, "cancela Carla", Ahora, "Carla");

        viaje.CancelarParada(r1, "cancela Ana", Ahora, "Ana");

        Assert.Equal(EstadoViaje.EnCurso, viaje.Estado);
        Assert.DoesNotContain("Ana", viaje.RutaJson!);
        Assert.DoesNotContain("Beto", viaje.RutaJson!);
        Assert.DoesNotContain("Carla", viaje.RutaJson!);
    }

    [Fact]
    public void CancelarParada_EnViajeFinalizado_Lanza()
    {
        var viaje = ViajeEnCursoConRuta(out var r1, out _, out _);
        viaje.Finalizar(viaje.ConductorId, Ahora);

        Assert.Throws<TransicionDeEstadoInvalidaException>(() =>
            viaje.CancelarParada(r1, "fuera de tiempo", Ahora, "Ana"));
    }

    [Fact]
    public void CancelarParada_EnViajeCancelado_Lanza()
    {
        var pasajero = Pasajero();
        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), new[] { pasajero }, Ahora);
        viaje.Cancelar("no se presentó nadie", Ahora);

        Assert.Throws<TransicionDeEstadoInvalidaException>(() =>
            viaje.CancelarParada(pasajero.ReservaId, "no aplica", Ahora, "Ana"));
    }

    [Fact]
    public void CancelarParada_ReservaInexistente_Lanza()
    {
        var viaje = ViajeEnCursoConRuta(out _, out _, out _);

        Assert.Throws<PasajeroNoPerteneceAlViajeException>(() =>
            viaje.CancelarParada(ReservaId.Nuevo(), "no existe", Ahora, "X"));
    }

    [Fact]
    public void CancelarParada_SinMotivo_Lanza()
    {
        var viaje = ViajeEnCursoConRuta(out var r1, out _, out _);

        Assert.Throws<ReglaDeNegocioException>(() => viaje.CancelarParada(r1, "   ", Ahora, "Ana"));
    }
}
