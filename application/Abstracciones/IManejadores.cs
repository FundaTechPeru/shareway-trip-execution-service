namespace ShareWay.TripExecution.Application.Abstracciones;

public interface IManejadorDeComando<in TComando>
{
    Task ManejarAsync(TComando comando, CancellationToken ct);
}

public interface IManejadorDeComando<in TComando, TRespuesta>
{
    Task<TRespuesta> ManejarAsync(TComando comando, CancellationToken ct);
}

public interface IManejadorDeConsulta<in TConsulta, TRespuesta>
{
    Task<TRespuesta> ManejarAsync(TConsulta consulta, CancellationToken ct);
}
