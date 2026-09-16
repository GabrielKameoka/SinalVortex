using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SinalVortex.Application.Common.Contexts;

namespace SinalVortex.Infrastructure.Persistence;

/// <summary>
/// Factory utilizada exclusivamente pelo EF Core CLI para gerar e aplicar migrations.
/// Não participa do container HTTP nem do Worker.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        // Em design-time não há requisição nem tenant autenticado. A factory nunca
        // executa consultas de negócio; existe somente para o modelo/migrations.
        return new AppDbContext(options, new TenantContext());
    }
}
