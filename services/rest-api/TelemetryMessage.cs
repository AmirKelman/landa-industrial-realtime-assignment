namespace RestApi;

public sealed record TelemetryMessage(string SensorId, DateTimeOffset TimestampUtc, double Value);

