using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace IntegrationTests;

public sealed class RealTimeFlowTests
{
  private static readonly string[] ExpectedSensorIds =
    Enumerable.Range(1, 20).Select(i => $"sensor-{i:00}").ToArray();

  [Fact(Timeout = 60_000)]
  public async Task SignalR_receives_telemetry_for_all_20_sensors_within_time_window()
  {
    // When tests run inside a container, "localhost" points to the test container itself.
    // On Docker Desktop, host services are reachable via host.docker.internal.
    var baseUrl = Environment.GetEnvironmentVariable("REST_API_BASE_URL") ?? "http://host.docker.internal:5001";
    var hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/telemetry";

    var seen = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
    var total = 0;

    var conn = new HubConnectionBuilder()
      .WithUrl(hubUrl, options =>
      {
        options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets
                             | Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents
                             | Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
      })
      .WithAutomaticReconnect()
      .Build();

    conn.On<TelemetryDto>("telemetry", msg =>
    {
      if (!string.IsNullOrWhiteSpace(msg.sensorId))
      {
        seen.AddOrUpdate(msg.sensorId, 1, (_, c) => c + 1);
      }
      Interlocked.Increment(ref total);
    });

    await conn.StartAsync();

    try
    {
      // We expect ~20 msgs/sec. Give it a few seconds to tolerate startup/reconnect jitter.
      var deadline = DateTimeOffset.UtcNow.AddSeconds(8);
      while (DateTimeOffset.UtcNow < deadline)
      {
        if (ExpectedSensorIds.All(id => seen.ContainsKey(id)))
        {
          break;
        }
        await Task.Delay(200);
      }

      var missing = ExpectedSensorIds.Where(id => !seen.ContainsKey(id)).ToArray();
      Assert.True(missing.Length == 0, $"Missing sensors: {string.Join(", ", missing)}");

      // Extra sanity: prove "real-time flow" (not just a single burst).
      // After we have seen all sensors at least once, wait a bit and verify more messages arrive.
      var totalAfterFirstCoverage = total;
      await Task.Delay(2000);
      Assert.True(total >= totalAfterFirstCoverage + 20,
        $"Expected at least 20 additional messages after coverage. Before={totalAfterFirstCoverage}, After={total}");
    }
    finally
    {
      await conn.DisposeAsync();
    }
  }

  private sealed record TelemetryDto(string sensorId, string timestampUtc, double value);
}

