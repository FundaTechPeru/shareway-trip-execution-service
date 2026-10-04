using Microsoft.EntityFrameworkCore;
using ShareWay.TripExecution.Application.Abstracciones;
using ShareWay.TripExecution.Application.EventosDeIntegracion.Salientes;
using ShareWay.TripExecution.Domain.SeedWork;
using ShareWay.TripExecution.Domain.Viajes;
using ShareWay.TripExecution.Infrastructure.Persistencia.Outbox;

namespace ShareWay.TripExecution.Infrastructure.Persistencia;

public sealed class TripExecutionDbContext(DbContextOptions<TripExecutionDbContext> opciones) : DbContext(opciones), IUnidadDeTrabajo
{
    public DbSet<Viaje> Viajes => Set<Viaje>();
    public DbSet<MensajeOutbox> Outbox => Set<MensajeOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("trip_execution");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TripExecutionDbContext).Assembly);
    }

    public Task<int> GuardarCambiosAsync(CancellationToken ct) => SaveChangesAsync(ct);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EncolarEventosEnOutbox();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void EncolarEventosEnOutbox()
    {
        var raices = ChangeTracker.Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(r => r.EventosDeDominio.Count > 0)
            .ToList();

        foreach (var raiz in raices)
        {
            foreach (var eventoDeDominio in raiz.EventosDeDominio)
            {
                var integracion = MapeadorDeEventos.ADeIntegracion(eventoDeDominio);
                if (integracion is not null)
                    Outbox.Add(MensajeOutbox.Crear(integracion));
            }
            raiz.LimpiarEventos();
        }
    }
}
