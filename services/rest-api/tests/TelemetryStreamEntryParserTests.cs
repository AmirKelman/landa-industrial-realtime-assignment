using StackExchange.Redis;
using Xunit;
using RestApi;

namespace RestApi.Tests;

public sealed class TelemetryStreamEntryParserTests
{
  [Fact]
  public void TryParse_parses_valid_fields()
  {
    var parser = new TelemetryStreamEntryParser();
    var fields = new[]
    {
      new NameValueEntry("sensorId", "sensor-01"),
      new NameValueEntry("tsUtc", "2026-05-10T12:00:00.0000000+00:00"),
      new NameValueEntry("value", "12.34")
    };

    var ok = parser.TryParse(fields, out var msg);

    Assert.True(ok);
    Assert.Equal("sensor-01", msg.SensorId);
    Assert.Equal(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero), msg.TimestampUtc);
    Assert.Equal(12.34, msg.Value, 3);
  }

  [Fact]
  public void TryParse_rejects_missing_fields()
  {
    var parser = new TelemetryStreamEntryParser();
    var ok = parser.TryParse(new[] { new NameValueEntry("sensorId", "sensor-01") }, out _);
    Assert.False(ok);
  }
}

