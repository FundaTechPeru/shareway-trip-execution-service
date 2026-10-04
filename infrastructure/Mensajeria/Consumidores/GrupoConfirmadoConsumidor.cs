using System.Text.Json;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Entrantes;
using ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;

namespace ShareWay.TripExecution.Infrastructure.Mensajeria.Consumidores;

public sealed class GrupoConfirmadoConsumidor(IManejadorDeComando<CrearViajeComando, Guid> crearViaje)
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);
    public async Task ProcesarAsync(string json, CancellationToken ct)
    {
        var evento = JsonSerializer.Deserialize<GrupoConfirmadoEvento>(json, Opciones) ?? throw new InvalidOperationException("Mensaje GrupoConfirmado inválido.");
        await crearViaje.ManejarAsync(new CrearViajeComando(evento.GrupoId, evento.ConductorId, evento.Pasajeros), ct);
    }
}
