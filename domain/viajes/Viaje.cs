using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes.Eventos;
using ShareWay.TripExecution.Domain.Viajes.Excepciones;

namespace ShareWay.TripExecution.Domain.Viajes;

public sealed class Viaje : AggregateRoot<ViajeId>
{
    private readonly List<Abordaje> _abordajes = new();

    private Viaje() { }

    private Viaje(ViajeId id, GrupoId grupoId, ConductorId conductorId)
    {
        Id = id;
        GrupoId = grupoId;
        ConductorId = conductorId;
        Estado = EstadoViaje.Confirmado;
    }

    public GrupoId GrupoId { get; private set; }
    public ConductorId ConductorId { get; private set; }
    /// <summary>
    /// Vehículo con el que se registra el viaje. Es opcional: en pruebas o migraciones
    /// antiguas puede quedar nulo. Los datos del vehículo (placa, marca, modelo) viven
    /// en Driver Management; aquí sólo guardamos el id histórico para que el panel pueda
    /// rehidratarlos consultando Driver Management.
    /// </summary>
    public VehiculoId? VehiculoId { get; private set; }
    public EstadoViaje Estado { get; private set; }
    public DateTimeOffset? InicioReal { get; private set; }
    public DateTimeOffset? FinReal { get; private set; }
    public string? RutaJson { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<Abordaje> Abordajes => _abordajes.AsReadOnly();

    public static Viaje Crear(
        GrupoId grupoId, ConductorId conductorId, IEnumerable<PasajeroConfirmado> pasajeros, DateTimeOffset ahora,
        string? rutaJson = null, VehiculoId? vehiculoId = null)
    {
        var lista = pasajeros.ToList();
        if (lista.Count == 0)
            throw new ReglaDeNegocioException("Un viaje requiere al menos un pasajero confirmado.");
        if (lista.Select(p => p.ReservaId).Distinct().Count() != lista.Count)
            throw new ReglaDeNegocioException("El manifiesto contiene reservas duplicadas.");

        var viaje = new Viaje(ViajeId.Nuevo(), grupoId, conductorId);
        viaje.RutaJson = rutaJson;
        viaje.VehiculoId = vehiculoId;
        foreach (var p in lista)
            viaje._abordajes.Add(Abordaje.Pendiente(p.ReservaId, p.PasajeroId, p.Codigo, p.Pin));

        viaje.LevantarEvento(new ViajeCreado(viaje.Id, grupoId, conductorId, ahora));
        return viaje;
    }

    public ResultadoAbordaje RegistrarAbordaje(
        ConductorId solicitante, ReservaId reservaId, string codigo, MetodoAbordaje metodo, DateTimeOffset ahora)
    {
        AsegurarConductor(solicitante);
        if (Estado is not (EstadoViaje.Confirmado or EstadoViaje.EnCurso))
            throw new TransicionDeEstadoInvalidaException(Id, Estado, nameof(RegistrarAbordaje));

        var abordaje = _abordajes.FirstOrDefault(a => a.Id == reservaId)
                       ?? throw new PasajeroNoPerteneceAlViajeException(Id, reservaId);

        var resultado = abordaje.Validar(codigo, metodo, ahora);

        switch (resultado)
        {
            case ResultadoAbordaje.Abordado:
                LevantarEvento(new PasajeroAbordo(Id, reservaId, abordaje.PasajeroId, metodo, ahora));
                break;
            case ResultadoAbordaje.CodigoInvalido:
                LevantarEvento(new AbordajeFallido(
                    Id, reservaId, abordaje.PasajeroId, abordaje.IntentosFallidos, abordaje.EstaBloqueado, ahora));
                break;
        }

        if (resultado is ResultadoAbordaje.Abordado or ResultadoAbordaje.CodigoInvalido)
            Version++;

        return resultado;
    }

    public void Iniciar(ConductorId solicitante, DateTimeOffset ahora)
    {
        AsegurarConductor(solicitante);
        AsegurarEstado(EstadoViaje.Confirmado, nameof(Iniciar));
        if (!_abordajes.Any(a => a.EstaAbordado))
            throw new ReglaDeNegocioException("No se puede iniciar un viaje sin al menos un pasajero abordado.");

        Estado = EstadoViaje.EnCurso;
        InicioReal = ahora;
        Version++;
        LevantarEvento(new ViajeIniciado(Id, GrupoId, ConductorId, ahora));
    }

    public void Finalizar(ConductorId solicitante, DateTimeOffset ahora)
    {
        AsegurarConductor(solicitante);
        AsegurarEstado(EstadoViaje.EnCurso, nameof(Finalizar));

        Estado = EstadoViaje.Finalizado;
        FinReal = ahora;
        Version++;

        var abordadas = _abordajes.Where(a => a.EstaAbordado).Select(a => a.Id).ToArray();
        var distanciaKm = RutaViaje.DesdeJson(RutaJson) is { } ruta
            ? Math.Round(ruta.DistanciaTotalKm(), 2, MidpointRounding.AwayFromZero)
            : (double?)null;
        LevantarEvento(new ViajeFinalizado(Id, GrupoId, ConductorId, InicioReal!.Value, ahora, abordadas, distanciaKm));
    }

    public void Cancelar(string motivo, DateTimeOffset ahora)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ReglaDeNegocioException("El motivo de cancelación es obligatorio.");
        AsegurarEstado(EstadoViaje.Confirmado, nameof(Cancelar));

        Estado = EstadoViaje.Cancelado;
        Version++;
        LevantarEvento(new ViajeCancelado(Id, GrupoId, motivo.Trim(), ahora));
    }

    /// <summary>Reordena las paradas de la ruta sin cambiar el estado del viaje.</summary>
    public void OptimizarRuta(DateTimeOffset ahora)
    {
        if (RutaViaje.DesdeJson(RutaJson) is not { } ruta)
            throw new ReglaDeNegocioException("El viaje no tiene una ruta que se pueda optimizar.");

        var optimizada = ruta.Optimizada();
        if (ruta.TieneMismoOrden(optimizada)) return;

        RutaJson = optimizada.AJson();
        Version++;
        LevantarEvento(new RutaOptimizada(Id, "Orden de paradas optimizado", ahora));
    }

    /// <summary>
    /// Quita la parada de un pasajero que canceló y recalcula el orden de la ruta.
    /// El viaje sigue en su estado actual (incluido <see cref="EstadoViaje.EnCurso"/>).
    /// </summary>
    public void CancelarParada(ReservaId reservaId, string motivo, DateTimeOffset ahora, string? etiqueta = null)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ReglaDeNegocioException("El motivo de cancelación de la parada es obligatorio.");
        if (Estado is not (EstadoViaje.Confirmado or EstadoViaje.EnCurso))
            throw new TransicionDeEstadoInvalidaException(Id, Estado, nameof(CancelarParada));
        if (!_abordajes.Any(a => a.Id == reservaId))
            throw new PasajeroNoPerteneceAlViajeException(Id, reservaId);
        if (RutaViaje.DesdeJson(RutaJson) is not { } ruta)
            throw new ReglaDeNegocioException("El viaje no tiene una ruta de la cual quitar la parada.");

        RutaJson = ruta.SinParada(reservaId.Valor, etiqueta).Optimizada().AJson();
        Version++;
        LevantarEvento(new ParadaCancelada(Id, reservaId, motivo.Trim(), ahora));
    }

    private void AsegurarConductor(ConductorId solicitante)
    {
        if (solicitante != ConductorId)
            throw new ConductorNoAsignadoException(Id, solicitante);
    }

    private void AsegurarEstado(EstadoViaje esperado, string operacion)
    {
        if (Estado != esperado)
            throw new TransicionDeEstadoInvalidaException(Id, Estado, operacion);
    }
}
