namespace Orleans.Http.Abstractions;

/// <summary>
/// Builder for registering named route grain provider policies.
/// Used via <c>UseRouteGrainProviders(builder => ...)</c> on <c>IApplicationBuilder</c>.
/// </summary>
public interface IRouteGrainProviderPolicyBuilder
{
    /// <summary>Register a custom <see cref="IRouteGrainProvider"/> under a policy name.</summary>
    IRouteGrainProviderPolicyBuilder RegisterRouteGrainProvider<T>(string policyName) where T : IRouteGrainProvider;

    /// <summary>Set the default policy name, used when no explicit policy is specified on a route.</summary>
    IRouteGrainProviderPolicyBuilder SetDefaultRouteGrainProviderPolicy(string policyName);
}
