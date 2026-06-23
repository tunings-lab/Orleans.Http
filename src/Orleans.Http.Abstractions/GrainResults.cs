using System.Net;
using Orleans.Http.Abstractions;

namespace Orleans;

/// <summary>
/// Convenience factory for creating IGrainHttpResult responses from grains,
/// mirroring the ASP.NET Core Results.* API surface.
/// 
/// Unlike Microsoft.AspNetCore.Http.Results.* (which returns IResult instances
/// that cannot cross the Orleans serialization boundary), these methods return
/// IGrainHttpResult instances that are [GenerateSerializer]-annotated and
/// fully Orleans-serializable.
/// </summary>
public static class GrainResults
{
    public static IGrainHttpResult<TResult> Ok<TResult>(TResult body)
        => new GrainHttpResult<TResult> { Body = body, StatusCode = HttpStatusCode.OK };

    public static IGrainHttpResult<TResult> Created<TResult>(TResult body)
        => new GrainHttpResult<TResult> { Body = body, StatusCode = HttpStatusCode.Created };

    public static IGrainHttpResult<TResult> Accepted<TResult>(TResult body)
        => new GrainHttpResult<TResult> { Body = body, StatusCode = HttpStatusCode.Accepted };

    public static IGrainHttpResult<TResult> NotFound<TResult>(TResult body)
        => new GrainHttpResult<TResult> { Body = body, StatusCode = HttpStatusCode.NotFound };

    public static IGrainHttpResult NotFound()
        => new GrainHttpResult<object> { StatusCode = HttpStatusCode.NotFound };

    public static IGrainHttpResult NoContent()
        => new GrainHttpResult<object> { StatusCode = HttpStatusCode.NoContent };

    public static IGrainHttpResult<TResult> BadRequest<TResult>(TResult body)
        => new GrainHttpResult<TResult> { Body = body, StatusCode = HttpStatusCode.BadRequest };

    public static IGrainHttpResult BadRequest(string? message)
        => new GrainHttpResult<object> { Body = message, StatusCode = HttpStatusCode.BadRequest };

    public static IGrainHttpResult Conflict(string? message)
        => new GrainHttpResult<object> { Body = message, StatusCode = HttpStatusCode.Conflict };

    public static IGrainHttpResult Unauthorized()
        => new GrainHttpResult<object> { StatusCode = HttpStatusCode.Unauthorized };

    public static IGrainHttpResult Forbidden()
        => new GrainHttpResult<object> { StatusCode = HttpStatusCode.Forbidden };
}
