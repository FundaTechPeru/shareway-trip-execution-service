using System.Text.Json.Serialization;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Viajes.Comandos.CancelarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.FinalizarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.IniciarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.ValidarAbordaje;
using ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;
using ShareWay.TripExecution.Application.Viajes.Consultas.ListarViajesDelConductor;
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
                if (!Guid.TryParse(subject, out _) || role is not ("ADMIN" or "DRIVER"))
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
});

    // Capa aplicacion:
    builder.Services.AddScoped<IManejadorDeComando<CrearViajeComando, Guid>, CrearViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<IniciarViajeComando>, IniciarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<ValidarAbordajeComando, ValidarAbordajeRespuesta>, ValidarAbordajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<FinalizarViajeComando>, FinalizarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<CancelarViajeComando>, CancelarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeConsulta<ObtenerViajeConsulta, ViajeDto>, ObtenerViajeManejador>();
    builder.Services.AddScoped<IManejadorDeConsulta<ListarViajesDelConductorConsulta, IReadOnlyList<ViajeDto>>, ListarViajesDelConductorManejador>();

    // Capa infraestructura:
    var conectionString = NormalizarCadenaDeConexion(
        builder.Configuration.GetConnectionString("TripExecution") ?? throw new InvalidOperationException("Falta ConnectionStrings:TripExecution."));
    builder.Services.AddDbContext<TripExecutionDbContext>(o => o.UseNpgsql(conectionString));
    builder.Services.AddScoped<IViajeRepositorio, ViajeRepositorio>();
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
    await scope.ServiceProvider.GetRequiredService<TripExecutionDbContext>().Database.EnsureCreatedAsync();
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
