using Xunit;
using TelemetryService;

namespace TelemetryService.Tests;

public sealed class SensorSimulatorTests
{
  [Fact]
  public void GenerateTick_requires_exactly_20_sensors()
  {
    var sim = new SensorSimulator();
    Assert.Throws<ArgumentOutOfRangeException>(() => sim.GenerateTick(DateTimeOffset.UtcNow, 19));
    Assert.Throws<ArgumentOutOfRangeException>(() => sim.GenerateTick(DateTimeOffset.UtcNow, 21));
  }

  [Fact]
  public void GenerateTick_returns_20_sensor_readings_with_expected_ids()
  {
    var sim = new SensorSimulator();
    var now = new DateTimeOffset(2026, 05, 10, 12, 0, 0, TimeSpan.Zero);
    var readings = sim.GenerateTick(now, 20);

    Assert.Equal(20, readings.Count);
    Assert.Equal("sensor-01", readings[0].SensorId);
    Assert.Equal("sensor-20", readings[19].SensorId);
    Assert.All(readings, r => Assert.Equal(now, r.TimestampUtc));
  }
}

