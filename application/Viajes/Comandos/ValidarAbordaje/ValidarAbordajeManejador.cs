using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.ValidarAbordaje;

public sealed class ValidarAbordajeManejador(IViajeRepositorio viajes, IUnidadDeTrabajo udt, TimeProvider reloj) : IManejadorDeComando<ValidarAbordajeComando, ValidarAbordajeRespuesta>
{
    public async Task<ValidarAbordajeRespuesta> ManejarAsync(ValidarAbordajeComando comando, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(comando.Codigo))
            throw new ArgumentException("El código de abordaje es obligatorio.", nameof(comando));

        var viaje = await viajes.ObtenerOFallarAsync(comando.ViajeId, ct);
        var reservaId = new ReservaId(comando.ReservaId);
        var resultado = viaje.RegistrarAbordaje(new ConductorId(comando.ConductorId), reservaId, comando.Codigo, comando.Metodo, reloj.GetUtcNow());

        await udt.GuardarCambiosAsync(ct);

        var abordaje = viaje.Abordajes.First(a => a.Id == reservaId);
        var restantes = Math.Max(0, PoliticaDeAbordaje.MaximoIntentosFallidos - abordaje.IntentosFallidos);
        return new ValidarAbordajeRespuesta(resultado, abordaje.IntentosFallidos, restantes);
    }
}
