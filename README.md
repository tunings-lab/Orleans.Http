# Orleans.Http

<p align="center">
  <h1>Orleans HTTP Endpoints for .NET 10</h1>
</p>

[![Build](https://github.com/tunings-lab/Orleans.Http/actions/workflows/ci.yml/badge.svg)](https://github.com/tunings-lab/Orleans.Http/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Orleans.Http** exposes Orleans grains as HTTP endpoints via ASP.NET Core endpoint routing — no controllers required. Grain methods decorated with `[HttpGet]`, `[HttpPost]`, etc. attributes are automatically mapped to HTTP routes at startup.

This is a ground-up rewrite of the [original OrleansContrib/Orleans.Http](https://github.com/OrleansContrib/Orleans.Http) (v3.0, .NET Core 3.0) targeting **.NET 10** and **Orleans 10**.

## What's New in v10

- **.NET 10 / Orleans 10.2+**: Targets `net10.0`, uses `GrainTypeOptions` instead of the removed `IApplicationPartManager`/`GrainInterfaceFeature`
- **C# 14 / Modern .NET**: Nullable reference types, implicit usings, collection expressions, `required` members
- **Minimal Hosting**: Uses `WebApplicationBuilder` instead of the legacy `IHostBuilder`/`Startup` pattern
- **HTTP PATCH support**: Added `[HttpPatch]` attribute
- **`[AllowAnonymous]` attribute**: Explicitly allow anonymous access, overriding class-level `[Authorize]`
- **`[FromRoute]` attribute**: Explicit route parameter binding (in addition to `[FromBody]` and `[FromQuery]`)
- **Thread-safe route registration**: `ConcurrentDictionary`-backed router
- **Central Package Management**: `Directory.Packages.props` with pinned versions
- **Nullable throughout**: All APIs are nullability-annotated
- **Source-link & deterministic builds**: Ready for reproducible NuGet packaging

## Packages

| Package | Description |
|---|---|
| `Orleans.Http.Abstractions` | Attributes and interfaces for grain interfaces |
| `Orleans.Http` | Core routing + JSON/XML/Forms media type handlers |
| `Orleans.Http.MediaTypes.Protobuf` | Protobuf media type handler (protobuf-net) |

## Quick Start

### 1. Grain Interface (Contracts project)

```csharp
using Orleans.Http.Abstractions;

[Route("users")]
public interface IUserGrain : IGrainWithStringKey
{
    [HttpGet("{grainId}")]
    Task<User> GetUser();

    [HttpPost("{grainId}")]
    Task<User> CreateUser([FromBody] CreateUserRequest request);

    [HttpPatch("{grainId}")]
    Task<User> UpdateUser([FromBody] UpdateUserRequest request);

    [HttpDelete("{grainId}")]
    Task DeleteUser();
}
```

### 2. Silo Host (minimal hosting)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(silo => silo.UseLocalhostClustering());

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddAuthentication().AddJwtBearer(/* ... */);
builder.Services.AddAuthorization();

builder.Services
    .AddGrainRouter()
    .AddJsonMediaType()
    .AddProtobufMediaType();  // optional

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrains("api");  // prefix: /api/...

app.Run();
```

## Routing

Routes are generated from attributes on grain interface methods, similar to ASP.NET Core attribute routing.

### Default Pattern

If no `Pattern` is specified on the attribute:
```
{prefix}/{topLevelPattern}/{grainTypeName}/{grainId}/{methodName}
```

### Custom Patterns

```csharp
[HttpGet("{grainId}/profile")]        // relative to prefix/topLevel
[HttpGet("/users/{grainId}/profile")] // absolute (ignores prefix)
```

### Parameter Binding

| Attribute | Source | Types |
|---|---|---|
| `[FromBody]` | Request body (deserialized via `IMediaTypeHandler`) | Any |
| `[FromQuery]` | Query string | Primitives, enums, Guid, DateTime |
| `[FromRoute]` | Route values | Primitives, enums, Guid, DateTime |
| *(none)* | Route values (default) | Primitives |

### HTTP Result Helpers

Grains can return `IGrainHttpResult<T>` to control status codes, headers, and body:

```csharp
public Task<IGrainHttpResult<User>> GetUser()
{
    return Task.FromResult(this.Ok(user));
    // or: this.Created(user), this.NotFound(), this.BadRequest(errors), etc.
}
```

### Custom Grain Resolution

By default, grains are resolved by extracting `{grainId}` from the route. Custom providers allow any strategy:

```csharp
app.UseRouteGrainProviders(providers =>
{
    providers.RegisterRouteGrainProvider<TenantGrainProvider>("Tenant");
    providers.SetDefaultRouteGrainProviderPolicy("Tenant");
});

// Per-route override:
[HttpGet("dashboard", routeGrainProviderPolicy: "Tenant")]
Task<Dashboard> GetDashboard();
```

### Custom Media Types

Implement `IMediaTypeHandler` and register via DI:

```csharp
builder.Services.AddSingleton<IMediaTypeHandler, MyCustomMediaTypeHandler>();
```

## Authentication / Authorization

Add `[Authorize]` to grain interfaces or methods. It maps to ASP.NET Core's authorization pipeline:

```csharp
[Authorize]
public interface ISecureGrain : IGrainWithGuidKey
{
    [Authorize(Roles = "admin")]
    [HttpGet("admin")]
    Task AdminOnly();

    [AllowAnonymous]
    [HttpGet("public")]
    Task PublicEndpoint();
}
```

## Project Structure

```
Orleans.Http/
├── src/
│   ├── Orleans.Http.Abstractions/      # Attributes, interfaces, IGrainHttpResult
│   ├── Orleans.Http/                    # GrainRouter, GrainInvoker, HostingExtensions
│   └── Orleans.Http.MediaTypes.Protobuf/ # Protobuf support
├── test/
│   └── Orleans.Http.Test/              # xUnit integration tests
├── Directory.Build.props               # Shared build properties
├── Directory.Packages.props            # Central package management
└── global.json                          # .NET 10 SDK pin
```

## Development

```bash
dotnet build
dotnet test
```

## License

MIT — forked from [OrleansContrib/Orleans.Http](https://github.com/OrleansContrib/Orleans.Http) (originally by Gutemberg Ribeiro).
