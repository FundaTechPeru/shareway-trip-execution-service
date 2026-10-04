using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Configuraciones;

internal sealed class AbordajeConfiguracion : IEntityTypeConfiguration<Abordaje>
{
    public void Configure(EntityTypeBuilder<Abordaje> builder)
    {
        builder.ToTable("abordajes");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("reserva_id")
            .HasConversion(id => id.Valor, valor => new ReservaId(valor))
            .ValueGeneratedNever();

        builder.Property<ViajeId>("ViajeId")
            .HasColumnName("viaje_id")
            .HasConversion(id => id.Valor, valor => new ViajeId(valor));
        builder.HasIndex("ViajeId");

        builder.Property(a => a.PasajeroId).HasConversion(id => id.Valor, valor => new PasajeroId(valor)).IsRequired();
        builder.Property(a => a.Codigo)
            .HasColumnName("codigo_hash")
            .HasConversion(c => c.Hash, hash => CodigoAbordaje.DesdeHash(hash))
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(a => a.IntentosFallidos).IsRequired();
        builder.Property(a => a.Metodo).HasConversion<string>().HasMaxLength(10);
        builder.Property(a => a.AbordadoEn);

        builder.Ignore(a => a.EstaAbordado);
        builder.Ignore(a => a.EstaBloqueado);
    }
}
