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

        // Varre o assembly atual (Infrastructure) e aplica automaticamente 
        // todas as classes públicas que implementam IEntityTypeConfiguration<T>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}