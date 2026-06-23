using System.IO.Pipelines;
using Orleans.Http.Abstractions;
using ProtoBuf;

namespace Orleans.Http.MediaTypes.Protobuf;

internal sealed class ProtobufMediaTypeHandler : IMediaTypeHandler
{
    public string[] MediaTypes => ["application/protobuf", "application/x-protobuf"];

    public ValueTask<object?> Deserialize(PipeReader reader, Type type, CancellationToken cancellationToken)
    {
        var model = Serializer.Deserialize(type, reader.AsStream());
        return new ValueTask<object?>(model);
    }

    public ValueTask Serialize(object? obj, PipeWriter writer)
    {
        if (obj is null) return default;
        Serializer.Serialize(writer.AsStream(), obj);
        return default;
    }
}
