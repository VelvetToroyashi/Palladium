using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Remora.Discord.API;
using Remora.Rest.Core;

namespace PalladiumUtils;

public static class DatabaseConverterExtensions
{
}

public class SnowflakeConverter(ConverterMappingHints? mappingHints = null) : 
    ValueConverter<Snowflake, ulong>
    (
        id => id.Value,
        value => new Snowflake(value, Constants.DiscordEpoch),
        mappingHints
    );
    
public class SnowflakeNullableConverter(ConverterMappingHints? mappingHints = null) :
    ValueConverter<Snowflake?, ulong?>
    (
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new Snowflake(value.Value, Constants.DiscordEpoch) : null,
        mappingHints
    );