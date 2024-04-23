using JetBrains.Annotations;
using Remora.Results;

namespace PalladiumUtils;

/// <summary>
/// A helper class for creating results.
/// </summary>
[PublicAPI]
public static class ResultHelper
{
    /// <summary>
    /// Returns a successful <see cref="Result{T}"/> with the given value.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <returns></returns>
    public static Result<T> Successful<T>(T value) => Result<T>.FromSuccess(value);
    
    public static Result<T> NotFound<T>(string? message = null, T? value = default) 
        => Result<T>.FromError(new NotFoundError(message ?? "The searched-for entity was not found."));
    
    public static Result NotFound(string? message = null)
        => Result.FromError(new NotFoundError(message ?? "The searched-for entity was not found."));
    
}