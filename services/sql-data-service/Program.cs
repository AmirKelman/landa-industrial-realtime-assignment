using Grpc.Core;
using Landa.SqlData.Contracts;
using Microsoft.EntityFrameworkCore;
using Landa.SqlDataService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddDbContext<SqlDataDbContext>(options =>
{
  var cs = builder.Configuration.GetConnectionString("SqlServer");
  if (string.IsNullOrWhiteSpace(cs))
  {
    throw new InvalidOperationException("Missing connection string: ConnectionStrings:SqlServer");
  }
  options.UseSqlServer(cs);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
  var db = scope.ServiceProvider.GetRequiredService<SqlDataDbContext>();
  await db.Database.EnsureCreatedAsync();
  await SeedSensorsAsync(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "sql-data-service" }));
app.MapGrpcService<SqlDataGrpcService>();

app.Run();

static async Task SeedSensorsAsync(SqlDataDbContext db)
{
  if (await db.Sensors.AnyAsync())
    return;

  for (var i = 1; i <= 20; i++)
  {
    db.Sensors.Add(new SensorEntity
    {
      Id = $"sensor-{i:00}",
      DisplayName = $"Sensor {i:00}",
      Location = $"Zone-{((i - 1) / 5) + 1}"
    });
  }
  await db.SaveChangesAsync();
}

public sealed class SqlDataGrpcService : Landa.SqlData.Contracts.SqlDataService.SqlDataServiceBase
{
  private readonly SqlDataDbContext _db;

  public SqlDataGrpcService(SqlDataDbContext db)
  {
    _db = db;
  }

  public override async Task<ListSensorsResponse> ListSensors(ListSensorsRequest request, ServerCallContext context)
  {
    var sensors = await _db.Sensors.AsNoTracking().OrderBy(s => s.Id).ToListAsync(context.CancellationToken);
    var resp = new ListSensorsResponse();
    resp.Sensors.AddRange(sensors.Select(s => new Sensor
    {
      Id = s.Id,
      DisplayName = s.DisplayName,
      Location = s.Location ?? string.Empty
    }));
    return resp;
  }

  public override async Task<UpsertSensorsResponse> UpsertSensors(UpsertSensorsRequest request, ServerCallContext context)
  {
    var upserted = 0;

    foreach (var s in request.Sensors)
    {
      if (string.IsNullOrWhiteSpace(s.Id))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "sensor.id is required"));
      }
      if (string.IsNullOrWhiteSpace(s.DisplayName))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, $"sensor.display_name is required for {s.Id}"));
      }

      var existing = await _db.Sensors.SingleOrDefaultAsync(x => x.Id == s.Id, context.CancellationToken);
      if (existing is null)
      {
        _db.Sensors.Add(new SensorEntity
        {
          Id = s.Id.Trim(),
          DisplayName = s.DisplayName.Trim(),
          Location = string.IsNullOrWhiteSpace(s.Location) ? null : s.Location.Trim()
        });
        upserted++;
        continue;
      }

      existing.DisplayName = s.DisplayName.Trim();
      existing.Location = string.IsNullOrWhiteSpace(s.Location) ? null : s.Location.Trim();
      upserted++;
    }

    await _db.SaveChangesAsync(context.CancellationToken);
    return new UpsertSensorsResponse { UpsertedCount = upserted };
  }
}

