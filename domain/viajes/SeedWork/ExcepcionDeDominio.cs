namespace ShareWay.TripExecution.Domain.SeedWork;

public abstract class ExcepcionDeDominio(string mensaje) : Exception(mensaje);
public sealed class ReglaDeNegocioException(string mensaje) : ExcepcionDeDominio(mensaje);
