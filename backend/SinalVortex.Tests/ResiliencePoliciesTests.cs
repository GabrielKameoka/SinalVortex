namespace SinalVortex.Tests;

public class CircuitBreakerTransitionTests
{
    private readonly Mock<IEmailProvider> _primaryMock = new();
    private readonly Mock<IEmailProvider> _secondaryMock = new();
    private readonly FakeTimeProvider _fakeTimeProvider = new();

    [Fact]
    public async Task CircuitBreaker_ShouldTransitionToHalfOpenAndClose_WhenPrimaryProviderRecovers()
    {
        var options = new CircuitBreakerStrategyOptions
        {
            FailureRatio = 1.0,
            SamplingDuration = TimeSpan.FromSeconds(10),
            // Passa explicitamente o objeto herdado de System.TimeProvider
            TimeProvider = (System.TimeProvider)_fakeTimeProvider
        };
        
        // Arrange
        const int failureThreshold = 3;
        var breakDuration = TimeSpan.FromSeconds(30);
        
        var fakeTimeProvider = new FakeTimeProvider();

        _primaryMock
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Erro temporário"));

        var circuitBreakerOptions = new CircuitBreakerStrategyOptions
        {
            FailureRatio = 1.0,
            MinimumThroughput = failureThreshold,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = breakDuration,
            TimeProvider = fakeTimeProvider,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
        };

        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(circuitBreakerOptions)
            .Build();

        var sut = new EmailService(_primaryMock.Object, _secondaryMock.Object, pipeline);

        // 1. FASE OPEN: Força as falhas até abrir o circuito
        for (int i = 0; i < failureThreshold; i++)
        {
            var act = () => sut.SendEmailAsync("Mensagem");
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        // Garante que o circuito rejeita chamadas imediatamente
        var actBlocked = () => sut.SendEmailAsync("Chamada Bloqueada");
        await actBlocked.Should().ThrowAsync<BrokenCircuitException>();

        // 2. FASE HALF-OPEN: O serviço primário recupera e o tempo é adiantado no relógio virtual
        _primaryMock
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        fakeTimeProvider.Advance(breakDuration);

        // 3. FASE CLOSED: Realiza a "trial call" no estado Half-Open
        var recoveryResult = await sut.SendEmailAsync("Chamada de Teste (Trial Call)");

        // Assert
        recoveryResult.Should().BeTrue();

        var normalCallResult = await sut.SendEmailAsync("Chamada Normal pós-Recovery");
        normalCallResult.Should().BeTrue();

        _primaryMock.Verify(p => p.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }
}