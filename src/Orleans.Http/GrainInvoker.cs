using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Orleans.Http.Abstractions;

// Bring SetHttpUser/ClearHttpUser into scope
using static Orleans.GrainHttpContextExtensions;

namespace Orleans.Http;

internal sealed class GrainInvoker
{
    private static readonly Type[] ParameterAttributeTypes = [typeof(FromBodyAttribute), typeof(FromQueryAttribute), typeof(FromRouteAttribute)];
    private static readonly MethodInfo GetResultMethod = typeof(GrainInvoker)
        .GetMethod(nameof(GetResultCore), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly Type TaskOfTType = typeof(Task<>);
    private static readonly Type GrainHttpResultType = typeof(IGrainHttpResult);

    private readonly Dictionary<string, ParameterInfo> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private readonly MethodInfo _methodInfo;
    private readonly ILogger _logger;
    private readonly MediaTypeManager _mediaTypeManager;
    private readonly RouteGrainProviderFactory _routeGrainProviderFactory;
    private readonly string? _routeGrainProviderPolicy;
    private MethodInfo? _getResult;
    private bool _isIGrainHttpResultType;

    public Type GrainType => _methodInfo.DeclaringType!;

    public IRouteGrainProvider RouteGrainProvider
    {
        get
        {
            if (string.IsNullOrEmpty(_routeGrainProviderPolicy))
            {
                return _routeGrainProviderFactory.CreateDefault();
            }
            return _routeGrainProviderFactory.Create(_routeGrainProviderPolicy!);
        }
    }

    public GrainInvoker(IServiceProvider serviceProvider, MethodInfo methodInfo, string? routeGrainProviderPolicy)
    {
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<GrainInvoker>();
        _methodInfo = methodInfo;
        _mediaTypeManager = serviceProvider.GetRequiredService<MediaTypeManager>();
        _routeGrainProviderFactory = serviceProvider.GetRequiredService<RouteGrainProviderFactory>();
        _routeGrainProviderPolicy = routeGrainProviderPolicy;

        BuildResultDelegate();
        BuildParameterMap();
    }

    public async Task Invoke(IGrain grain, HttpContext context)
    {
        // Propagate HTTP user identity to the grain via RequestContext
        // so grains can access claims without HttpContextAccessor (which
        // doesn't flow across the Orleans scheduler boundary).
        SetHttpUser(context.User);

        try
        {
            var parameters = await GetParameters(context);
            var grainCall = (Task)_methodInfo.Invoke(grain, parameters)!;
            await grainCall;

        if (_getResult is not null)
        {
            var result = _getResult.Invoke(null, [grainCall]);

            if (result is not null)
            {
                string? contentType = null;

                if (context.Request.Headers.TryGetValue("Accept", out var acceptVal))
                {
                    contentType = acceptVal.FirstOrDefault();
                }

                contentType ??= context.Request.ContentType;

                context.Response.ContentType = contentType ?? "application/json";

                if (_isIGrainHttpResultType && result is IGrainHttpResult httpResult)
                {
                    context.Response.StatusCode = (int)httpResult.StatusCode;

                    if (httpResult.ResponseHeaders is { Count: > 0 })
                    {
                        foreach (var header in httpResult.ResponseHeaders)
                        {
                            context.Response.Headers[header.Key] = header.Value;
                        }
                    }

                    if (httpResult.Body is not null)
                    {
                        var serialized = await _mediaTypeManager.Serialize(contentType, httpResult.Body, context.Response.BodyWriter);
                        if (!serialized)
                        {
                            await context.Response.WriteAsync(httpResult.Body.ToString() ?? string.Empty);
                        }
                    }
                }
                else
                {
                    var serialized = await _mediaTypeManager.Serialize(contentType, result, context.Response.BodyWriter);
                    if (!serialized)
                    {
                        await context.Response.WriteAsync(result.ToString() ?? string.Empty);
                    }
                }
            }
        }
        }
        finally
        {
            ClearHttpUser();
        }
    }

    private async ValueTask<object?[]> GetParameters(HttpContext context)
    {
        if (_parameters.Count == 0) return [];

        var parameterValues = new object?[_parameters.Count];
        var routeParameters = context.Request.RouteValues;

        for (int i = 0; i < _parameters.Count; i++)
        {
            var param = _parameters.ElementAt(i);
            var paramName = param.Key;
            var paramInfo = param.Value;

            if (paramInfo.Source == ParameterSource.Body)
            {
                parameterValues[i] = await ParseBodyParameter(paramInfo, context);
            }
            else if (paramInfo.Source == ParameterSource.Route &&
                     routeParameters.TryGetValue(paramName, out var routeParam))
            {
                parameterValues[i] = ConvertType(routeParam?.ToString(), paramInfo.Type);
            }
            else if (paramInfo.Source == ParameterSource.Query &&
                     context.Request.Query.TryGetValue(paramName, out var query) &&
                     query.Count > 0)
            {
                var queryValue = query.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(queryValue))
                {
                    parameterValues[i] = ConvertType(queryValue, paramInfo.Type);
                }
            }
            else if (paramInfo.Source == ParameterSource.Route &&
                     context.Request.Query.TryGetValue(paramName, out var queryRoute) &&
                     queryRoute.Count > 0)
            {
                // Fallback: parameters without explicit attributes default to Route,
                // but we also check query string as a convenience
                var queryValue = queryRoute.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(queryValue))
                {
                    parameterValues[i] = ConvertType(queryValue, paramInfo.Type);
                }
            }
        }

        return parameterValues;
    }

    private async ValueTask<object?> ParseBodyParameter(ParameterInfo paramInfo, HttpContext context)
    {
        try
        {
            return await _mediaTypeManager.Deserialize(
                context.Request.ContentType,
                context.Request.BodyReader,
                paramInfo.Type,
                context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to deserialize parameter '{ParamName}' from request body: {Message}", paramInfo.Name, ex.Message);
            return null;
        }
    }

    private static object? ConvertType(string? value, Type targetType)
    {
        if (string.IsNullOrEmpty(value)) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string)) return value;
        if (underlying == typeof(int)) return int.TryParse(value, out var i) ? i : 0;
        if (underlying == typeof(long)) return long.TryParse(value, out var l) ? l : 0L;
        if (underlying == typeof(decimal)) return decimal.TryParse(value, out var d) ? d : 0m;
        if (underlying == typeof(double)) return double.TryParse(value, out var dd) ? dd : 0.0;
        if (underlying == typeof(float)) return float.TryParse(value, out var f) ? f : 0f;
        if (underlying == typeof(byte)) return byte.TryParse(value, out var b) ? b : (byte)0;
        if (underlying == typeof(bool)) return bool.TryParse(value, out var bl) ? bl : false;
        if (underlying == typeof(Guid)) return Guid.TryParse(value, out var g) ? g : Guid.Empty;
        if (underlying == typeof(DateTime)) return DateTime.TryParse(value, out var dt) ? dt : DateTime.MinValue;
        if (underlying == typeof(DateTimeOffset)) return DateTimeOffset.TryParse(value, out var dto) ? dto : DateTimeOffset.MinValue;
        if (underlying == typeof(char)) return char.TryParse(value, out var c) ? c : '\0';
        if (underlying.IsEnum) return Enum.TryParse(underlying, value, out var enumVal) ? enumVal : Activator.CreateInstance(underlying);

        return value;
    }

    private void BuildParameterMap()
    {
        var methodParams = _methodInfo.GetParameters();
        var hasBody = false;

        foreach (var methodParameter in methodParams)
        {
            if (methodParameter.Name == Constants.GrainId || methodParameter.Name == Constants.GrainIdExtension)
                continue;

            var attribute = methodParameter.GetCustomAttributes()
                .FirstOrDefault(attr => ParameterAttributeTypes.Contains(attr.GetType()));

            ParameterSource source;

            if (attribute is FromBodyAttribute)
            {
                if (hasBody) throw new InvalidOperationException("A method can only have one [FromBody] parameter.");
                source = ParameterSource.Body;
                hasBody = true;
            }
            else if (attribute is FromQueryAttribute)
            {
                source = ParameterSource.Query;
            }
            else if (attribute is FromRouteAttribute)
            {
                source = ParameterSource.Route;
            }
            else
            {
                // Default: route for simple types
                source = ParameterSource.Route;
            }

            _parameters[methodParameter.Name!] = new ParameterInfo(methodParameter.Name!, methodParameter.ParameterType, source);
        }
    }

    private void BuildResultDelegate()
    {
        if (_methodInfo.ReturnType.IsGenericType &&
            _methodInfo.ReturnType.GetGenericTypeDefinition() == TaskOfTType)
        {
            var returnType = _methodInfo.ReturnType.GenericTypeArguments[0];

            if (returnType == GrainHttpResultType || returnType.GetInterfaces().Any(i => i == GrainHttpResultType))
            {
                _isIGrainHttpResultType = true;
            }

            _getResult = GetResultMethod.MakeGenericMethod(returnType);
        }
    }

    private static object GetResultCore<T>(Task<T> input) => input.GetAwaiter().GetResult()!;

    private sealed record ParameterInfo(string Name, Type Type, ParameterSource Source);

    private enum ParameterSource
    {
        Body = 0,
        Query = 1,
        Route = 2
    }
}
