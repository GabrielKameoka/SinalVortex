using Microsoft.Extensions.DependencyInjection;
using SinalVortex.Application.Commands.Webhooks;

namespace SinalVortex.Worker;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerApplicationHandlers(this IServiceCollection services)
    {
        return services.AddMediatR(configuration =>
        {
            // API handlers require services that do not belong to the Worker host.
            configuration.TypeEvaluator = type => type == typeof(ProcessarInboundWebhookCommandHandler);
            configuration.RegisterServicesFromAssemblyContaining<ProcessarInboundWebhookCommandHandler>();
        });
    }
}
