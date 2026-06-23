namespace Orleans.Http.Abstractions;

/// <summary>
/// Resolves the grain instance for an incoming HTTP request.
/// The default implementation extracts the grain ID from route values.
/// Custom implementations can resolve grains by any strategy (e.g. random, affinity).
/// </summary>
public interface IRouteGrainProvider
{
    /// <summary>Get the grain for the specified grain interface type.</summary>
    ValueTask<IGrain?> GetGrain(Type grainType);
}
