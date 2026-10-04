using System.Security.Cryptography;
using System.Text;

namespace ShareWay.TripExecution.Domain.Viajes;

public sealed record CodigoAbordaje
{
    public string Hash { get; }
    private CodigoAbordaje(string hash) => Hash = hash;

    public static CodigoAbordaje DesdeHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("El hash del código de abordaje es obligatorio.", nameof(hash));
        return new CodigoAbordaje(hash.Trim().ToLowerInvariant());
    }

    public static CodigoAbordaje DesdeTextoPlano(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código es obligatorio.", nameof(codigo));
        return new CodigoAbordaje(Calcular(codigo));
    }

    public bool Coincide(string? codigoPlano)
    {
        if (string.IsNullOrWhiteSpace(codigoPlano)) return false;
        var candidato = Encoding.UTF8.GetBytes(Calcular(codigoPlano));
        var esperado = Encoding.UTF8.GetBytes(Hash);
        return CryptographicOperations.FixedTimeEquals(candidato, esperado);
    }
    private static string Calcular(string texto) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(texto.Trim())));
}
