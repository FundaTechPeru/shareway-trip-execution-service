using System.Security.Cryptography;
using System.Text;

namespace ShareWay.TripExecution.Infrastructure.Seguridad;

/// <summary>
/// Clave compartida entre los servicios de ShareWay para las llamadas internas
/// (encabezado <c>X-Internal-Api-Key</c>). Se lee de la variable <c>INTERNAL_API_KEY</c>.
/// </summary>
public sealed class ClaveInterna
{
    public const string Encabezado = "X-Internal-Api-Key";
    public const string VariableDeEntorno = "INTERNAL_API_KEY";
    /// <summary>Solo para desarrollo local, cuando <c>INTERNAL_API_KEY</c> no está configurada.</summary>
    public const string ValorDeDesarrollo = "dev-only-shareway-internal-api-key";

    private readonly byte[] _huella;

    public ClaveInterna(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("La clave interna no puede estar vacía.", nameof(valor));
        Valor = valor;
        _huella = Huella(valor);
    }

    public string Valor { get; }
    public bool EsDeDesarrollo => Valor == ValorDeDesarrollo;

    /// <summary>
    /// Comparación en tiempo constante. Se comparan los SHA-256 de ambos valores para que la
    /// duración tampoco dependa de la longitud de la clave recibida.
    /// </summary>
    public bool Coincide(string? candidata) =>
        candidata is not null && CryptographicOperations.FixedTimeEquals(Huella(candidata), _huella);

    private static byte[] Huella(string valor) => SHA256.HashData(Encoding.UTF8.GetBytes(valor));
}
