namespace SinalVortex.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;

public class SinalVortexMetrics
{
    public const string MeterName = "SinalVortex.Metrics";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> NotificacoesProcessadas = Meter.CreateCounter<long>(
        "sinalvortex_notificacoes_processadas_total",
        description: "Total de notificações processadas com sucesso");

    public static readonly Counter<long> NotificacoesFalhas = Meter.CreateCounter<long>(
        "sinalvortex_notificacoes_falhas_total",
        description: "Total de notificações que falharam no processamento");

    public static readonly Histogram<double> TempoProcessamentoMs = Meter.CreateHistogram<double>(
        "sinalvortex_processamento_duracao_ms",
        unit: "ms",
        description: "Tempo de execução do processamento da notificação");
}