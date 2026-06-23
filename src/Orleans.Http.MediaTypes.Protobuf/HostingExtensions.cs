using Microsoft.Extensions.DependencyInjection;
using Orleans.Http.Abstractions;

namespace Orleans.Http.MediaTypes.Protobuf;

public static class HostingExtensions
{
    /// <summary>Register the Protobuf media type handler.</summary>
    public static IServiceCollection AddProtobufMediaType(this IServiceCollection services)
    {
        return services.AddSingleton<IMediaTypeHandler, ProtobufMediaTypeHandler>();
    }
}
