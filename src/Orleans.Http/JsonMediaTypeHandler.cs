using System.IO.Pipelines;
using System.Text.Json;
using Orleans.Http.Abstractions;

namespace Orleans.Http;

internal sealed class JsonMediaTypeHandler : IMediaTypeHandler
{
    private readonly JsonSerializerOptions _options;

    public string[] MediaTypes => ["application/json; charset=utf-8", "application/json;charset=utf-8", "application/json"];

    public JsonMediaTypeHandler(JsonSerializerOptions options)
    {
        _options = options;
    }

    public async ValueTask<object?> Deserialize(PipeReader reader, Type type, CancellationToken cancellationToken)
    {
        // Read the entire body into a stream, then deserialize
        using var stream = new MemoryStream();
        await reader.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        if (stream.Length == 0)
            return null;

        return await JsonSerializer.DeserializeAsync(stream, type, _options, cancellationToken);
    }

    public async ValueTask Serialize(object? obj, PipeWriter writer)
    {
        if (obj is null) return;
        await JsonSerializer.SerializeAsync(writer.AsStream(), obj, obj.GetType(), _options);
    }
}
