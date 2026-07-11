using System.Diagnostics.Metrics;
using IdleGuild.Api.Observability;

namespace IdleGuild.Api.Tests;

/// <summary>HTTP 요청이 표준 Meter의 처리량과 응답 시간으로 기록되는지 검증합니다.</summary>
public sealed class ObservabilityTests(IdleGuildApiFactory factory) : IClassFixture<IdleGuildApiFactory>
{
    [Fact]
    public async Task Request_RecordsCountAndDurationWithRouteTemplate()
    {
        var requestRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var durationRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ApiTelemetry.MeterName) meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            var route = tags.ToArray().SingleOrDefault(tag => tag.Key == "http.route").Value?.ToString();
            if (instrument.Name == "idleguild.api.requests" && value == 1 &&
                route == "/api/v1/system/status")
            {
                requestRecorded.TrySetResult();
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            var route = tags.ToArray().SingleOrDefault(tag => tag.Key == "http.route").Value?.ToString();
            if (instrument.Name == "idleguild.api.request.duration" && value >= 0 &&
                route == "/api/v1/system/status")
            {
                durationRecorded.TrySetResult();
            }
        });
        listener.Start();

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/system/status");

        response.EnsureSuccessStatusCode();
        await requestRecorded.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await durationRecorded.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
