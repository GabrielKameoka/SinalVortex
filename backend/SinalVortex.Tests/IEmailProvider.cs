using Polly;

namespace SinalVortex.Tests;

public interface IEmailProvider
{
    Task<bool> SendAsync(string message, CancellationToken cancellationToken);
}

public class EmailService
{
    private readonly IEmailProvider _primary;
    private readonly IEmailProvider _fallback;
    private readonly ResiliencePipeline _pipeline;

    public EmailService(IEmailProvider primary, IEmailProvider fallback, ResiliencePipeline pipeline)
    {
        _primary = primary;
        _fallback = fallback;
        _pipeline = pipeline;
    }

    public async Task<bool> SendEmailAsync(string message, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            return await _primary.SendAsync(message, ct);
        }, cancellationToken);
    }
}