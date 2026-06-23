using Microsoft.Extensions.DependencyInjection;
using Orleans.Http.Abstractions;

namespace Orleans.Http;

internal sealed class RouteGrainProviderFactory : IRouteGrainProviderPolicyBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _routeGrainProviders = new();
    private string? _defaultPolicyName;

    public RouteGrainProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IRouteGrainProvider Create(string routeGrainProviderPolicy)
    {
        if (_routeGrainProviders.TryGetValue(routeGrainProviderPolicy, out var providerType))
        {
            return (IRouteGrainProvider)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, providerType);
        }

        throw new ArgumentException($"No RouteGrainProvider found for policy \"{routeGrainProviderPolicy}\"");
    }

    public IRouteGrainProvider CreateDefault()
    {
        if (string.IsNullOrEmpty(_defaultPolicyName))
        {
            return (IRouteGrainProvider)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, typeof(GrainIdFromRouteGrainProvider));
        }

        return Create(_defaultPolicyName);
    }

    public IRouteGrainProviderPolicyBuilder RegisterRouteGrainProvider<T>(string policyName) where T : IRouteGrainProvider
    {
        _routeGrainProviders[policyName] = typeof(T);
        return this;
    }

    public IRouteGrainProviderPolicyBuilder SetDefaultRouteGrainProviderPolicy(string policyName)
    {
        _defaultPolicyName = policyName;
        return this;
    }
}
