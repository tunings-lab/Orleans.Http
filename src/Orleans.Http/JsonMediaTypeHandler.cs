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
        while (!cancellationToken.IsCancellationRequested)
        {
            var readResult = await reader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;

            if (buffer.IsEmpty && readResult.IsCompleted)
                return null;

            // Read all bytes from the buffer into a stream
            using var stream = new MemoryStream();
            foreach (var segment in buffer)
            {
                stream.Write(segment.Span);
            }
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync(stream, type, _options, cancellationToken);

            reader.AdvanceTo(buffer.End);

            if (readResult.IsCompleted) return result;
        }

        return null;
    }

    public async ValueTask Serialize(object? obj, PipeWriter writer)
    {
        if (obj is null) return;
        await JsonSerializer.SerializeAsync(writer.AsStream(), obj, obj.GetType(), _options);
    }
}
