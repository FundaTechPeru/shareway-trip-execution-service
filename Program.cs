using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.Viajes.Comandos.CancelarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.FinalizarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.IniciarViaje;
using ShareWay.TripExecution.Application.Viajes.Comandos.ValidarAbordaje;
using ShareWay.TripExecution.Application.Viajes.Consultas.ObtenerViaje;
using ShareWay.TripExecution.Infrastructure.Mensajeria;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Consumidores;
using ShareWay.TripExecution.Infrastructure.Mensajeria.Outbox;
using ShareWay.TripExecution.Infrastructure.Persistencia;
using ShareWay.TripExecution.Infrastructure.Persistencia.Repositorios;
using ShareWay.TripExecution.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
    // Capa aplicacion:
    builder.Services.AddScoped<IManejadorDeComando<CrearViajeComando, Guid>, CrearViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<IniciarViajeComando>, IniciarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<ValidarAbordajeComando, ValidarAbordajeRespuesta>, ValidarAbordajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<FinalizarViajeComando>, FinalizarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeComando<CancelarViajeComando>, CancelarViajeManejador>();
    builder.Services.AddScoped<IManejadorDeConsulta<ObtenerViajeConsulta, ViajeDto>, ObtenerViajeManejador>();

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
app.UseAuthorization();
app.MapControllers();
app.Run();
