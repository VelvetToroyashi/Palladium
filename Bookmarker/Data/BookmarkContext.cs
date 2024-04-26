using Microsoft.EntityFrameworkCore;
using PalladiumUtils;
using Remora.Rest.Core;

namespace Bookmarker.Data;

public class BookmarkContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<BookmarkEntity> Bookmarks { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<Snowflake>().HaveConversion<SnowflakeConverter>();
        configurationBuilder.Properties<Snowflake?>().HaveConversion<SnowflakeNullableConverter>();
    }
}