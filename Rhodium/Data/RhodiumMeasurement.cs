using System.ComponentModel;

namespace Rhodium.Data;

/// <summary>
/// Represents a measurement unit.
/// </summary>
public enum RhodiumMeasurement
{
    [Description("Imperial (US) [ft, in, lb]")]
    Imperial,

    [Description("Metric [m, cm, kg]")]
    Metric,

}
