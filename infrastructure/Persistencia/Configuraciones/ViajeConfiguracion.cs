using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Configuraciones;

internal sealed class ViajeConfiguracion : IEntityTypeConfiguration<Viaje>
{
    public void Configure(EntityTypeBuilder<Viaje> builder)
    {
        builder.ToTable("viajes");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasConversion(id => id.Valor, valor => new ViajeId(valor)).ValueGeneratedNever();
        builder.Property(v => v.GrupoId).HasConversion(id => id.Valor, valor => new GrupoId(valor)).IsRequired();
        builder.Property(v => v.ConductorId).HasConversion(id => id.Valor, valor => new ConductorId(valor)).IsRequired();
        builder.Property(v => v.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.InicioReal);
        builder.Property(v => v.FinReal);
        builder.Property(v => v.Version).IsConcurrencyToken();
        builder.HasIndex(v => v.GrupoId).IsUnique();
        builder.HasIndex(v => v.ConductorId);
        builder.Ignore(v => v.EventosDeDominio);

        builder.HasMany(v => v.Abordajes)
            .WithOne()
            .HasForeignKey("ViajeId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(v => v.Abordajes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
