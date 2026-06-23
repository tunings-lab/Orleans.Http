using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using Orleans.Http.Abstractions;

namespace Orleans.Http;

internal sealed class FormsMediaTypeHandler : IMediaTypeHandler
{
    private static readonly Type DictionaryType = typeof(Dictionary<string, string>);

    public string[] MediaTypes => ["application/x-www-form-urlencoded"];

    public async ValueTask<object?> Deserialize(PipeReader reader, Type type, CancellationToken cancellationToken)
    {
        if (type != DictionaryType) return null;

        var formsReader = new FormPipeReader(reader);
        var form = await formsReader.ReadFormAsync(cancellationToken);

        var model = new Dictionary<string, string>(form.Count);
        foreach (var kv in form)
        {
            model[kv.Key] = kv.Value!;
        }

        return model;
    }

    public async ValueTask Serialize(object? obj, PipeWriter writer)
    {
        if (obj is not Dictionary<string, string> dict) return;

        var content = new FormUrlEncodedContent(dict);
        await writer.WriteAsync((await content.ReadAsByteArrayAsync()).AsMemory());
    }
}
