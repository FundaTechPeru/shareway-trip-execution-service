using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShareWay.TripExecution.Infrastructure.Persistencia;

namespace ShareWay.TripExecution.Tests.Integracion;

public sealed class GruposConfirmadosEndpointTests : IClassFixture<FabricaDeApi>
{
    private const string Ruta = "/api/internal/grupos-confirmados";
    private readonly FabricaDeApi _fabrica;

    public GruposConfirmadosEndpointTests(FabricaDeApi fabrica) => _fabrica = fabrica;

    private static string Sha256(string texto) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));

    private static object Cuerpo(Guid grupoId, string? pinHash = null) => new
    {
        idEvento = Guid.NewGuid(),
        grupoId,
        conductorId = Guid.NewGuid(),
        pasajeros = new[]
        {
            new { reservaId = Guid.NewGuid(), pasajeroId = Guid.NewGuid(), codigoHash = Sha256("token-qr"), pinHash = pinHash ?? Sha256("482913") }
        }
    };

    private HttpClient Cliente(string? clave = FabricaDeApi.ClaveDePrueba)
    {
        var cliente = _fabrica.CreateClient();
        if (clave is not null)
            cliente.DefaultRequestHeaders.Add("X-Internal-Api-Key", clave);
        return cliente;
    }

    private static async Task<Guid> LeerIdAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task GrupoNuevo_Devuelve201ConElIdDelViaje_YGuardaElPinHash()
    {
        var grupoId = Guid.NewGuid();

        var respuesta = await Cliente().PostAsJsonAsync(Ruta, Cuerpo(grupoId));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var id = await LeerIdAsync(respuesta);

        await using var scope = _fabrica.Services.CreateAsyncScope();
        var contexto = scope.ServiceProvider.GetRequiredService<TripExecutionDbContext>();
        var viaje = await contexto.Viajes.Include(v => v.Abordajes).SingleAsync(v => v.Id == new Domain.Identificadores.ViajeId(id));
        Assert.Equal(grupoId, viaje.GrupoId.Valor);
        var abordaje = Assert.Single(viaje.Abordajes);
        Assert.Equal(Sha256("482913"), abordaje.Pin!.Hash);
        Assert.Equal(Sha256("token-qr"), abordaje.Codigo.Hash);
    }

    [Fact]
    public async Task GrupoRepetido_Devuelve200ConElMismoId()
    {
        var grupoId = Guid.NewGuid();
        var cliente = Cliente();

        var primera = await cliente.PostAsJsonAsync(Ruta, Cuerpo(grupoId));
        var segunda = await cliente.PostAsJsonAsync(Ruta, Cuerpo(grupoId));

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(await LeerIdAsync(primera), await LeerIdAsync(segunda));
    }

    [Fact]
    public async Task SinClave_Devuelve401()
    {
        var respuesta = await Cliente(clave: null).PostAsJsonAsync(Ruta, Cuerpo(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task ConClaveIncorrecta_Devuelve401()
    {
        var respuesta = await Cliente(clave: "otra-clave").PostAsJsonAsync(Ruta, Cuerpo(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task ConJwtPeroSinClave_Devuelve401()
    {
        var cliente = Cliente(clave: null);
        cliente.DefaultRequestHeaders.Authorization = new("Bearer", "cualquier-token");

        var respuesta = await cliente.PostAsJsonAsync(Ruta, Cuerpo(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task HashMalFormado_Devuelve400()
    {
        var respuesta = await Cliente().PostAsJsonAsync(Ruta, Cuerpo(Guid.NewGuid(), pinHash: "482913"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task NoSeExponeBajoApiViajes()
    {
        var respuesta = await Cliente().PostAsJsonAsync("/api/viajes/internal/grupos-confirmados", Cuerpo(Guid.NewGuid()));

        Assert.NotEqual(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
