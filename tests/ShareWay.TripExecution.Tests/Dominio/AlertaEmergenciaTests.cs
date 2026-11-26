using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Domain.Viajes.Eventos;

namespace ShareWay.TripExecution.Tests.Dominio;

/// <summary>Pruebas unitarias del agregado <see cref="AlertaEmergencia"/>.</summary>
public sealed class AlertaEmergenciaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    private static AlertaEmergencia AlertaAbierta(string rol = "DRIVER") =>
        AlertaEmergencia.Registrar(
            new ViajeId(Guid.NewGuid()), Guid.NewGuid(), rol, "ACCIDENTE", "Choque en la Panamericana Sur", Ahora);

    [Fact]
    public void Registrar_CamposCompletos_QuedaAbiertaYLevantaEvento()
    {
        var alerta = AlertaAbierta();

        Assert.Equal(EstadoAlerta.Abierta, alerta.Estado);
        Assert.Equal("ACCIDENTE", alerta.Tipo);
        Assert.Contains(alerta.EventosDeDominio, e => e is AlertaEmergenciaRegistrada);
    }

    [Theory]
    [InlineData("", "ACCIDENTE", "detalle")]
    [InlineData("DRIVER", "", "detalle")]
    [InlineData("DRIVER", "ACCIDENTE", "")]
    public void Registrar_SinCamposObligatorios_Lanza(string rol, string tipo, string descripcion)
    {
        Assert.Throws<ReglaDeNegocioException>(() => AlertaEmergencia.Registrar(
            new ViajeId(Guid.NewGuid()), Guid.NewGuid(), rol, tipo, descripcion, Ahora));
    }

    [Fact]
    public void Registrar_ReportanteVacio_Lanza()
    {
        Assert.Throws<ReglaDeNegocioException>(() => AlertaEmergencia.Registrar(
            new ViajeId(Guid.NewGuid()), Guid.Empty, "DRIVER", "ACCIDENTE", "detalle", Ahora));
    }

    [Fact]
    public void Atender_AlertaAbierta_PasaAAtendidaYLevantaEvento()
    {
        var alerta = AlertaAbierta();
        var admin = Guid.NewGuid();
        var momento = Ahora.AddMinutes(15);

        alerta.Atender(admin, momento);

        Assert.Equal(EstadoAlerta.Atendida, alerta.Estado);
        Assert.Equal(momento, alerta.AtendidaEn);
        Assert.Equal(admin, alerta.AtendidaPor);
        Assert.Contains(alerta.EventosDeDominio, e => e is AlertaEmergenciaAtendida);
    }

    [Fact]
    public void Atender_SinAdministrador_Lanza()
    {
        var alerta = AlertaAbierta();

        Assert.Throws<ReglaDeNegocioException>(() => alerta.Atender(Guid.Empty, Ahora));
    }

    [Fact]
    public void Atender_AlertaYaAtendida_Lanza()
    {
        var alerta = AlertaAbierta();
        alerta.Atender(Guid.NewGuid(), Ahora);

        Assert.Throws<ReglaDeNegocioException>(() => alerta.Atender(Guid.NewGuid(), Ahora.AddMinutes(1)));
    }
}
