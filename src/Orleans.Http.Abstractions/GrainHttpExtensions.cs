using System.Net;
using Orleans;

namespace Orleans;

/// <summary>
/// Represents an HTTP result returned from a grain method,
/// allowing grains to set status codes, headers, and body content.
/// </summary>
public interface IGrainHttpResult
{
    Dictionary<string, string>? ResponseHeaders { get; }
    HttpStatusCode StatusCode { get; }
    object? Body { get; }
}

/// <summary>Typed variant of <see cref="IGrainHttpResult"/> for strong-typing the body.</summary>
public interface IGrainHttpResult<TResult> : IGrainHttpResult { }

/// <summary>Default implementation of <see cref="IGrainHttpResult{TResult}"/>.</summary>
[GenerateSerializer]
public sealed class GrainHttpResult<TResult> : IGrainHttpResult<TResult>
{
    [Id(0)]
    public Dictionary<string, string>? ResponseHeaders { get; set; }

    [Id(1)]
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    [Id(2)]
    public object? Body { get; set; }
}

/// <summary>
/// Extension methods for grains to produce <see cref="IGrainHttpResult"/> responses,
/// similar to ASP.NET Core controller helper methods (Ok, Created, NotFound, etc).
/// </summary>
public static class GrainHttpExtensions
{
    public static IGrainHttpResult<TResult> Ok<TResult>(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = default, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.OK };

    public static IGrainHttpResult<TResult> Ok<TResult>(this Grain grain, TResult body, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = body, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.OK };

    public static IGrainHttpResult<TResult> Created<TResult>(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = default, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Created };

    public static IGrainHttpResult<TResult> Created<TResult>(this Grain grain, TResult body, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = body, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Created };

    public static IGrainHttpResult<TResult> Accepted<TResult>(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = default, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Accepted };

    public static IGrainHttpResult<TResult> Accepted<TResult>(this Grain grain, TResult body, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = body, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Accepted };

    public static IGrainHttpResult<TResult> Conflict<TResult>(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = default, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Conflict };

    public static IGrainHttpResult<TResult> Conflict<TResult>(this Grain grain, TResult body, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = body, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Conflict };

    public static IGrainHttpResult<TResult> BadRequest<TResult>(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = default, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.BadRequest };

    public static IGrainHttpResult<TResult> BadRequest<TResult>(this Grain grain, TResult body, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = body, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.BadRequest };

    public static IGrainHttpResult<TResult> Unauthorized<TResult>(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = default, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Unauthorized };

    public static IGrainHttpResult<TResult> Unauthorized<TResult>(this Grain grain, TResult body, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = body, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Unauthorized };

    public static IGrainHttpResult<TResult> Forbidden<TResult>(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = default, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Forbidden };

    public static IGrainHttpResult<TResult> Forbidden<TResult>(this Grain grain, TResult body, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<TResult> { Body = body, ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.Forbidden };

    public static IGrainHttpResult NotFound(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<object> { ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.NotFound };

    public static IGrainHttpResult NoContent(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<object> { ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.NoContent };

    public static IGrainHttpResult NotAcceptable(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<object> { ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.NotAcceptable };

    public static IGrainHttpResult NotImplemented(this Grain grain, Dictionary<string, string>? responseHeaders = null)
        => new GrainHttpResult<object> { ResponseHeaders = responseHeaders, StatusCode = HttpStatusCode.NotImplemented };
}
