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
  {
    throw new InvalidOperationException("Missing Redis connection string (Redis:ConnectionString / Redis__ConnectionString).");
  }
  return ConnectionMultiplexer.Connect(cs);
});

builder.Services.AddSingleton<TelemetryStreamEntryParser>();
builder.Services.AddHostedService<RedisTelemetryStreamWorker>();

var app = builder.Build();

app.UseCors("ui");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "rest-api" }));
app.MapHub<TelemetryHub>("/hubs/telemetry");

app.Run();

