using Grpc.Core;
using Landa.SqlData.Contracts;
using Microsoft.EntityFrameworkCore;
using Landa.SqlDataService.Data;
using Xunit;

namespace Landa.SqlDataService.Tests;

public sealed class SqlDataGrpcServiceTests
{
  private static SqlDataDbContext CreateDb(string dbName)
  {
    var options = new DbContextOptionsBuilder<SqlDataDbContext>()
      .UseInMemoryDatabase(dbName)
      .Options;
    return new SqlDataDbContext(options);
  }

  [Fact]
  public async Task UpsertSensors_inserts_and_updates()
  {
    await using var db = CreateDb(nameof(UpsertSensors_inserts_and_updates));
    var svc = new SqlDataGrpcService(db);
    var ctx = TestServerCallContext.Create();

    var insertResp = await svc.UpsertSensors(new UpsertSensorsRequest
    {
      Sensors =
      {
        new Sensor { Id = "sensor-01", DisplayName = "S1", Location = "L1" },
        new Sensor { Id = "sensor-02", DisplayName = "S2", Location = "" }
      }
    }, ctx);

    Assert.Equal(2, insertResp.UpsertedCount);

    var updateResp = await svc.UpsertSensors(new UpsertSensorsRequest
    {
      Sensors =
      {
        new Sensor { Id = "sensor-02", DisplayName = "S2-updated", Location = "L2" }
      }
    }, ctx);

    Assert.Equal(1, updateResp.UpsertedCount);

    var list = await svc.ListSensors(new ListSensorsRequest(), ctx);
    Assert.Collection(list.Sensors,
      s =>
      {
        Assert.Equal("sensor-01", s.Id);
        Assert.Equal("S1", s.DisplayName);
        Assert.Equal("L1", s.Location);
      },
      s =>
      {
        Assert.Equal("sensor-02", s.Id);
        Assert.Equal("S2-updated", s.DisplayName);
        Assert.Equal("L2", s.Location);
      });
  }

  [Fact]
  public async Task UpsertSensors_rejects_missing_id()
  {
    await using var db = CreateDb(nameof(UpsertSensors_rejects_missing_id));
    var svc = new SqlDataGrpcService(db);
    var ctx = TestServerCallContext.Create();

    var ex = await Assert.ThrowsAsync<RpcException>(() => svc.UpsertSensors(new UpsertSensorsRequest
    {
      Sensors = { new Sensor { Id = "", DisplayName = "X" } }
    }, ctx));

    Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
  }
}

internal sealed class TestServerCallContext : ServerCallContext
{
  private readonly Metadata _requestHeaders = new();
  private readonly CancellationToken _cancellationToken = CancellationToken.None;
  private readonly Metadata _responseTrailers = new();
  private readonly AuthContext _authContext = new("test", new Dictionary<string, List<AuthProperty>>());

  public static TestServerCallContext Create() => new();

  protected override string MethodCore => "test";
  protected override string HostCore => "localhost";
  protected override string PeerCore => "peer";
  protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
  protected override Metadata RequestHeadersCore => _requestHeaders;
  protected override CancellationToken CancellationTokenCore => _cancellationToken;
  protected override Metadata ResponseTrailersCore => _responseTrailers;
  protected override Status StatusCore { get; set; }
  protected override WriteOptions? WriteOptionsCore { get; set; }
  protected override AuthContext AuthContextCore => _authContext;

  protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
    throw new NotSupportedException();
  protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}

