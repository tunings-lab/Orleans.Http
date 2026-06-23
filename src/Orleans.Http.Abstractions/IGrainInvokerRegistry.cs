using System;
using System.Threading.Tasks;

namespace Orleans.Http.Abstractions;

/// <summary>
/// Registry for compiled grain invocation delegates, populated by source-generated code.
/// When available, GrainInvoker uses these delegates instead of MethodInfo.Invoke reflection.
/// </summary>
public interface IGrainInvokerRegistry
{
    /// <summary>
    /// Try to get a compiled invocation delegate for the specified grain interface type and method.
    /// </summary>
    /// <param name="grainInterfaceType">The grain interface type (e.g. ITestGrain)</param>
    /// <param name="methodName">The method name</param>
    /// <param name="invoker">The invocation delegate, or null if not found</param>
    /// <returns>True if a compiled delegate was found</returns>
    bool TryGetInvoker(Type grainInterfaceType, string methodName, out Func<IGrain, object?[], Task>? invoker);

    /// <summary>
    /// Try to get a compiled result extraction delegate for the specified grain interface type and method.
    /// </summary>
    /// <param name="grainInterfaceType">The grain interface type</param>
    /// <param name="methodName">The method name</param>
    /// <param name="getResult">The result extraction delegate, or null if not found</param>
    /// <returns>True if a compiled delegate was found</returns>
    bool TryGetResultExtractor(Type grainInterfaceType, string methodName, out Func<Task, object?>? getResult);

    /// <summary>
    /// Register a compiled invocation delegate.
    /// Called by source-generated initialization code.
    /// </summary>
    void RegisterInvoker(Type grainInterfaceType, string methodName, Func<IGrain, object?[], Task> invoker);

    /// <summary>
    /// Register a compiled result extraction delegate.
    /// Called by source-generated initialization code.
    /// </summary>
    void RegisterResultExtractor(Type grainInterfaceType, string methodName, Func<Task, object?> getResult);
}
