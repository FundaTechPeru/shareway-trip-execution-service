namespace ShareWay.TripExecution.Domain.Identificadores;

public readonly record struct AlertaEmergenciaId(Guid Valor)
{
    public static AlertaEmergenciaId Nuevo() => new(Guid.NewGuid());
}
