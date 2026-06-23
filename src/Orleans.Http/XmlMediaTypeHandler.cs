using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Xml.Serialization;
using Orleans.Http.Abstractions;

namespace Orleans.Http;

internal sealed class XmlMediaTypeHandler : IMediaTypeHandler
{
    private readonly ConcurrentDictionary<Type, XmlSerializer> _serializers = new();

    public string[] MediaTypes => ["application/xml", "text/xml"];

    public ValueTask<object?> Deserialize(PipeReader reader, Type type, CancellationToken cancellationToken)
    {
        XmlSerializer serializer = GetSerializer(type);
        var model = serializer.Deserialize(reader.AsStream());
        return new ValueTask<object?>(model);
    }

    public ValueTask Serialize(object? obj, PipeWriter writer)
    {
        if (obj is null) return default;
        var serializer = GetSerializer(obj.GetType());
        serializer.Serialize(writer.AsStream(), obj);
        return default;
    }

    private XmlSerializer GetSerializer(Type type)
    {
        return _serializers.GetOrAdd(type, t => new XmlSerializer(t));
    }
}
