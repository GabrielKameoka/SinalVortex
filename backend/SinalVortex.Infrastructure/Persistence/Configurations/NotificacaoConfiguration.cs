using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SinalVortex.Domain.Models;

namespace SinalVortex.Infrastructure.Persistence.Configurations;

public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("Notificacoes");

        builder.HasKey(n => n.Id);

        // Mapeamento explícito do Value Object Destinatario
        builder.ComplexProperty(n => n.Destinatario, d =>
        {
            d.Property(p => p.Valor)
                .HasColumnName("Destinatario")
                .HasMaxLength(255)
                .IsRequired();
        });

        builder.Property(n => n.Conteudo)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(n => n.Assunto)
            .HasMaxLength(200);

        builder.Property(n => n.Status)
            .IsRequired();

        builder.Property(n => n.Canal)
            .IsRequired();

        builder.Property(n => n.Prioridade)
            .IsRequired();

        // Configuração do relacionamento 1:N com LogNotificacao usando o campo privado _logs
        builder.HasMany(n => n.Logs)
            .WithOne()
            .HasForeignKey(l => l.NotificacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices para otimização de consultas paginadas e por status
        builder.HasIndex(n => n.AplicacaoId);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.CriadoEm);
    }
}