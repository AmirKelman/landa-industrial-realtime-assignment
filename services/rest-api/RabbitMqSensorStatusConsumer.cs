using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RestApi;

public sealed class RabbitMqSensorStatusConsumer : BackgroundService
{
  private const string ExchangeName = "landa.events";
  private const string RoutingKey = "sensor.status.changed";
  private const string QueueName = "rest-api.sensor-status";

  private readonly IConnectionFactory _factory;
  private readonly IHubContext<TelemetryHub> _hub;
  private readonly ILogger<RabbitMqSensorStatusConsumer> _logger;

  public RabbitMqSensorStatusConsumer(
    IConnectionFactory factory,
    IHubContext<TelemetryHub> hub,
    ILogger<RabbitMqSensorStatusConsumer> logger)
  {
    _factory = factory;
    _hub = hub;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    for (var attempt = 0; !stoppingToken.IsCancellationRequested; attempt++)
    {
      try
      {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(QueueName, ExchangeName, RoutingKey);
        channel.BasicQos(0, 50, false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, ea) =>
        {
          var json = Encoding.UTF8.GetString(ea.Body.ToArray());
          _logger.LogInformation("SensorStatusChanged received: {Json}", json);
          _ = _hub.Clients.All.SendAsync("sensorStatus", json, stoppingToken);
          channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("RabbitMQ consumer started on queue {Queue}", QueueName);

        // Hold the connection open until the host stops.
        await Task.Delay(Timeout.Infinite, stoppingToken);
        return;
      }
      catch (OperationCanceledException)
      {
        return;
      }
      catch (Exception ex)
      {
        var delay = Math.Min(10_000, 1_000 * (int)Math.Pow(2, Math.Min(attempt, 6)));
        _logger.LogWarning(
          "RabbitMQ not ready (attempt {Attempt}), retrying in {Delay}ms: {Message}",
          attempt + 1, delay, ex.Message);
        await Task.Delay(delay, stoppingToken);
      }
    }
  }
}
