using ProtoBuf;
using System.Text.Json.Serialization;
using Orleans.Concurrency;
using Orleans.Http.Abstractions;
using Orleans;

namespace Orleans.Http.Test;

/// <summary>
/// Test grain interface exercising all routing features: default routes, custom patterns,
/// HTTP verbs, body/query/route parameters, auth, custom results, and route grain providers.
/// </summary>
[Route("Test")]
public interface ITestGrain : IGrainWithGuidKey
{
    Task PrivateMethod();

    [HttpGet]
    Task Get();

    [HttpPost]
    Task Post();

    [HttpGet("{grainId}/Get2")]
    Task<string> Get2();

    [HttpPost("Post2")]
    Task Post2();

    [HttpGet("{grainId}/Get3/{hello}")]
    Task<string> Get3(string hello);

    [HttpPost("Post3")]
    Task Post3(TestPayload body);

    [HttpGet("Get4")]
    Task<string> Get4([FromQuery] string hello);

    [HttpPost]
    Task Post4([FromBody] TestPayload body);

    [HttpGet("Get5")]
    Task<Immutable<string>> Get5([FromQuery] Immutable<string> hello);

    [HttpPost("Post5")]
    Task Post5([FromBody] Immutable<TestPayload> body);

    [HttpPost("Post6")]
    Task Post6([FromQuery] Immutable<string> hello, [FromBody] Immutable<TestPayload> body);

    [HttpPost]
    Task<TestPayload> Post7([FromBody] TestPayload body);

    [Authorize]
    [HttpGet]
    Task<AuthResponse> GetWithAuth();

    [Authorize(Roles = "admin")]
    [HttpGet]
    Task<AuthResponse> GetWithAuthAdmin();

    [HttpGet("{grainId}/GetCustom")]
    Task<IGrainHttpResult<TestPayload>> GetCustomStatus();

    [HttpGet("{grainId}/SameUrl")]
    Task SameUrlGet();

    [HttpPost("{grainId}/SameUrl")]
    Task SameUrlPost();

    [HttpGet("{grainId}/SameUrlAndMethod")]
    [HttpPost("{grainId}/SameUrlAndMethod")]
    Task SameUrlAndMethod();

    [HttpGet(pattern: "Get6", routeGrainProviderPolicy: nameof(RandomGuidRouteGrainProvider))]
    Task Get6();

    [HttpGet(pattern: "Get7", routeGrainProviderPolicy: nameof(FailingRouteGrainProvider))]
    Task Get7();

    [HttpGet(pattern: "Get8")]
    Task Get8();

    [HttpPost("{grainId}/FormTest")]
    Task FormTest([FromBody] Dictionary<string, string> payload);

    [HttpPost("{grainId}/JsonTest")]
    Task<TestPayload> JsonTest([FromBody] TestPayload payload);

    [HttpPatch("{grainId}/PatchTest")]
    Task<string> PatchTest();
}

[ProtoContract]
[GenerateSerializer]
public class TestPayload
{
    [ProtoMember(1)]
    [JsonPropertyName("number")]
    [Id(0)]
    public int Number { get; set; }

    [ProtoMember(2)]
    [JsonPropertyName("text")]
    [Id(1)]
    public string? Text { get; set; }
}

[ProtoContract]
[GenerateSerializer]
public class AuthResponse
{
    [ProtoMember(1)]
    [Id(0)]
    public string? User { get; set; }

    [ProtoMember(2)]
    [Id(1)]
    public string? Role { get; set; }
}
