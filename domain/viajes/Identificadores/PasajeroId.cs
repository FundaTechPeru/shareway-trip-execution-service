namespace ShareWay.TripExecution.Domain.Identificadores;

public readonly record struct PasajeroId(Guid Valor)
{
    public static PasajeroId Nuevo() => new(Guid.NewGuid());

    public override string ToString() => Valor.ToString();
}
