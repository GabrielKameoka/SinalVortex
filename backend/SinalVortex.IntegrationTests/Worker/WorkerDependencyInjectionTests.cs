using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SinalVortex.Application.Commands.Webhooks;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Worker;

namespace SinalVortex.IntegrationTests.Worker;

public class WorkerDependencyInjectionTests
{
    [Fact]
    public void Worker_DeveValidarHandlersSemDependenciasDaApi()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<INotificacaoRepository>(_ => Substitute.For<INotificacaoRepository>());
        services.AddWorkerApplicationHandlers();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();
        Assert.IsType<ProcessarInboundWebhookCommandHandler>(
            scope.ServiceProvider.GetRequiredService<IRequestHandler<ProcessarInboundWebhookCommand, bool>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISender>());
    }
}
