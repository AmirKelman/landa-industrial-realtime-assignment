using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace TelemetryService
{
  internal static class Program
  {
    public static async Task Main(string[] args)
    {
      var builder = Host.CreateApplicationBuilder(args);

      builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
      {
        var cs = builder.Configuration["Redis:ConnectionString"] ?? builder.Configuration["Redis__ConnectionString"];
        if (string.IsNullOrWhiteSpace(cs))
        {
          throw new InvalidOperationException("Missing Redis connection string (Redis:ConnectionString / Redis__ConnectionString).");
        }
        return ConnectionMultiplexer.Connect(cs);
      });

      builder.Services.AddSingleton<SensorSimulator>();
      builder.Services.AddHostedService<Worker>();

      var host = builder.Build();
      await host.RunAsync();
    }
  }

  public sealed class Worker : BackgroundService
  {
    private const int SensorCount = 20;
    private const string TelemetryStreamKey = "telemetry:stream";

    private readonly IConnectionMultiplexer _redis;
    private readonly SensorSimulator _sim;

    public Worker(IConnectionMultiplexer redis, SensorSimulator sim)
    {
      _redis = redis;
      _sim = sim;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      var db = _redis.GetDatabase();
      using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

      while (await timer.WaitForNextTickAsync(stoppingToken))
      {
        var now = DateTimeOffset.UtcNow;
        var readings = _sim.GenerateTick(now, SensorCount);

        // Add 20 entries per second (one per sensor) into a single Redis Stream.
        // The authoritative telemetry stream originates in Redis, per assignment.
        foreach (var r in readings)
        {
          var fields = new NameValueEntry[]
          {
            new("sensorId", r.SensorId),
            new("tsUtc", r.TimestampUtc.ToString("O")),
            new("value", r.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
          };

          _ = await db.StreamAddAsync(TelemetryStreamKey, fields).ConfigureAwait(false);
        }

        Console.WriteLine($"telemetry tick {now:O} wrote {readings.Count} stream entries");
      }
    }
  }

  public sealed class SensorSimulator
  {
    public IReadOnlyList<SensorReading> GenerateTick(DateTimeOffset nowUtc, int sensorCount)
    {
      if (sensorCount != 20)
      {
        // Keep the requirement explicit and hard to accidentally change.
        throw new ArgumentOutOfRangeException(nameof(sensorCount), "sensorCount must be exactly 20.");
      }

      var list = new List<SensorReading>(capacity: sensorCount);
      var t = nowUtc.ToUnixTimeSeconds();

      for (var i = 1; i <= sensorCount; i++)
      {
        var sensorId = $"sensor-{i:00}";
        var baseValue = i * 10.0;
        var wave = Math.Sin((t + i) * 0.1) * 5.0;
        var value = baseValue + wave;

        list.Add(new SensorReading(sensorId, nowUtc, value));
      }

      return list;
    }
  }

  public readonly record struct SensorReading(string SensorId, DateTimeOffset TimestampUtc, double Value);
}

