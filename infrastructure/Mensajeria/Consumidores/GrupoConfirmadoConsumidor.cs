using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Entrantes;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;
using ShareWay.TripExecution.Infrastructure.Persistencia;

namespace ShareWay.TripExecution.Infrastructure.Mensajeria.Consumidores;

public sealed class GrupoConfirmadoConsumidor(
    IManejadorDeComando<CrearViajeComando, CrearViajeResultado> crearViaje,
    TripExecutionDbContext contexto,
    ILogger<GrupoConfirmadoConsumidor> log)
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    public Task<CrearViajeResultado> ProcesarAsync(string json, CancellationToken ct)
    {
        var evento = JsonSerializer.Deserialize<GrupoConfirmadoEvento>(json, Opciones) ?? throw new InvalidOperationException("Mensaje GrupoConfirmado inválido.");
        return ProcesarAsync(evento, ct);
    }

    /// <summary>Crea el viaje del grupo; si el grupo ya tenía uno, devuelve ese (idempotente).</summary>
    public async Task<CrearViajeResultado> ProcesarAsync(GrupoConfirmadoEvento evento, CancellationToken ct)
    {
        var comando = new CrearViajeComando(evento.GrupoId, evento.ConductorId, evento.Pasajeros);
        try
        {
            var resultado = await crearViaje.ManejarAsync(comando, ct);
            log.LogInformation("GrupoConfirmado {IdEvento} (grupo {GrupoId}): viaje {ViajeId} {Accion}.",
                evento.IdEvento, evento.GrupoId, resultado.Id, resultado.Creado ? "creado" : "ya existía");
            return resultado;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dos entregas simultáneas del mismo grupo: la otra ganó el índice único; se devuelve su viaje.
            contexto.ChangeTracker.Clear();
            return await crearViaje.ManejarAsync(comando, ct);
        }
    }
}
