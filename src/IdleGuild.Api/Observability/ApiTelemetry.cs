using System.Diagnostics.Metrics;

namespace IdleGuild.Api.Observability;

/// <summary>API 처리량, 오류와 응답 시간을 표준 .NET Meter로 노출합니다.</summary>
public static class ApiTelemetry
{
    public const string MeterName = "IdleGuild.Api";
    public const string TraceHeaderName = "X-Trace-Id";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> RequestCount =
        Meter.CreateCounter<long>("idleguild.api.requests", "{request}", "Completed HTTP requests.");

    public static readonly Counter<long> ErrorCount =
        Meter.CreateCounter<long>("idleguild.api.errors", "{error}", "HTTP responses with status 500 or higher.");

    public static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("idleguild.api.request.duration", "ms", "HTTP request duration.");
}
