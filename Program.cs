using System.Text.Json.Serialization;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Viajes.Comandos.AtenderAlerta;
using ShareWay.TripExecution.Application.Viajes.Comandos.CancelarParada;
using ShareWay.TripExecution.Application.Viajes.Comandos.CancelarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.FinalizarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.IniciarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.OptimizarRuta;
using ShareWay.TripExecution.Application.Viajes.Comandos.RegistrarAlerta;
using ShareWay.TripExecution.Application.Viajes.Comandos.ValidarAbordaje;
using ShareWay.TripExecution.Application.Viajes.Consultas.ListarAlertas;
using ShareWay.TripExecution.Application.Viajes.Consultas.ListarViajesAdmin;
using ShareWay.TripExecution.Application.Viajes.Consultas.ListarViajesDelConductor;
using ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;
using ShareWay.TripExecution.Infrastructure.Mensajeria;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Consumidores;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Outbox;
using ShareWay.TripExecution.Infrastructure.Persistencia;
using ShareWay.TripExecution.Infrastructure.Persistencia.Repositorios;
using ShareWay.TripExecution.Api.Middleware;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Render usa las claves estándar Jwt__Key, Jwt__Issuer y Jwt__Audience.
// JWT_SECRET se mantiene como override (tiene prioridad), según la guía de rotación.
var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    jwtSecret = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("Falta configurar JWT_SECRET o Jwt:Key para validar las sesiones del Gateway.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
            ValidIssuer = jwtIssuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var subject = context.Principal?.FindFirstValue("sub");
                var role = context.Principal?.FindFirstValue("role");
                if (!Guid.TryParse(subject, out _) || role is not ("ADMIN" or "DRIVER" or "PASSENGER"))
                    context.Fail("El token no contiene una identidad ShareWay válida.");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("DriverOnly", policy => policy.RequireRole("DRIVER"));
    options.AddPolicy("DriverOrAdmin", policy => policy.RequireRole("DRIVER", "ADMIN"));
    // Las alertas de emergencia las abre el conductor del viaje o un pasajero abordado.
    options.AddPolicy("DriverOrPassenger", policy => policy.RequireRole("DRIVER", "PASSENGER"));
});

    // Capa aplicacion:
    builder.Services.AddScoped<IManejadorDeComando<CrearViajeComando, Guid>, CrearViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<IniciarViajeComando>, IniciarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<ValidarAbordajeComando, ValidarAbordajeRespuesta>, ValidarAbordajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<FinalizarViajeComando>, FinalizarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<CancelarViajeComando>, CancelarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<CancelarParadaComando>, CancelarParadaManejador>();
    builder.Services.AddScoped<IManejadorDeComando<OptimizarRutaComando>, OptimizarRutaManejador>();
    builder.Services.AddScoped<IManejadorDeComando<RegistrarAlertaComando, Guid>, RegistrarAlertaManejador>();
    builder.Services.AddScoped<IManejadorDeComando<AtenderAlertaComando>, AtenderAlertaManejador>();
    builder.Services.AddScoped<IManejadorDeConsulta<ObtenerViajeConsulta, ViajeDto>, ObtenerViajeManejador>();
    builder.Services.AddScoped<IManejadorDeConsulta<ListarViajesDelConductorConsulta, IReadOnlyList<ViajeDto>>, ListarViajesDelConductorManejador>();
    builder.Services.AddScoped<IManejadorDeConsulta<ListarViajesAdminConsulta, IReadOnlyList<ViajeDto>>, ListarViajesAdminManejador>();
    builder.Services.AddScoped<IManejadorDeConsulta<ListarAlertasConsulta, PaginaAlertasDto>, ListarAlertasManejador>();

    // Capa infraestructura:
    var conectionString = NormalizarCadenaDeConexion(
        builder.Configuration.GetConnectionString("TripExecution") ?? throw new InvalidOperationException("Falta ConnectionStrings:TripExecution."));
    builder.Services.AddDbContext<TripExecutionDbContext>(o => o.UseNpgsql(conectionString));
    builder.Services.AddScoped<IViajeRepositorio, ViajeRepositorio>();
    builder.Services.AddScoped<IAlertaEmergenciaRepositorio, AlertaEmergenciaRepositorio>();
    builder.Services.AddScoped<IUnidadDeTrabajo>(sp => sp.GetRequiredService<TripExecutionDbContext>());
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<IBrokerDeMensajes, BrokerEnLog>();
    builder.Services.AddScoped<GrupoConfirmadoConsumidor>();
    builder.Services.AddHostedService<ProcesadorOutbox>();

    // Servicios
    builder.Services.AddExceptionHandler<ManejadorGlobalDeExcepciones>();
    builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));;
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();
    builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.MapOpenApi();
    app.UseSwaggerUI( options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
    });
}

// Para entornos de prueba: crea esquema y tablas si no existen (no hay migraciones desplegadas).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:AutoCreate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TripExecutionDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    // EnsureCreated no altera una base existente: los cambios incrementales viven en
    // database/migrations/*.sql (embebidos e idempotentes) y se aplican en cada arranque.
    // Si alguno falla, la excepción se propaga y el servicio no arranca.
    var ensamblado = typeof(Program).Assembly;
    var scripts = ensamblado.GetManifestResourceNames()
        .Where(n => n.StartsWith("migrations/", StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.Ordinal))
        .Order(StringComparer.Ordinal);
    foreach (var script in scripts)
    {
        await using var stream = ensamblado.GetManifestResourceStream(script)
            ?? throw new InvalidOperationException($"No se encontró el script embebido {script}.");
        using var lector = new StreamReader(stream);
        var sql = await lector.ReadToEndAsync();
        await dbContext.Database.ExecuteSqlRawAsync(sql);
        app.Logger.LogInformation("Script de migración aplicado: {Script}", script);
    }
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));
app.MapControllers();
app.Run();

static string NormalizarCadenaDeConexion(string cadena)
{
    if (!cadena.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !cadena.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return cadena;

    var uri = new Uri(cadena);
    var credenciales = uri.UserInfo.Split(':', 2);
    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(credenciales[0]),
        Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : null
    }.ConnectionString;
}

public partial class Program;
