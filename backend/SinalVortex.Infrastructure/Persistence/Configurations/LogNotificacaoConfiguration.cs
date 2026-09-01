using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SinalVortex.Domain.Entities;

namespace SinalVortex.Infrastructure.Persistence.Configurations;

public class LogNotificacaoConfiguration : IEntityTypeConfiguration<LogNotificacao>
{
    public void Configure(EntityTypeBuilder<LogNotificacao> builder)
    {
        builder.ToTable("LogsNotificacoes");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.MensagemErro)
            .HasMaxLength(1000);

        builder.Property(l => l.StatusAnterior)
            .IsRequired();

        builder.Property(l => l.NovoStatus)
            .IsRequired();

        builder.Property(l => l.CriadoEm)
            .IsRequired();

        builder.HasIndex(l => l.NotificacaoId);
    }
}