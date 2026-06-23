using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Orleans.Http;

internal sealed class GrainRouter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, Dictionary<string, GrainInvoker>> _routes = new(StringComparer.OrdinalIgnoreCase);

    public GrainRouter(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<GrainRouter>();
    }

    public bool RegisterRoute(string pattern, string httpMethod, MethodInfo method, string? routeGrainProviderPolicy)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));

        var grainRoutes = _routes.GetOrAdd(pattern, _ => new Dictionary<string, GrainInvoker>(StringComparer.OrdinalIgnoreCase));

        lock (grainRoutes)
        {
            if (grainRoutes.ContainsKey(httpMethod)) return false;

            grainRoutes[httpMethod] = new GrainInvoker(_serviceProvider, method, routeGrainProviderPolicy);
        }

        return true;
    }

    public async Task Dispatch(HttpContext context)
    {
        var endpoint = (RouteEndpoint?)context.GetEndpoint();
        if (endpoint is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var pattern = endpoint.RoutePattern;

        if (!_routes.TryGetValue(pattern.RawText ?? string.Empty, out var allRoutes))
        {
            _logger.LogError("No routes registered for pattern '{Pattern}'", pattern.RawText);
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        if (!allRoutes.TryGetValue("*", out var invoker))
        {
            if (!allRoutes.TryGetValue(context.Request.Method, out invoker))
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return;
            }
        }

        IGrain? grain = null;
        var routeGrainProvider = invoker.RouteGrainProvider;

        try
        {
            grain = await routeGrainProvider.GetGrain(invoker.GrainType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting grain for route '{Pattern}'", pattern.RawText);
        }

        if (grain is null)
        {
            if (context.Response.StatusCode == (int)HttpStatusCode.OK)
            {
                _logger.LogError("Failure getting grain '{GrainType}' for route '{Pattern}' with RouteGrainProvider '{ProviderType}' — unhandled",
                    invoker.GrainType.FullName, pattern.RawText, routeGrainProvider.GetType());
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            return;
        }

        await invoker.Invoke(grain, context);
    }
}
