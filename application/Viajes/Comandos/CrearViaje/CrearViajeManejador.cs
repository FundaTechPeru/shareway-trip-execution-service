using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Application.Viajes.Comandos.CrearViaje;

public sealed class CrearViajeManejador(IViajeRepositorio viajes, IUnidadDeTrabajo udt, TimeProvider reloj) : IManejadorDeComando<CrearViajeComando, CrearViajeResultado>
{
    public async Task<CrearViajeResultado> ManejarAsync(CrearViajeComando comando, CancellationToken ct)
    {
        var grupoId = new GrupoId(comando.GrupoId);
        var existente = await viajes.ObtenerPorGrupoAsync(grupoId, ct);
        if (existente is not null) return new CrearViajeResultado(existente.Id.Valor, Creado: false);

        var pasajeros = comando.Pasajeros.Select(p => new PasajeroConfirmado(
            new ReservaId(p.ReservaId),
            new PasajeroId(p.PasajeroId),
            CodigoAbordaje.DesdeHash(p.CodigoHash),
            string.IsNullOrWhiteSpace(p.PinHash) ? null : CodigoAbordaje.DesdeHash(p.PinHash)));

        var viaje = Viaje.Crear(
            grupoId,
            new ConductorId(comando.ConductorId),
            pasajeros,
            reloj.GetUtcNow(),
            comando.RutaJson,
            VehiculoId.Desde(comando.VehiculoId));

        await viajes.AgregarAsync(viaje, ct);
        await udt.GuardarCambiosAsync(ct);
        return new CrearViajeResultado(viaje.Id.Valor, Creado: true);
    }
}
