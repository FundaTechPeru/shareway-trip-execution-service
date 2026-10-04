using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.ValidarAbordaje;

public sealed record ValidarAbordajeComando(Guid ViajeId, Guid ConductorId, Guid ReservaId, string Codigo, MetodoAbordaje Metodo);
public sealed record ValidarAbordajeRespuesta(ResultadoAbordaje Resultado, int IntentosFallidos, int IntentosRestantes);
