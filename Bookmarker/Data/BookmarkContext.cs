using Microsoft.EntityFrameworkCore;
using PalladiumUtils;
using Remora.Rest.Core;

namespace Bookmarker.Data;

public class BookmarkContext : DbContext
{
    public DbSet<BookmarkEntity> Bookmarks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=bookmarks.db");
        base.OnConfiguring(optionsBuilder);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookmarkContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Snowflake>().HaveConversion<SnowflakeConverter>();
        configurationBuilder.Properties<Snowflake?>().HaveConversion<SnowflakeNullableConverter>();
        base.ConfigureConventions(configurationBuilder);
    }
}