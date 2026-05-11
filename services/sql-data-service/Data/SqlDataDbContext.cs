using Microsoft.EntityFrameworkCore;

namespace Landa.SqlDataService.Data;

public sealed class SqlDataDbContext : DbContext
{
  public SqlDataDbContext(DbContextOptions<SqlDataDbContext> options) : base(options) { }

  public DbSet<SensorEntity> Sensors => Set<SensorEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<SensorEntity>(b =>
    {
      b.ToTable("Sensors");
      b.HasKey(x => x.Id);

      b.Property(x => x.Id)
        .HasMaxLength(64)
        .IsRequired();

      b.Property(x => x.DisplayName)
        .HasMaxLength(200)
        .IsRequired();

      b.Property(x => x.Location)
        .HasMaxLength(200);
    });
  }
}

