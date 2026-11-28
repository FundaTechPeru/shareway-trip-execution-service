using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Tests.Dominio;

/// <summary>El método de abordaje decide contra qué hash se compara: QR → codigoHash, PIN → pinHash.</summary>
public sealed class AbordajePorMetodoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);
    private const string TokenQr = "qr-7f3a9c";
    private const string Pin = "482913";

    private static (Viaje Viaje, ReservaId Reserva) ViajeConPin(bool conPin = true)
    {
        var reserva = ReservaId.Nuevo();
        var pasajero = new PasajeroConfirmado(
            reserva, PasajeroId.Nuevo(),
            CodigoAbordaje.DesdeTextoPlano(TokenQr),
            conPin ? CodigoAbordaje.DesdeTextoPlano(Pin) : null);
        return (Viaje.Crear(GrupoId.Nuevo(), ConductorId.Nuevo(), [pasajero], Ahora), reserva);
    }

    [Fact]
    public void Pin_ConElPinCorrecto_Aborda()
    {
        var (viaje, reserva) = ViajeConPin();

        var resultado = viaje.RegistrarAbordaje(viaje.ConductorId, reserva, Pin, MetodoAbordaje.Pin, Ahora);

        Assert.Equal(ResultadoAbordaje.Abordado, resultado);
        Assert.Equal(MetodoAbordaje.Pin, viaje.Abordajes.Single().Metodo);
    }

    [Fact]
    public void Qr_ConElTokenCorrecto_Aborda()
    {
        var (viaje, reserva) = ViajeConPin();

        var resultado = viaje.RegistrarAbordaje(viaje.ConductorId, reserva, TokenQr, MetodoAbordaje.Qr, Ahora);

        Assert.Equal(ResultadoAbordaje.Abordado, resultado);
        Assert.Equal(MetodoAbordaje.Qr, viaje.Abordajes.Single().Metodo);
    }

    [Fact]
    public void Pin_ConElTokenQr_EsInvalido()
    {
        var (viaje, reserva) = ViajeConPin();

        var resultado = viaje.RegistrarAbordaje(viaje.ConductorId, reserva, TokenQr, MetodoAbordaje.Pin, Ahora);

        Assert.Equal(ResultadoAbordaje.CodigoInvalido, resultado);
        Assert.Equal(1, viaje.Abordajes.Single().IntentosFallidos);
    }

    [Fact]
    public void Qr_ConElPin_EsInvalido()
    {
        var (viaje, reserva) = ViajeConPin();

        var resultado = viaje.RegistrarAbordaje(viaje.ConductorId, reserva, Pin, MetodoAbordaje.Qr, Ahora);

        Assert.Equal(ResultadoAbordaje.CodigoInvalido, resultado);
    }

    [Fact]
    public void Pin_EnViajeSinPinHash_SeComparaContraElCodigo()
    {
        var (viaje, reserva) = ViajeConPin(conPin: false);

        Assert.Equal(ResultadoAbordaje.CodigoInvalido,
            viaje.RegistrarAbordaje(viaje.ConductorId, reserva, Pin, MetodoAbordaje.Pin, Ahora));
        Assert.Equal(ResultadoAbordaje.Abordado,
            viaje.RegistrarAbordaje(viaje.ConductorId, reserva, TokenQr, MetodoAbordaje.Pin, Ahora));
    }

    [Fact]
    public void PinHash_SeNormalizaAMinusculas()
    {
        var hash = CodigoAbordaje.DesdeTextoPlano(Pin).Hash.ToUpperInvariant();

        var codigo = CodigoAbordaje.DesdeHash(hash);

        Assert.True(codigo.Coincide(Pin));
    }
}
