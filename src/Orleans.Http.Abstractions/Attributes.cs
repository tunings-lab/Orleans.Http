namespace Orleans.Http.Abstractions;

/// <summary>
/// Base attribute for grain HTTP routing attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public abstract class GrainRouteAttributeBase : Attribute
{
    /// <summary>The route pattern. If empty, a default pattern is used.</summary>
    public string Pattern { get; }

    /// <summary>An optional friendly name for the route.</summary>
    public string Name { get; }

    /// <summary>An optional policy name for selecting a custom <see cref="IRouteGrainProvider"/>.</summary>
    public string RouteGrainProviderPolicy { get; }

    protected GrainRouteAttributeBase(string pattern = "", string name = "", string routeGrainProviderPolicy = "")
    {
        Pattern = pattern;
        Name = name;
        RouteGrainProviderPolicy = routeGrainProviderPolicy;
    }
}

/// <summary>
/// Applied to a grain interface or method to register an HTTP route.
/// When on an interface, the pattern is used as a prefix for all method routes on that grain.
/// When on a method, routes ALL HTTP verbs to that method.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RouteAttribute : GrainRouteAttributeBase
{
    public RouteAttribute(string pattern = "", string name = "", string routeGrainProviderPolicy = "")
        : base(pattern, name, routeGrainProviderPolicy) { }
}

/// <summary>
/// Base class for HTTP verb-specific attributes (HttpGet, HttpPost, etc).
/// </summary>
public abstract class HttpMethodAttribute : GrainRouteAttributeBase
{
    /// <summary>The HTTP method (GET, POST, PUT, DELETE, PATCH).</summary>
    public abstract string HttpMethod { get; }

    protected HttpMethodAttribute(string pattern = "", string name = "", string routeGrainProviderPolicy = "")
        : base(pattern, name, routeGrainProviderPolicy) { }
}

/// <summary>Routes GET requests to this grain method.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class HttpGetAttribute : HttpMethodAttribute
{
    public override string HttpMethod => "GET";
    public HttpGetAttribute(string pattern = "", string name = "", string routeGrainProviderPolicy = "")
        : base(pattern, name, routeGrainProviderPolicy) { }
}

/// <summary>Routes POST requests to this grain method.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class HttpPostAttribute : HttpMethodAttribute
{
    public override string HttpMethod => "POST";
    public HttpPostAttribute(string pattern = "", string name = "", string routeGrainProviderPolicy = "")
        : base(pattern, name, routeGrainProviderPolicy) { }
}

/// <summary>Routes PUT requests to this grain method.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class HttpPutAttribute : HttpMethodAttribute
{
    public override string HttpMethod => "PUT";
    public HttpPutAttribute(string pattern = "", string name = "", string routeGrainProviderPolicy = "")
        : base(pattern, name, routeGrainProviderPolicy) { }
}

/// <summary>Routes DELETE requests to this grain method.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class HttpDeleteAttribute : HttpMethodAttribute
{
    public override string HttpMethod => "DELETE";
    public HttpDeleteAttribute(string pattern = "", string name = "", string routeGrainProviderPolicy = "")
        : base(pattern, name, routeGrainProviderPolicy) { }
}

/// <summary>Routes PATCH requests to this grain method.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class HttpPatchAttribute : HttpMethodAttribute
{
    public override string HttpMethod => "PATCH";
    public HttpPatchAttribute(string pattern = "", string name = "", string routeGrainProviderPolicy = "")
        : base(pattern, name, routeGrainProviderPolicy) { }
}

/// <summary>Marks a grain method parameter as bound from the request body.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromBodyAttribute : Attribute { }

/// <summary>Marks a grain method parameter as bound from the query string.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromQueryAttribute : Attribute { }

/// <summary>Marks a grain method parameter as bound from the route.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromRouteAttribute : Attribute { }

/// <summary>
/// Applied to a grain interface or method to require authentication/authorization.
/// Maps to ASP.NET Core's authorization pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuthorizeAttribute : Attribute
{
    public AuthorizeAttribute() { }
    public AuthorizeAttribute(string policy) { Policy = policy; }
    public string? AuthenticationSchemes { get; set; }
    public string? Policy { get; set; }
    public string? Roles { get; set; }
}

/// <summary>
/// Applied to a grain interface or method to allow anonymous access,
/// overriding a class-level <see cref="AuthorizeAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AllowAnonymousAttribute : Attribute { }
