using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Domain.Viajes.Eventos;

namespace ShareWay.TripExecution.Tests.Dominio;

public sealed class DistanciaDeRutaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);

    private static RutaViaje Ruta(params PuntoRuta[] puntos) => new(Ahora, "A", "B", puntos, EsPrueba: true);

    [Fact]
    public void UnGradoDeLatitud_MideUnos111Km()
    {
        var ruta = Ruta(new("A", 0, 0, "ORIGEN"), new("B", 1, 0, "DESTINO"));

        // 2π · 6371.0088 / 360 = 111.195 km
        Assert.Equal(111.195, ruta.DistanciaTotalKm(), precision: 3);
    }

    [Fact]
    public void SumaLosTramosConsecutivos()
    {
        var ruta = Ruta(
            new("A", 0, 0, "ORIGEN"),
            new("P", 1, 0, "PARADA"),
            new("B", 2, 0, "DESTINO"));

        Assert.Equal(2 * 111.195, ruta.DistanciaTotalKm(), precision: 3);
    }

    [Fact]
    public void RutaDeUnSoloPunto_MideCero()
    {
        Assert.Equal(0, Ruta(new PuntoRuta("A", -12.088, -77.043, "ORIGEN")).DistanciaTotalKm());
    }

    private static ViajeFinalizado Finalizar(string? rutaJson)
    {
        var reserva = ReservaId.Nuevo();
        var viaje = Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(),
            [new PasajeroConfirmado(reserva, PasajeroId.Nuevo(), CodigoAbordaje.DesdeTextoPlano("1234"))], Ahora, rutaJson);
        viaje.RegistrarAbordaje(viaje.ConductorId, reserva, "1234", MetodoAbordaje.Qr, Ahora);
        viaje.Iniciar(viaje.ConductorId, Ahora);
        viaje.Finalizar(viaje.ConductorId, Ahora.AddMinutes(20));
        return viaje.EventosDeDominio.OfType<ViajeFinalizado>().Single();
    }

    [Fact]
    public void ViajeFinalizado_ConRuta_LlevaLaDistanciaRedondeadaADosDecimales()
    {
        // Lince → San Isidro (los puntos del README).
        var ruta = Ruta(
            new("Lince", -12.088, -77.043, "ORIGEN"),
            new("San Isidro", -12.105, -77.023, "DESTINO"));

        var evento = Finalizar(ruta.AJson());

        Assert.NotNull(evento.DistanciaKm);
        Assert.Equal(Math.Round(ruta.DistanciaTotalKm(), 2), evento.DistanciaKm);
        Assert.Equal(evento.DistanciaKm, Math.Round(evento.DistanciaKm!.Value, 2));
        Assert.InRange(evento.DistanciaKm.Value, 2.8, 2.9);
    }

    [Fact]
    public void ViajeFinalizado_SinRuta_LlevaDistanciaNula()
    {
        Assert.Null(Finalizar(rutaJson: null).DistanciaKm);
    }
}
