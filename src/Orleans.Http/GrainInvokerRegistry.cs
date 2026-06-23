using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Orleans.Http.Abstractions;

namespace Orleans.Http;

/// <summary>
/// Default implementation of <see cref="IGrainInvokerRegistry"/> using concurrent dictionaries.
/// Source-generated code calls RegisterInvoker/RegisterResultExtractor at type initialization
/// via module initializers, which access the static <see cref="StaticInstance"/>.
/// </summary>
internal sealed class GrainInvokerRegistry : IGrainInvokerRegistry
{
    /// <summary>
    /// Static instance used by module initializers before the DI container is built.
    /// Set by <c>AddGrainRouter</c> and also accessible to source-generated code.
    /// </summary>
    internal static readonly GrainInvokerRegistry StaticInstance = new();

    private readonly ConcurrentDictionary<(Type, string), Func<IGrain, object?[], Task>> _invokers = new();
    private readonly ConcurrentDictionary<(Type, string), Func<Task, object?>> _extractors = new();

    public bool TryGetInvoker(Type grainInterfaceType, string methodName, out Func<IGrain, object?[], Task>? invoker)
    {
        return _invokers.TryGetValue((grainInterfaceType, methodName), out invoker!);
    }

    public bool TryGetResultExtractor(Type grainInterfaceType, string methodName, out Func<Task, object?>? getResult)
    {
        return _extractors.TryGetValue((grainInterfaceType, methodName), out getResult!);
    }

    public void RegisterInvoker(Type grainInterfaceType, string methodName, Func<IGrain, object?[], Task> invoker)
    {
        _invokers[(grainInterfaceType, methodName)] = invoker;
    }

    public void RegisterResultExtractor(Type grainInterfaceType, string methodName, Func<Task, object?> getResult)
    {
        _extractors[(grainInterfaceType, methodName)] = getResult;
    }
}
