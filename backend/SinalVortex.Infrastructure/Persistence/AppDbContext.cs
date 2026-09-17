using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Common;
using SinalVortex.Domain.Entities;
using SinalVortex.Domain.Models;

namespace SinalVortex.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    
    // Propriedade pública lida diretamente pelo Global Query Filter da Expression Tree
    public Guid CurrentTenantId => _tenantContext?.TenantId ?? Guid.Empty;

    public DbSet<Aplicacoes> Aplicacoes => Set<Aplicacoes>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Contato> Contatos => Set<Contato>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) 
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeia o Value Object Destinatario como Owned Type
        modelBuilder.Entity<Notificacao>(b =>
        {
            b.OwnsOne(n => n.Destinatario, d =>
            {
                d.Property(p => p.Valor)
                    .HasColumnName("Destinatario") // Nome da coluna na tabela de Notificacoes
                    .IsRequired();
            });
        });

        modelBuilder.Entity<Usuario>(b =>
        {
            b.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            b.Property(u => u.Email).HasMaxLength(320);
            b.Property(u => u.Nome).HasMaxLength(200);
        });

        modelBuilder.Entity<Template>()
            .HasQueryFilter(template => template.TenantId == CurrentTenantId);
        
        // Aplica Global Query Filter para todas as entidades derivadas de BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                
                // Mapeia e.TenantId == this.CurrentTenantId
                var filter = Expression.Lambda(
                    Expression.Equal(
                        Expression.Property(parameter, nameof(BaseEntity.TenantId)),
                        Expression.Property(Expression.Constant(this), nameof(CurrentTenantId))
                    ),
                    parameter
                );

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                if (_tenantContext?.HasTenant == true)
                {
                    entry.Entity.SetTenantId(_tenantContext.TenantId);
                }
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Touch();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
