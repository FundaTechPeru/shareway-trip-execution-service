using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShareWay.TripExecution.Infrastructure.Persistencia.Outbox;

namespace ShareWay.TripExecution.Infrastructure.Persistencia.Configuraciones;

internal sealed class MensajeOutboxConfiguracion : IEntityTypeConfiguration<MensajeOutbox>
{
    public void Configure(EntityTypeBuilder<MensajeOutbox> builder)
    {
        builder.ToTable("outbox");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Tipo).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Contenido).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OcurridoEn).IsRequired();
        builder.Property(m => m.Intentos).IsRequired().HasDefaultValue(0);
        builder.Property(m => m.SiguienteIntentoEn);
        builder.Property(m => m.FallidoEn);
        builder.Property(m => m.UltimoError).HasMaxLength(MensajeOutbox.LongitudMaximaError);
        builder.HasIndex(m => m.ProcesadoEn);
    }
}
