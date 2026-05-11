using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

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
          throw new InvalidOperationException("Missing Redis connection string (Redis:ConnectionString / Redis__ConnectionString).");
        return ConnectionMultiplexer.Connect(cs);
      });

      builder.Services.AddSingleton<IConnectionFactory>(_ =>
      {
        var host = builder.Configuration["RabbitMq:HostName"] ?? builder.Configuration["RabbitMq__HostName"] ?? "localhost";
        return new ConnectionFactory { HostName = host };
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
    private const string RabbitExchange = "landa.events";
    private const string SensorStatusRoutingKey = "sensor.status.changed";

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionFactory _rabbitFactory;
    private readonly SensorSimulator _sim;

    public Worker(IConnectionMultiplexer redis, IConnectionFactory rabbitFactory, SensorSimulator sim)
    {
      _redis = redis;
      _rabbitFactory = rabbitFactory;
      _sim = sim;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      await PublishSensorStatusOnlineAsync(stoppingToken);

      var db = _redis.GetDatabase();
      using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

      while (await timer.WaitForNextTickAsync(stoppingToken))
      {
        var now = DateTimeOffset.UtcNow;
        var readings = _sim.GenerateTick(now, SensorCount);

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

    private async Task PublishSensorStatusOnlineAsync(CancellationToken ct)
    {
      for (var attempt = 0; !ct.IsCancellationRequested; attempt++)
      {
        try
        {
          using var connection = _rabbitFactory.CreateConnection();
          using var channel = connection.CreateModel();

          channel.ExchangeDeclare(RabbitExchange, ExchangeType.Topic, durable: true);

          var now = DateTimeOffset.UtcNow;
          for (var i = 1; i <= SensorCount; i++)
          {
            var msg = new
            {
              schemaVersion = 1,
              eventId = Guid.NewGuid().ToString(),
              occurredAtUtc = now.ToString("O"),
              sensorId = $"sensor-{i:00}",
              status = "Online",
              details = (string?)null
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));
            var props = channel.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2; // persistent

            channel.BasicPublish(RabbitExchange, SensorStatusRoutingKey, props, body);
          }

          Console.WriteLine($"Published SensorStatusChanged(Online) for {SensorCount} sensors to RabbitMQ");
          return;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
          var delay = Math.Min(10_000, 1_000 * (int)Math.Pow(2, Math.Min(attempt, 6)));
          Console.WriteLine($"RabbitMQ not ready (attempt {attempt + 1}), retrying in {delay}ms: {ex.Message}");
          await Task.Delay(delay, ct).ConfigureAwait(false);
        }
      }
    }
  }

  public sealed class SensorSimulator
  {
    public IReadOnlyList<SensorReading> GenerateTick(DateTimeOffset nowUtc, int sensorCount)
    {
      if (sensorCount != 20)
        throw new ArgumentOutOfRangeException(nameof(sensorCount), "sensorCount must be exactly 20.");

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
