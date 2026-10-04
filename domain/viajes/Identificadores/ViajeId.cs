namespace ShareWay.TripExecution.Domain.Identificadores;

public readonly record struct ViajeId(Guid Valor)
{
    public static ViajeId Nuevo() => new(Guid.NewGuid());

    public override string ToString() => Valor.ToString();
}
