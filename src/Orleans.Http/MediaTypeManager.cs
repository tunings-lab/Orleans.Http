using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Http.Abstractions;

namespace Orleans.Http;

internal sealed class MediaTypeManager
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, IMediaTypeHandler> _handlers;

    public MediaTypeManager(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<MediaTypeManager>();
        _handlers = new Dictionary<string, IMediaTypeHandler>(StringComparer.OrdinalIgnoreCase);

        var handlers = serviceProvider.GetServices<IMediaTypeHandler>();
        if (handlers is not null)
        {
            foreach (var handler in handlers)
            {
                foreach (var mediaType in handler.MediaTypes)
                {
                    _handlers[mediaType] = handler;
                }
            }
        }

        if (_handlers.Count == 0)
        {
            _logger.LogWarning("No IMediaTypeHandlers registered — request bodies will be ignored.");
        }
    }

    public async ValueTask<bool> Serialize(string? mediaType, object? obj, PipeWriter writer)
    {
        if (string.IsNullOrEmpty(mediaType)) return false;

        if (_handlers.TryGetValue(mediaType, out var handler))
        {
            try
            {
                await handler.Serialize(obj, writer);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize body as '{MediaType}' using {HandlerType}: {Message}", mediaType, handler.GetType().FullName, ex.Message);
            }
        }

        return false;
    }

    public ValueTask<object?> Deserialize(string? mediaType, PipeReader reader, Type type, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(mediaType)) return default;

        if (_handlers.TryGetValue(mediaType, out var handler))
        {
            try
            {
                return handler.Deserialize(reader, type, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize body as '{MediaType}': {Message}", mediaType, ex.Message);
            }
        }

        return default;
    }
}
