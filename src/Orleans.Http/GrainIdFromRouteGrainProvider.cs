using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Orleans.Http.Abstractions;

namespace Orleans.Http;

/// <summary>
/// Default <see cref="IRouteGrainProvider"/> that resolves grains by extracting the grain ID
/// from the route's {grainId} value, inferring the key type from the grain interface.
/// </summary>
public class GrainIdFromRouteGrainProvider : IRouteGrainProvider
{
    private enum GrainIdType
    {
        Guid = 0,
        String = 1,
        Integer = 2,
        GuidCompound = 3,
        IntegerCompound = 4
    }

    private static readonly ValueTask<IGrain?> NullGrain = default;

    private readonly IClusterClient _clusterClient;
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GrainIdFromRouteGrainProvider(IClusterClient clusterClient, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor)
    {
        _clusterClient = clusterClient;
        _logger = loggerFactory.CreateLogger<GrainIdFromRouteGrainProvider>();
        _httpContextAccessor = httpContextAccessor;
    }

    public ValueTask<IGrain?> GetGrain(Type grainType)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            throw new InvalidOperationException("No HttpContext available.");
        }

        var endpoint = (RouteEndpoint?)context.GetEndpoint();
        var pattern = endpoint?.RoutePattern;

        try
        {
            if (context.Request.RouteValues.TryGetValue(Constants.GrainId, out var grainIdParameter) && grainIdParameter is not null)
            {
                var grainIdType = GetGrainIdType(grainType);
                var grainIdExtensionParameter = context.Request.RouteValues.TryGetValue(Constants.GrainIdExtension, out var ext) ? ext : null;

                IGrain grain = grainIdType switch
                {
                    GrainIdType.String => _clusterClient.GetGrain(grainType, (string)grainIdParameter),
                    GrainIdType.Integer => _clusterClient.GetGrain(grainType, Convert.ToInt64(grainIdParameter)),
                    GrainIdType.IntegerCompound => _clusterClient.GetGrain(grainType, Convert.ToInt64(grainIdParameter), (string?)grainIdExtensionParameter ?? string.Empty),
                    GrainIdType.GuidCompound => _clusterClient.GetGrain(grainType, Guid.Parse((string)grainIdParameter), (string?)grainIdExtensionParameter ?? string.Empty),
                    _ => _clusterClient.GetGrain(grainType, Guid.Parse((string)grainIdParameter))
                };

                return new ValueTask<IGrain?>(grain);
            }

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return NullGrain;
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            _logger.LogError(ex, "Failed to get grain '{GrainType}' for route '{Pattern}': {Message}", grainType.FullName, pattern?.RawText, ex.Message);
            return NullGrain;
        }
    }

    private static GrainIdType GetGrainIdType(Type grainInterfaceType)
    {
        var ifaces = grainInterfaceType.GetInterfaces();

        if (ifaces.Contains(typeof(IGrainWithGuidKey)))
            return GrainIdType.Guid;

        if (ifaces.Contains(typeof(IGrainWithGuidCompoundKey)))
            return GrainIdType.GuidCompound;

        if (ifaces.Contains(typeof(IGrainWithIntegerKey)))
            return GrainIdType.Integer;

        if (ifaces.Contains(typeof(IGrainWithIntegerCompoundKey)))
            return GrainIdType.IntegerCompound;

        return GrainIdType.String;
    }
}
