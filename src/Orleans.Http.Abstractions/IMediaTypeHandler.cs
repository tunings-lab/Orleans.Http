using System.IO.Pipelines;

namespace Orleans.Http.Abstractions;

/// <summary>
/// Handles serialization/deserialization for a specific media type (e.g. application/json).
/// Implementations are registered via DI and selected by Content-Type/Accept headers.
/// </summary>
public interface IMediaTypeHandler
{
    /// <summary>The media types this handler supports (e.g. "application/json").</summary>
    string[] MediaTypes { get; }

    /// <summary>Serialize an object to the response body writer.</summary>
    ValueTask Serialize(object? obj, PipeWriter writer);

    /// <summary>Deserialize the request body reader to the target type.</summary>
    ValueTask<object?> Deserialize(PipeReader reader, Type type, CancellationToken cancellationToken);
}
