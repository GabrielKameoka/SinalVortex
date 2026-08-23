using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Infrastructure.Repositories;
using SinalVortex.Infrastructure.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers & Documentação OpenAPI / Scalar
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. Configurações de Conexão (PostgreSQL & Redis)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres";

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection")
                            ?? "localhost:6379";

// 3. Banco de Dados - PostgreSQL via Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 4. Redis - IDistributedCache + Multiplexer para Filas
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "SinalVortex_";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false; // Garante resiliência no startup da aplicação
    return ConnectionMultiplexer.Connect(configuration);
});

// 5. Injeção de Serviços do Negócio e Infraestrutura
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IHealthService, HealthService>();

builder.Services.AddScoped<INotificacaoRepository, NotificacaoRepository>();

// 6. MediatR - Registra Handlers escaneando a marcação AssemblyReference da camada Application
builder.Services.AddValidatorsFromAssembly(typeof(SinalVortex.Application.AssemblyReference).Assembly);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SinalVortex.Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(SinalVortex.Application.Common.Behaviors.ValidationBehavior<,>));
});

// 7. CORS - Política para o Frontend em Angular
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

// 8. Pipeline HTTP - Scalar API Reference no Ambiente de Desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("SinalVortex API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// 9. Execução de Migrations Pendentes no PostgreSQL durante o Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        if (context.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("Aplicando migrations pendentes no banco de dados...");
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

// 10. Middlewares e Rotas
app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.MapControllers();

app.Run();