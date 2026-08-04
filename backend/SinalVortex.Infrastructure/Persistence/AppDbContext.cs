using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Models;

namespace SinalVortex.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Aplicacoes> Aplicacoes => Set<Aplicacoes>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<LogNotificacao> LogsNotificacoes => Set<LogNotificacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeamento direto do Value Object Destinatario
        modelBuilder.Entity<Notificacao>(entity =>
        {
            entity.ComplexProperty(n => n.Destinatario, d =>
            {
                d.Property(p => p.Valor)
                    .HasColumnName("Destinatario")
                    .HasMaxLength(255)
                    .IsRequired();
            });
        });

        // Mantém a varredura para quaisquer outras configurações no assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}