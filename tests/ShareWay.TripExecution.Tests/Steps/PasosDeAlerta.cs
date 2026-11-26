using Reqnroll;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Domain.Viajes.Eventos;

namespace ShareWay.TripExecution.Tests.Steps;

[Binding]
public sealed class PasosDeAlerta
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    private AlertaEmergencia _alerta = default!;
    private Exception? _ultimoError;

    [Given("una alerta de emergencia reportada por el conductor con tipo {string} y descripción {string}")]
    public void DadaAlertaDelConductor(string tipo, string descripcion)
    {
        _alerta = AlertaEmergencia.Registrar(
            new ViajeId(Guid.NewGuid()), Guid.NewGuid(), "DRIVER", tipo, descripcion, Ahora);
    }

    [Given("la alerta ya fue atendida por un administrador")]
    public void DadaAlertaYaAtendida() => _alerta.Atender(Guid.NewGuid(), Ahora.AddMinutes(5));

    [When("un administrador atiende la alerta")]
    public void CuandoAdminAtiende() => _alerta.Atender(Guid.NewGuid(), Ahora.AddMinutes(10));

    [When("se intenta atender la alerta nuevamente")]
    public void CuandoSeIntentaAtender()
    {
        try { _alerta.Atender(Guid.NewGuid(), Ahora.AddMinutes(20)); }
        catch (Exception e) { _ultimoError = e; }
    }

    [Then("la alerta queda en estado {string}")]
    public void EntoncesEstado(string estado) =>
        Assert.Equal(estado, _alerta.Estado.ToString());

    [Then("la alerta levanta un evento {string}")]
    public void EntoncesEvento(string tipoEvento) =>
        Assert.Contains(_alerta.EventosDeDominio, e => e.GetType().Name == tipoEvento);

    [Then("la operación falla con una regla de negocio")]
    public void EntoncesFallaConReglaDeNegocio() =>
        Assert.IsType<ReglaDeNegocioException>(_ultimoError);
}
