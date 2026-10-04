namespace ShareWay.TripExecution.Domain.Identificadores;

public readonly record struct ReservaId(Guid Valor)
{
    public static ReservaId Nuevo() => new(Guid.NewGuid());

    public override string ToString() => Valor.ToString();
}
