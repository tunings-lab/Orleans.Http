using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Http.Abstractions;
using ASPNetAuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
using ASPNetAllowAnonymousAttribute = Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute;

namespace Orleans.Http;

public static class HostingExtensions
{
    private static readonly Type[] HttpMethodAttributeTypes =
    [
        typeof(HttpGetAttribute),
        typeof(HttpPostAttribute),
        typeof(HttpPutAttribute),
        typeof(HttpDeleteAttribute),
        typeof(HttpPatchAttribute)
    ];

    private static readonly Type RouteAttributeType = typeof(RouteAttribute);
    private static readonly Type AuthorizeAttributeType = typeof(AuthorizeAttribute);
    private static readonly Type AllowAnonymousAttributeType = typeof(AllowAnonymousAttribute);

    /// <summary>
    /// Registers the GrainRouter and supporting services in the DI container.
    /// </summary>
    public static IServiceCollection AddGrainRouter(this IServiceCollection services)
    {
        return services
            .AddSingleton<MediaTypeManager>()
            .AddSingleton<GrainRouter>()
            .AddSingleton<RouteGrainProviderFactory>()
            .AddSingleton<IRouteGrainProviderPolicyBuilder>(sp => sp.GetRequiredService<RouteGrainProviderFactory>());
    }

    /// <summary>Register the JSON media type handler with optional configuration.</summary>
    public static IServiceCollection AddJsonMediaType(this IServiceCollection services, Action<JsonSerializerOptions>? configure = null)
    {
        var options = new JsonSerializerOptions();
        if (configure is null)
        {
            options.PropertyNameCaseInsensitive = true;
            options.AllowTrailingCommas = true;
        }
        else
        {
            configure(options);
        }

        return services.AddSingleton<IMediaTypeHandler>(sp => new JsonMediaTypeHandler(options));
    }

    /// <summary>Register the XML media type handler.</summary>
    public static IServiceCollection AddXmlMediaType(this IServiceCollection services)
    {
        return services.AddSingleton<IMediaTypeHandler, XmlMediaTypeHandler>();
    }

    /// <summary>Register the Forms (url-encoded) media type handler.</summary>
    public static IServiceCollection AddFormsMediaType(this IServiceCollection services)
    {
        return services.AddSingleton<IMediaTypeHandler, FormsMediaTypeHandler>();
    }

    /// <summary>
    /// Configure custom route grain provider policies.
    /// </summary>
    public static IApplicationBuilder UseRouteGrainProviders(
        this IApplicationBuilder app,
        Action<IRouteGrainProviderPolicyBuilder> configure)
    {
        var builder = app.ApplicationServices.GetRequiredService<IRouteGrainProviderPolicyBuilder>();
        configure?.Invoke(builder);
        return app;
    }

    /// <summary>
    /// Maps all grain interfaces decorated with routing attributes as HTTP endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <param name="prefix">An optional prefix for all grain routes (e.g. "grains").</param>
    public static IEndpointRouteBuilder MapGrains(this IEndpointRouteBuilder routes, string? prefix = null)
    {
        prefix = string.IsNullOrWhiteSpace(prefix) ? "/" : $"{prefix.Trim('/')}/";

        var sp = routes.ServiceProvider;
        var dispatcher = sp.GetRequiredService<GrainRouter>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<GrainRouter>();
        var grainTypeOptions = sp.GetRequiredService<IOptions<GrainTypeOptions>>().Value;

        logger.LogInformation("Scanning grain types for HTTP route mapping...");
        var grainTypesToMap = DiscoverGrainTypesToMap(grainTypeOptions, logger);

        int routesCreated = 0;
        foreach (var grainType in grainTypesToMap)
        {
            routesCreated += MapGrainToRoute(routes, grainType, prefix, dispatcher, logger);
        }

        logger.LogInformation("{Count} route(s) created for grains.", routesCreated);
        return routes;
    }

    private static int MapGrainToRoute(
        IEndpointRouteBuilder routes,
        Type grainType,
        string prefix,
        GrainRouter dispatcher,
        ILogger logger)
    {
        logger.LogInformation("Mapping routes for grain '{GrainType}'...", grainType.FullName);

        var topRouteAttr = grainType.GetCustomAttributes(true)
            .FirstOrDefault(attr => attr.GetType() == RouteAttributeType) as RouteAttribute;
        var topAuthorizeAttr = grainType.GetCustomAttributes(true)
            .FirstOrDefault(attr => attr.GetType() == AuthorizeAttributeType) as AuthorizeAttribute;
        var topAllowAnonymousAttr = grainType.GetCustomAttributes(true)
            .FirstOrDefault(attr => attr.GetType() == AllowAnonymousAttributeType) as AllowAnonymousAttribute;

        string topLevelPattern = string.Empty;
        if (!string.IsNullOrWhiteSpace(topRouteAttr?.Pattern))
        {
            topLevelPattern = $"{topRouteAttr.Pattern}/";
        }

        var methods = grainType.GetMethods().Where(m => m.GetCustomAttributes(true)
            .Any(attr => attr.GetType() == RouteAttributeType || HttpMethodAttributeTypes.Contains(attr.GetType()))).ToArray();

        int routesRegistered = 0;

        foreach (var method in methods)
        {
            var methodAttributes = method.GetCustomAttributes(true)
                .Where(attr => attr.GetType() == RouteAttributeType ||
                               attr.GetType() == AuthorizeAttributeType ||
                               attr.GetType() == AllowAnonymousAttributeType ||
                               HttpMethodAttributeTypes.Contains(attr.GetType()))
                .ToArray();

            foreach (var attribute in methodAttributes)
            {
                RoutePattern? routePattern = null;
                var httpMethod = string.Empty;
                string? routeGrainProviderPolicy = null;
                Func<string, RequestDelegate, IEndpointConventionBuilder>? mapFunc = null;

                if (attribute is RouteAttribute routeAttr)
                {
                    routePattern = RoutePatternBuilder.BuildRoutePattern(prefix, topLevelPattern, grainType.FullName!, method.Name, routeAttr.Pattern);
                    routeGrainProviderPolicy = routeAttr.RouteGrainProviderPolicy;
                    httpMethod = "*";
                    mapFunc = (pattern, handler) => routes.MapMethods(pattern, [httpMethod], handler);
                }
                else if (attribute is HttpMethodAttribute methodAttr)
                {
                    routePattern = RoutePatternBuilder.BuildRoutePattern(prefix, topLevelPattern, grainType.FullName!, method.Name, methodAttr.Pattern);
                    routeGrainProviderPolicy = methodAttr.RouteGrainProviderPolicy;
                    httpMethod = methodAttr.HttpMethod;
                    mapFunc = (pattern, handler) => routes.MapMethods(pattern, [httpMethod], handler);
                }

                if (routePattern is null)
                {
                    logger.LogWarning("Cannot create route pattern for '{GrainType}.{MethodName}' — skipping.", grainType.FullName, method.Name);
                    continue;
                }

                if (!dispatcher.RegisterRoute(routePattern.RawText!, httpMethod, method, routeGrainProviderPolicy))
                {
                    throw new InvalidOperationException($"Duplicate route pattern '{routePattern.RawText}'. A route with this pattern already exists.");
                }

                var routeBuilder = mapFunc!(routePattern.RawText ?? string.Empty, dispatcher.Dispatch);

                // Apply authorization
                var methodAuthorizeAttr = methodAttributes.FirstOrDefault(a => a is AuthorizeAttribute) as AuthorizeAttribute;
                var methodAllowAnonymousAttr = methodAttributes.FirstOrDefault(a => a is AllowAnonymousAttribute) as AllowAnonymousAttribute;

                if (methodAllowAnonymousAttr is not null || topAllowAnonymousAttr is not null)
                {
                    routeBuilder.AllowAnonymous();
                }
                else if (methodAuthorizeAttr is not null)
                {
                    routeBuilder.RequireAuthorization(new ASPNetAuthorizeAttribute(methodAuthorizeAttr.Policy ?? string.Empty)
                    {
                        Roles = methodAuthorizeAttr.Roles,
                        AuthenticationSchemes = methodAuthorizeAttr.AuthenticationSchemes
                    });
                }
                else if (topAuthorizeAttr is not null)
                {
                    routeBuilder.RequireAuthorization(new ASPNetAuthorizeAttribute(topAuthorizeAttr.Policy ?? string.Empty)
                    {
                        Roles = topAuthorizeAttr.Roles,
                        AuthenticationSchemes = topAuthorizeAttr.AuthenticationSchemes
                    });
                }

                logger.LogInformation("[{Method}] [{GrainType}.{MethodName}] -> {Pattern}", httpMethod, grainType.FullName, method.Name, routePattern.RawText);
                routesRegistered++;
            }
        }

        logger.LogInformation("{Count} route(s) created for grain '{GrainType}'.", routesRegistered, grainType.FullName);
        return routesRegistered;
    }

    private static List<Type> DiscoverGrainTypesToMap(GrainTypeOptions grainTypeOptions, ILogger logger)
    {
        var grainTypesToMap = new List<Type>();

        foreach (var grainType in grainTypeOptions.Interfaces)
        {
            var hasTopLevelRoute = grainType.GetCustomAttributes(true).Any(attr => attr.GetType() == RouteAttributeType);
            var hasMethodRoute = grainType.GetMethods().Any(m => m.GetCustomAttributes(true)
                .Any(attr => attr.GetType() == RouteAttributeType || HttpMethodAttributeTypes.Contains(attr.GetType())));

            if (hasTopLevelRoute || hasMethodRoute)
            {
                grainTypesToMap.Add(grainType);
            }
        }

        return grainTypesToMap;
    }
}
