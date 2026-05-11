using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace RestApi;

public sealed class RedisTelemetryStreamWorker : BackgroundService
{
  private const string StreamKey = "telemetry:stream";
  private const string GroupName = "rest-api";
  private const string ConsumerName = "rest-api-1";

  private readonly IConnectionMultiplexer _redis;
  private readonly IHubContext<TelemetryHub> _hub;
  private readonly TelemetryStreamEntryParser _parser;

  public RedisTelemetryStreamWorker(IConnectionMultiplexer redis, IHubContext<TelemetryHub> hub, TelemetryStreamEntryParser parser)
  {
    _redis = redis;
    _hub = hub;
    _parser = parser;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var db = _redis.GetDatabase();
    await EnsureConsumerGroupExists(db);

    while (!stoppingToken.IsCancellationRequested)
    {
      // True blocking read (no polling): Redis blocks up to 1000ms waiting for stream entries.
      // Command: XREADGROUP GROUP <group> <consumer> BLOCK 1000 COUNT 200 STREAMS <key> >
      var result = await db.ExecuteAsync("XREADGROUP",
        "GROUP", GroupName, ConsumerName,
        "BLOCK", "1000",
        "COUNT", "200",
        "STREAMS", StreamKey, ">"
      ).ConfigureAwait(false);

      var entries = RedisStreamResultParser.ParseEntries(result);
      if (entries.Count == 0)
      {
        continue;
      }

      foreach (var entry in entries)
      {
        if (_parser.TryParse(entry.fields, out var msg))
        {
          await _hub.Clients.All.SendAsync("telemetry", msg, stoppingToken);
        }

        await db.StreamAcknowledgeAsync(StreamKey, GroupName, entry.id).ConfigureAwait(false);
      }

      Console.WriteLine($"redis->signalr forwarded {entries.Count} entries");
    }
  }

  private static async Task EnsureConsumerGroupExists(IDatabase db)
  {
    try
    {
      await db.StreamCreateConsumerGroupAsync(StreamKey, GroupName, "0-0", createStream: true).ConfigureAwait(false);
    }
    catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
    {
      // group already exists
    }
  }
}

internal static class RedisStreamResultParser
{
  // Expected XREADGROUP reply:
  // [
  //   [ streamKey,
  //     [
  //       [ entryId, [ field, value, field, value, ... ] ],
  //       ...
  //     ]
  //   ]
  // ]
  internal static List<(RedisValue id, NameValueEntry[] fields)> ParseEntries(RedisResult result)
  {
    var list = new List<(RedisValue id, NameValueEntry[] fields)>();
    if (result.IsNull) return list;

    var outer = (RedisResult[]?)result;
    if (outer is null || outer.Length == 0) return list;

    foreach (var streamBlock in outer)
    {
      var sb = (RedisResult[]?)streamBlock;
      if (sb is null || sb.Length < 2) continue;

      var entries = (RedisResult[]?)sb[1];
      if (entries is null) continue;

      foreach (var entry in entries)
      {
        var e = (RedisResult[]?)entry;
        if (e is null || e.Length < 2) continue;

        var id = (RedisValue)(string)e[0]!;
        var fv = (RedisResult[]?)e[1];
        if (fv is null || fv.Length % 2 != 0) continue;

        var fields = new NameValueEntry[fv.Length / 2];
        for (var i = 0; i < fv.Length; i += 2)
        {
          var name = (RedisValue)(string)fv[i]!;
          var value = (RedisValue)(string)fv[i + 1]!;
          fields[i / 2] = new NameValueEntry(name, value);
        }

        list.Add((id, fields));
      }
    }

    return list;
  }
}

