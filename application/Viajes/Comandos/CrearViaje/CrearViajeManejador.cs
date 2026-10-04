using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;

public sealed class CrearViajeManejador(IViajeRepositorio viajes, IUnidadDeTrabajo udt, TimeProvider reloj) : IManejadorDeComando<CrearViajeComando, Guid>
{
    public async Task<Guid> ManejarAsync(CrearViajeComando comando, CancellationToken ct)
    {
        var grupoId = new GrupoId(comando.GrupoId);
        var existente = await viajes.ObtenerPorGrupoAsync(grupoId, ct);
        if (existente is not null) return existente.Id.Valor;

        var pasajeros = comando.Pasajeros.Select(p => new PasajeroConfirmado(
            new ReservaId(p.ReservaId),
            new PasajeroId(p.PasajeroId),
            CodigoAbordaje.DesdeHash(p.CodigoHash)));

        var viaje = Viaje.Crear(grupoId, new ConductorId(comando.ConductorId), pasajeros, reloj.GetUtcNow());

        await viajes.AgregarAsync(viaje, ct);
        await udt.GuardarCambiosAsync(ct);
        return viaje.Id.Valor;
    }
}
