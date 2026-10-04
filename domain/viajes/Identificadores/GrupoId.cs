namespace ShareWay.TripExecution.Domain.Identificadores;

public readonly record struct GrupoId(Guid Valor)
{
    public static GrupoId Nuevo() => new(Guid.NewGuid());

    public override string ToString() => Valor.ToString();
}
