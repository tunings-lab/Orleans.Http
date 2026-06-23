using Microsoft.AspNetCore.Routing.Patterns;

namespace Orleans.Http;

internal static class RoutePatternBuilder
{
    private const string GrainIdToken = "{grainId}";

    public static RoutePattern BuildRoutePattern(
        string prefix,
        string topLevelPattern,
        string grainTypeName,
        string methodName,
        string routeAttributePattern)
    {
        // If the user defined a pattern, use it
        if (!string.IsNullOrWhiteSpace(routeAttributePattern))
        {
            if (routeAttributePattern.StartsWith('/'))
            {
                return RoutePatternFactory.Parse(routeAttributePattern);
            }

            return RoutePatternFactory.Parse($"{prefix}{topLevelPattern}{routeAttributePattern}");
        }

        // Otherwise use the default: prefix/topLevel/grainType/{grainId}/methodName
        return RoutePatternFactory.Parse($"{prefix}{topLevelPattern}{grainTypeName}/{{grainId}}/{methodName}");
    }
}
