using System.Security.Claims;
using Orleans.Concurrency;
using Orleans.Http.Abstractions;

namespace Orleans.Http.Test;

public class TestGrain : Grain, ITestGrain
{
    public TestGrain() { }

    public Task Get() => Task.CompletedTask;

    public Task<string> Get2() => Task.FromResult("Get2");

    public Task<string> Get3(string hello) => Task.FromResult(hello);

    public Task<string> Get4([FromQuery] string hello) => Task.FromResult(hello);

    public Task<Immutable<string>> Get5([FromQuery] Immutable<string> hello) => Task.FromResult(hello.Value.AsImmutable());

    public Task PrivateMethod() => throw new NotSupportedException();

    public Task Post() => Task.CompletedTask;

    public Task Post2() => Task.CompletedTask;

    public Task Post3(TestPayload body) => Task.FromResult(body);

    public Task Post4([FromBody] TestPayload body) => Task.FromResult(body);

    public Task Post5([FromBody] Immutable<TestPayload> body) => Task.FromResult(body.Value.AsImmutable());

    public Task Post6([FromQuery] Immutable<string> hello, [FromBody] Immutable<TestPayload> body)
    {
        if (string.IsNullOrWhiteSpace(hello.Value)) throw new ArgumentNullException(nameof(hello));
        if (body.Value == null) throw new ArgumentNullException(nameof(body));
        return Task.CompletedTask;
    }

    public Task<TestPayload> Post7([FromBody] TestPayload body) => Task.FromResult(body);

    public Task<AuthResponse> GetWithAuth()
    {
        var user = this.GetHttpUser();
        return Task.FromResult(new AuthResponse
        {
            User = user.FindFirst(ClaimTypes.Name)?.Value,
            Role = user.FindFirst(ClaimTypes.Role)?.Value
        });
    }

    public Task<IGrainHttpResult<TestPayload>> GetCustomStatus()
    {
        return Task.FromResult(
            this.Created(
                new TestPayload { Number = 1, Text = "IGrainHttpResult" },
                new Dictionary<string, string> { { "CustomHeader", "HeaderValue" } }
            )
        );
    }

    public Task<AuthResponse> GetWithAuthAdmin()
    {
        var user = this.GetHttpUser();
        return Task.FromResult(new AuthResponse
        {
            User = user.FindFirst(ClaimTypes.Name)?.Value,
            Role = user.FindFirst(ClaimTypes.Role)?.Value
        });
    }

    public Task SameUrlGet() => Task.CompletedTask;

    public Task SameUrlPost() => Task.CompletedTask;

    public Task SameUrlAndMethod() => Task.CompletedTask;

    public Task Get6() => Task.CompletedTask;

    public Task Get7() => Task.CompletedTask;

    public Task Get8() => Task.CompletedTask;

    public Task FormTest(Dictionary<string, string> payload)
    {
        if (payload != null && payload.Count == 1) return Task.CompletedTask;
        throw new ArgumentException(nameof(payload));
    }

    public Task<TestPayload> JsonTest(TestPayload payload)
    {
        if (payload != null && payload.Number == 12340000 && payload.Text == "Test text")
        {
            return Task.FromResult(payload);
        }
        throw new ArgumentException(nameof(payload));
    }

    public Task<string> PatchTest() => Task.FromResult("Patched");

    public Task<IGrainHttpResult<TestPayload>> IResultLikeTest()
    {
        return Task.FromResult(GrainResults.Ok(new TestPayload { Number = 42, Text = "GrainResults" }));
    }

    public Task<IGrainHttpResult<string>> IResultLikeNotFoundTest()
    {
        return Task.FromResult(GrainResults.NotFound("Not found via GrainResults"));
    }
}

public class RandomGuidRouteGrainProvider : IRouteGrainProvider
{
    private readonly IClusterClient _clusterClient;

    public RandomGuidRouteGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public ValueTask<IGrain?> GetGrain(Type grainType)
    {
        return new ValueTask<IGrain?>(_clusterClient.GetGrain(grainType, Guid.NewGuid()));
    }
}

public class FailingRouteGrainProvider : IRouteGrainProvider
{
    public ValueTask<IGrain?> GetGrain(Type grainType)
    {
        IGrain? nullResult = null;
        return new ValueTask<IGrain?>(nullResult);
    }
}
