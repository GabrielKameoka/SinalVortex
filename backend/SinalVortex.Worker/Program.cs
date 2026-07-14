using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("SinalVortex Worker iniciando...");

// Configurar DI
var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres";

services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

services.AddScoped<IHealthService, HealthService>();

var serviceProvider = services.BuildServiceProvider();

// Exemplo de uso de serviço
using (var scope = serviceProvider.CreateScope())
{
    var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();
    var health = await healthService.GetHealthAsync();
    Console.WriteLine($"Worker Health Status: {health.Status} at {health.Timestamp}");
}

Console.WriteLine("Worker finalizado");

