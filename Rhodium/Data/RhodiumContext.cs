using Microsoft.EntityFrameworkCore;
using PalladiumUtils;
using Remora.Rest.Core;

namespace Rhodium.Data;

public class RhodiumContext : DbContext
{
    public DbSet<RhodiumUserConfig> UserConfigs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=./rhodium.db");
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RhodiumContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Snowflake>().HaveConversion<SnowflakeConverter>();
        configurationBuilder.Properties<Snowflake?>().HaveConversion<SnowflakeNullableConverter>();
        base.ConfigureConventions(configurationBuilder);
    }
}
