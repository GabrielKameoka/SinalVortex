using MediatR;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Domain.Enums;

namespace SinalVortex.Application.Queries.Dashboard;

public sealed record ObterMetricasDashboardQuery : IRequest<DashboardMetricsDto>;

public sealed record DashboardMetricsDto(
    int Total,
    int Delivered,
    int FailedOrDlq,
    decimal SuccessRate,
    IReadOnlyCollection<DashboardChannelMetricDto> ByChannel,
    DateTime WindowStartUtc,
    DateTime GeneratedAtUtc);

public sealed record DashboardChannelMetricDto(CanalNotificacao Channel, int Count);

public sealed record DashboardMetricsData(
    int Total,
    int Delivered,
    int FailedOrDlq,
    IReadOnlyDictionary<CanalNotificacao, int> ByChannel);

public sealed class ObterMetricasDashboardQueryHandler(INotificacaoRepository repository)
    : IRequestHandler<ObterMetricasDashboardQuery, DashboardMetricsDto>
{
    public async Task<DashboardMetricsDto> Handle(
        ObterMetricasDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var generatedAt = DateTime.UtcNow;
        var windowStart = generatedAt.AddHours(-24);
        var data = await repository.ObterMetricasDashboardAsync(windowStart, cancellationToken);
        var terminalCount = data.Delivered + data.FailedOrDlq;
        var successRate = terminalCount == 0 ? 0m : Math.Round(data.Delivered * 100m / terminalCount, 2);

        var channels = Enum.GetValues<CanalNotificacao>()
            .Select(channel => new DashboardChannelMetricDto(
                channel,
                data.ByChannel.GetValueOrDefault(channel)))
            .ToArray();

        return new DashboardMetricsDto(
            data.Total,
            data.Delivered,
            data.FailedOrDlq,
            successRate,
            channels,
            windowStart,
            generatedAt);
    }
}
