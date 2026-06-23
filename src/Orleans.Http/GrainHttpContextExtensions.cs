using System.Security.Claims;
using Orleans.Runtime;
using Orleans.Http.Abstractions;

namespace Orleans;

/// <summary>
/// Extension methods for accessing HTTP request context data from within grains.
/// Uses Orleans RequestContext to propagate data across the grain call boundary.
/// </summary>
public static class GrainHttpContextExtensions
{
    private const string UserKey = "Orleans.Http.User";

    /// <summary>
    /// Gets the ClaimsPrincipal from the current grain call's HTTP context.
    /// Returns an unauthenticated principal if no HTTP context was propagated.
    /// </summary>
    public static ClaimsPrincipal GetHttpUser(this Grain grain)
    {
        if (RequestContext.Get(UserKey) is GrainHttpUser httpUser)
        {
            return httpUser.ToClaimsPrincipal();
        }

        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    /// <summary>
    /// Sets the HTTP user in the RequestContext for propagation to grains.
    /// Called by the GrainInvoker before invoking the grain method.
    /// </summary>
    internal static void SetHttpUser(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated == true)
        {
            RequestContext.Set(UserKey, GrainHttpUser.FromClaimsPrincipal(principal));
        }
    }

    /// <summary>
    /// Clears the HTTP user from RequestContext after the grain call completes.
    /// </summary>
    internal static void ClearHttpUser()
    {
        RequestContext.Remove(UserKey);
    }
}
