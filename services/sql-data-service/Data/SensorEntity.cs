namespace Landa.SqlDataService.Data;

public sealed class SensorEntity
{
  public required string Id { get; set; }
  public required string DisplayName { get; set; }
  public string? Location { get; set; }
}

