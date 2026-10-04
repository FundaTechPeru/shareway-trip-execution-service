namespace ShareWay.TripExecution.Application.Abstracciones;

public interface IUnidadDeTrabajo
{
    Task<int> GuardarCambiosAsync(CancellationToken ct);
}
