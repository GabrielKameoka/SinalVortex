using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Infrastructure.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configuração da String de Conexão do Redis (Suporta Local e Railway)
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection")
                            ?? "localhost:6379";

// Registro do Redis no Inversion of Control (IoC)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "SinalVortex_";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnectionString));

// Registra nossa Abstração do Cache
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Register Application Services
builder.Services.AddScoped<IHealthService, HealthService>();

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Verifica se há migrações pendentes e as aplica no banco da Railway
        if (context.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("Aplicando migrations pendentes no banco de dados da Railway...");
            context.Database.Migrate();
            Console.WriteLine("Banco de dados atualizado com sucesso!");
        }
        else
        {
            Console.WriteLine("Nenhuma migration pendente detectada.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao aplicar migrations na inicialização: {ex.Message}");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.MapControllers();

app.Run();