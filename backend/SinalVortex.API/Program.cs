using FluentValidation;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using SinalVortex.Api.Middlewares;
using SinalVortex.API.Authentication;
using SinalVortex.API.Middlewares;
using SinalVortex.Application.Common.Contexts;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Services;
using SinalVortex.Infrastructure.Health;
using SinalVortex.Infrastructure.Persistence;
using SinalVortex.Infrastructure.Repositories;
using SinalVortex.Infrastructure.Services;
using SinalVortex.Infrastructure.Services.Notificacoes;
using SinalVortex.Infrastructure.Services.Webhooks;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                 ?? throw new InvalidOperationException("A seção Jwt é obrigatória.");
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey deve possuir ao menos 32 bytes e ser fornecida por secret manager ou variável de ambiente.");
if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
    throw new InvalidOperationException("Jwt:Issuer e Jwt:Audience são obrigatórios.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tenantClaim = context.Principal?.FindFirst("tenant_id")?.Value;
                if (!Guid.TryParse(tenantClaim, out _))
                    context.Fail("O token não contém um tenant_id válido.");

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 1. Controllers & Documentação OpenAPI / Scalar
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var serverUrl = builder.Environment.IsDevelopment()
    ? "http://localhost:5287"
    : builder.Configuration["ApiBaseUrl"] ?? "https://sinalvortex-production.up.railway.app";

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = serverUrl }
        };
        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Informe o JWT no formato: Bearer {token}."
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
builder.Services.AddScoped<IContatoRepository, ContatoRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRedisQueueService, RedisQueueService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<IWebhookSignatureValidator, WebhookSignatureValidator>();
builder.Services.AddSingleton<IEmailResiliencePolicy, EmailResiliencePolicy>();
builder.Services.AddScoped<INotificacaoService, EmailNotificacaoService>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

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
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// 9. Pipeline Middleware Base
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors("AllowAll");

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

// 11. Endpoints Públicos e Documentação (PermitAnonymous Explícito)
app.MapHealthChecks("/health").AllowAnonymous();

// Expõe a spec OpenAPI de forma pública
app.MapOpenApi().AllowAnonymous();

// Configura o Scalar apontando explicitamente para o spec /openapi/v1.json
app.MapScalarApiReference("/scalar/v1", options =>
{
    options
        .WithTitle("SinalVortex API")
        .WithOpenApiRoutePattern("/openapi/v1.json")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
}).AllowAnonymous();

// 12. Endpoints Autenticados
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
