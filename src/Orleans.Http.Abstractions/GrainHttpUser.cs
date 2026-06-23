using System.Collections.Generic;
using System.Security.Claims;
using Orleans;

namespace Orleans.Http.Abstractions;

/// <summary>
/// Serializable representation of an HTTP request's user identity,
/// propagated to grains via Orleans RequestContext.
/// </summary>
[Serializable]
[GenerateSerializer]
public sealed class GrainHttpUser
{
    [Id(0)]
    public string? AuthenticationType { get; set; }

    [Id(1)]
    public string? Name { get; set; }

    [Id(2)]
    public List<GrainHttpClaim> Claims { get; set; } = [];

    /// <summary>
    /// Reconstructs a ClaimsPrincipal from this GrainHttpUser.
    /// </summary>
    public ClaimsPrincipal ToClaimsPrincipal()
    {
        var identity = new ClaimsIdentity(AuthenticationType);
        foreach (var c in Claims)
        {
            identity.AddClaim(new Claim(c.Type!, c.Value!, c.ValueType, c.Issuer, c.OriginalIssuer));
        }
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates a GrainHttpUser from a ClaimsPrincipal.
    /// </summary>
    public static GrainHttpUser FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        var user = new GrainHttpUser
        {
            AuthenticationType = principal.Identity?.AuthenticationType,
            Name = principal.Identity?.Name
        };

        foreach (var claim in principal.Claims)
        {
            user.Claims.Add(new GrainHttpClaim
            {
                Type = claim.Type,
                Value = claim.Value,
                ValueType = claim.ValueType,
                Issuer = claim.Issuer,
                OriginalIssuer = claim.OriginalIssuer
            });
        }

        return user;
    }
}

/// <summary>Serializable claim data.</summary>
[Serializable]
[GenerateSerializer]
public sealed class GrainHttpClaim
{
    [Id(0)]
    public string? Type { get; set; }

    [Id(1)]
    public string? Value { get; set; }

    [Id(2)]
    public string? ValueType { get; set; }

    [Id(3)]
    public string? Issuer { get; set; }

    [Id(4)]
    public string? OriginalIssuer { get; set; }
}
