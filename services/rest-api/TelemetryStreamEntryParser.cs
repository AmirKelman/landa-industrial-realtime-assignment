using System.Globalization;
using StackExchange.Redis;

namespace RestApi;

public sealed class TelemetryStreamEntryParser
{
  public bool TryParse(NameValueEntry[] fields, out TelemetryMessage message)
  {
    string? sensorId = null;
    string? tsUtc = null;
    string? value = null;

    foreach (var f in fields)
    {
      var name = (string)f.Name!;
      var val = (string)f.Value!;
      if (name == "sensorId") sensorId = val;
      else if (name == "tsUtc") tsUtc = val;
      else if (name == "value") value = val;
    }

    if (string.IsNullOrWhiteSpace(sensorId) || string.IsNullOrWhiteSpace(tsUtc) || string.IsNullOrWhiteSpace(value))
    {
      message = default!;
      return false;
    }

    if (!DateTimeOffset.TryParse(tsUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ts))
    {
      message = default!;
      return false;
    }

    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
    {
      message = default!;
      return false;
    }

    message = new TelemetryMessage(sensorId, ts, v);
    return true;
  }
}

