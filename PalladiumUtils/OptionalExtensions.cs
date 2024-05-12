using JetBrains.Annotations;
using Remora.Rest.Core;

namespace PalladiumUtils;

[PublicAPI]
public static class OptionalExtensions
{
    /// <summary>
    /// Returns the value of the optional if it has one, otherwise returns the default value.
    /// </summary>
    /// <param name="optional">The optional to check.</param>
    /// <param name="defaultValue">A function returning the default value, which is lazily evaluated.</param>
    /// <typeparam name="T">The type of the optional.</typeparam>
    /// <returns>If the optional contained a value, the original optional,
    /// otherwise a new optional containing the return result of <paramref name="defaultValue"/>.</returns>
    public static T OrDefault<T>(this Optional<T> optional, Func<T> defaultValue)
        => optional.IsDefined(out var value) ? value : defaultValue();
    
}