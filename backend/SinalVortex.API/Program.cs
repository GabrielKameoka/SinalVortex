using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Health;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Infrastructure.Repositories;
using SinalVortex.Infrastructure.Services;
using SinalVortex.Infrastructure.Services.Notificacoes;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers & Documentação OpenAPI / Scalar
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = "https://sinalvortex-production.up.railway.app" }
        };
        return Task.CompletedTask;
    });
});

// 2. Health Check
builder.Services.AddHealthChecks()
    .AddCheck<SinalVortexHealthCheck>("infra_health_check");

// 3. Configurações de Conexão (PostgreSQL & Redis)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=sinalvortex;Username=postgres;Password=postgres";

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection")
                            ?? "localhost:6379";

// 4. Banco de Dados - PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 5. Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "SinalVortex_";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

// 6. Injeção de Serviços
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<INotificacaoRepository, NotificacaoRepository>();
builder.Services.AddScoped<IRedisQueueService, RedisQueueService>();
builder.Services.AddSingleton<IEmailResiliencePolicy, EmailResiliencePolicy>();
builder.Services.AddScoped<INotificacaoService, EmailNotificacaoService>();

// 7. MediatR
builder.Services.AddValidatorsFromAssembly(typeof(SinalVortex.Application.AssemblyReference).Assembly);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SinalVortex.Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(SinalVortex.Application.Common.Behaviors.ValidationBehavior<,>));
});

// 8. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// 9. Pipeline HTTP
app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("SinalVortex API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapHealthChecks("/health");

// 10. Execução de Migrations Pendentes
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        if (context.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("Aplicando migrations pendentes no banco de dados...");
            context.Database.Migrate();
            logger.LogInformation("Banco de dados atualizado com sucesso!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao aplicar migrations na inicialização do banco de dados.");
    }
}

// 11. Middlewares e Rotas
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();