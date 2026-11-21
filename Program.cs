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

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("Falta configurar JWT_SECRET para validar las sesiones del Gateway.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
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
    var conectionString = builder.Configuration.GetConnectionString("TripExecution") ?? throw new InvalidOperationException("Falta ConnectionStrings:TripExecution.");
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI( options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
    });
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<TripExecutionDbContext>().Database.EnsureCreatedAsync();
}

app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
