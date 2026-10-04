namespace ShareWay.TripExecution.Domain.Identificadores;

public readonly record struct ConductorId(Guid Valor)
{
    public static ConductorId Nuevo() => new(Guid.NewGuid());

    public override string ToString() => Valor.ToString();
}
