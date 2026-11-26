namespace ShareWay.TripExecution.Domain.Identificadores;

public readonly record struct VehiculoId(Guid Valor)
{
    public static VehiculoId? Desde(Guid? valor) => valor is { } v ? new VehiculoId(v) : null;
}
