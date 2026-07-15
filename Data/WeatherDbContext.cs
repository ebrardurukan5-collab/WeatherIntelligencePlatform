using Microsoft.EntityFrameworkCore;
using WeatherIntelligencePlatform.Models;

namespace WeatherIntelligencePlatform.Data;

public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

    public DbSet<WeatherQueryLog> WeatherQueryLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<WeatherQueryLog>()
            .Property(x => x.Id)
            .ValueGeneratedOnAdd();
    }
}