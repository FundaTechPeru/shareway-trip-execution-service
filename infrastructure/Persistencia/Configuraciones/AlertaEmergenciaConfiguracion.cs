using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShareWay.TripExecution.Domain.Identificadores;
using ShareWay.TripExecution.Domain.Viajes;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Configuraciones;

internal sealed class AlertaEmergenciaConfiguracion : IEntityTypeConfiguration<AlertaEmergencia>
{
    public void Configure(EntityTypeBuilder<AlertaEmergencia> builder)
    {
        builder.ToTable("alertas_emergencia");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Valor, valor => new AlertaEmergenciaId(valor))
            .ValueGeneratedNever();
        builder.Property(a => a.ViajeId)
            .HasConversion(id => id.Valor, valor => new ViajeId(valor))
            .IsRequired();
        builder.Property(a => a.ReportanteId).IsRequired();
        builder.Property(a => a.RolReportante).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Tipo).HasMaxLength(60).IsRequired();
        builder.Property(a => a.Descripcion).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.CreadaEn).IsRequired();
        builder.Property(a => a.AtendidaEn);
        builder.Property(a => a.AtendidaPor);
        builder.Property(a => a.Version).IsConcurrencyToken();
        builder.HasIndex(a => a.Estado);
        builder.HasIndex(a => a.ViajeId);
        builder.Ignore(a => a.EventosDeDominio);
    }
}
