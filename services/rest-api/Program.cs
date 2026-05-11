using Grpc.Net.Client;
using Landa.SqlData.Contracts;
using RabbitMQ.Client;
using RestApi;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
  options.AddPolicy("ui", p =>
    p
      .WithOrigins("http://localhost:5173")
      .AllowAnyHeader()
      .AllowAnyMethod()
  );
});

builder.Services.AddSignalR();

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

builder.Services.AddSingleton(_ =>
{
  var address = builder.Configuration["Grpc:SqlDataServiceAddress"] ?? builder.Configuration["Grpc__SqlDataServiceAddress"];
  if (string.IsNullOrWhiteSpace(address))
    throw new InvalidOperationException("Missing gRPC address (Grpc:SqlDataServiceAddress / Grpc__SqlDataServiceAddress).");
  return GrpcChannel.ForAddress(address);
});

builder.Services.AddSingleton(sp =>
  new SqlDataService.SqlDataServiceClient(sp.GetRequiredService<GrpcChannel>()));

builder.Services.AddSingleton<TelemetryStreamEntryParser>();
builder.Services.AddHostedService<RedisTelemetryStreamWorker>();
builder.Services.AddHostedService<RabbitMqSensorStatusConsumer>();

var app = builder.Build();

app.UseCors("ui");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "rest-api" }));
app.MapHub<TelemetryHub>("/hubs/telemetry");

app.MapGet("/sensors", async (SqlDataService.SqlDataServiceClient grpc) =>
{
  var response = await grpc.ListSensorsAsync(new ListSensorsRequest());
  return Results.Ok(response.Sensors);
});

app.Run();
