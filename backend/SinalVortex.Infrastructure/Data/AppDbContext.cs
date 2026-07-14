using SinalVortex.Domain.Models;

namespace SinalVortex.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using SinalVortex.Domain.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    private DbSet<BaseEntity> BaseEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Adicionar configurações de entidades aqui
    }
}
