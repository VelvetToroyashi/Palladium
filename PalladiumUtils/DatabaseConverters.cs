using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Remora.Discord.API;
using Remora.Rest.Core;

namespace PalladiumUtils;

public static class DatabaseConverterExtensions
{
}

public class SnowflakeConverter() : 
    ValueConverter<Snowflake, ulong>
    (
        id => id.Value,
        value => new Snowflake(value, Constants.DiscordEpoch)
    );
    
public class SnowflakeNullableConverter() :
    ValueConverter<Snowflake?, ulong?>
    (
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new Snowflake(value.Value, Constants.DiscordEpoch) : null
    );