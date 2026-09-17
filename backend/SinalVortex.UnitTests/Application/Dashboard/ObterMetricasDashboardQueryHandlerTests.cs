using NSubstitute;
using SinalVortex.Application.Common.Interfaces;
using SinalVortex.Application.Queries.Dashboard;
using SinalVortex.Domain.Enums;

namespace SinalVortex.UnitTests.Application.Dashboard;

public class ObterMetricasDashboardQueryHandlerTests
{
    [Fact]
    public async Task Handle_DeveCalcularTaxaEPreencherTodosOsCanais()
    {
        var repository = Substitute.For<INotificacaoRepository>();
        repository.ObterMetricasDashboardAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardMetricsData(
                10,
                7,
                3,
                new Dictionary<CanalNotificacao, int> { [CanalNotificacao.Email] = 6, [CanalNotificacao.Sms] = 4 }));

        var result = await new ObterMetricasDashboardQueryHandler(repository)
            .Handle(new ObterMetricasDashboardQuery(), CancellationToken.None);

        Assert.Equal(10, result.Total);
        Assert.Equal(7, result.Delivered);
        Assert.Equal(3, result.FailedOrDlq);
        Assert.Equal(70m, result.SuccessRate);
        Assert.Equal(5, result.ByChannel.Count);
        Assert.Contains(result.ByChannel, item => item.Channel == CanalNotificacao.Email && item.Count == 6);
        Assert.Contains(result.ByChannel, item => item.Channel == CanalNotificacao.Push && item.Count == 0);
        await repository.Received(1).ObterMetricasDashboardAsync(
            Arg.Is<DateTime>(date => date <= DateTime.UtcNow.AddHours(-23) && date >= DateTime.UtcNow.AddHours(-25)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SemStatusTerminal_DeveRetornarTaxaZero()
    {
        var repository = Substitute.For<INotificacaoRepository>();
        repository.ObterMetricasDashboardAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardMetricsData(2, 0, 0, new Dictionary<CanalNotificacao, int>()));

        var result = await new ObterMetricasDashboardQueryHandler(repository)
            .Handle(new ObterMetricasDashboardQuery(), CancellationToken.None);

        Assert.Equal(0m, result.SuccessRate);
    }
}
