using Microsoft.Recognizers.Text.NumberWithUnit;
using Remora.Rest.Core;

namespace Rhodium.Data;

/// <summary>
/// Represents data for a user's configuration.
/// </summary>
public class RhodiumUserConfig
{
    public Snowflake ID { get; set; }

    public RhodiumTemperature PreferredTemperatureUnit { get; set; }

    public RhodiumMeasurement PreferredMeasurementUnit { get; set; }

}
