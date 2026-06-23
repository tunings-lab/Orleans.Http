# Design Spec: Orleans.Http v10

## Overview
Ground-up rewrite of OrleansContrib/Orleans.Http (v3.0) targeting .NET 10 + Orleans 10.

## Reference Analysis (v3.0)
- **Architecture**: Attribute-based routing on grain interface methods → ASP.NET Core endpoint routing
- **Core Types**:
  - `GrainRouter` — route registry + dispatch
  - `GrainInvoker` — parameter binding + grain method invocation + response serialization
  - `HostingExtensions` — `MapGrains()`, `AddGrainRouter()`, media type registration
  - `GrainIdFromRouteGrainProvider` — default `{grainId}` → grain resolution
  - `MediaTypeManager` — Content-Type → `IMediaTypeHandler` lookup
- **v3 Limitations**:
  - Uses removed `IApplicationPartManager`/`GrainInterfaceFeature` API
  - `MethodInfo.Invoke` reflection (no source generators)
  - Manual type parsing switch (no `TypeConverter`/`Bindable.Parse`)
  - No PATCH support
  - No `[AllowAnonymous]`
  - Startup-class pattern (deprecated in .NET 10)

## v10 Design Decisions

### API Changes
- `GrainTypeOptions` replaces `IApplicationPartManager` + `GrainInterfaceFeature`
  - Injected via `IOptions<GrainTypeOptions>` from DI
  - `GrainTypeOptions.Interfaces` is a `HashSet<Type>` of grain interfaces
- `UseOrleans` on `IHostBuilder` (no `ConfigureApplicationParts` needed — Orleans 10 auto-discovers)
- `WebApplicationBuilder` minimal hosting replaces `HostBuilder` + `Startup`
- `MapMethods` replaces verb-specific `MapGet`/`MapPost` etc. for unified route building

### New Features
- `[HttpPatch]` attribute
- `[AllowAnonymous]` attribute
- `[FromRoute]` explicit parameter attribute
- Nullable reference types throughout
- Central package management (CPM)
- Source-link + deterministic builds

### Deferred / Future
- Source generator-based grain invocation (avoid `MethodInfo.Invoke`)
- `IResult` integration (ASP.NET Core minimal APIs)
- OpenAPI/Swagger generation from grain routes
- Async streaming responses (`IAsyncEnumerable<T>`)
- Rate limiting integration
- Problem Details (RFC 9457) error responses
