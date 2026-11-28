using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ShareWay.TripExecution.Infrastructure.Mensajeria;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Outbox;
using ShareWay.TripExecution.Infrastructure.Persistencia;

namespace ShareWay.TripExecution.Tests.Integracion;

/// <summary>
/// Levanta la API en memoria: base EF InMemory, sin procesador de outbox y con
/// <see cref="BrokerEnLog"/> en lugar del broker HTTP hacia Booking.
/// </summary>
public sealed class FabricaDeApi : WebApplicationFactory<Program>
{
    public const string ClaveDePrueba = "clave-interna-de-prueba";
    private readonly string _baseDeDatos = $"trip-execution-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Un entorno distinto de Development evita EnsureCreated y los scripts SQL (no aplican a InMemory).
        builder.UseEnvironment("Testing");
        builder.UseSetting("INTERNAL_API_KEY", ClaveDePrueba);
        builder.UseSetting("JWT_SECRET", "clave-jwt-de-prueba-con-longitud-suficiente-para-hs256");
        builder.UseSetting("ConnectionStrings:TripExecution", "Host=localhost;Database=no-se-usa");

        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<TripExecutionDbContext>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<TripExecutionDbContext>>();
            servicios.AddDbContext<TripExecutionDbContext>(o => o.UseInMemoryDatabase(_baseDeDatos));

            var procesador = servicios.Single(d => d.ImplementationType == typeof(ProcesadorOutbox));
            servicios.Remove(procesador);

            servicios.RemoveAll<IBrokerDeMensajes>();
            servicios.AddSingleton<IBrokerDeMensajes, BrokerEnLog>();
        });
    }
}
