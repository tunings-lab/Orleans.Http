using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ProtoBuf;
using Xunit;

namespace Orleans.Http.Test;

public class HttpTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public HttpTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // No InitializeAsync/DisposeAsync needed — WebApplicationFactory manages lifecycle

    [Fact]
    public async Task RouteTest_MissingGrainId_ReturnsNotFound()
    {
        var url = "/grains";
        var response = await _client.GetWithAcceptAsync("application/json", url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RouteTest_MalformedGrainId_ReturnsBadRequest()
    {
        var url = "/grains/test/Orleans.Http.Test.ITestGrain/malformed-grain-id/get";
        var response = await _client.GetWithAcceptAsync("application/json", url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RouteTest_DefaultPattern_ReturnsOk()
    {
        var url = "/grains/test/Orleans.Http.Test.ITestGrain/00000000-0000-0000-0000-000000000000/get";
        var response = await _client.GetWithAcceptAsync("application/json", url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RouteTest_WrongMethod_ReturnsMethodNotAllowed()
    {
        var url = "/grains/test/Orleans.Http.Test.ITestGrain/00000000-0000-0000-0000-000000000000/get";
        var response = await _client.PostAsync(url, new StringContent(""));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task RouteTest_CustomPattern_ReturnsCreatedWithHeader()
    {
        var url = "/grains/test/00000000-0000-0000-0000-000000000000/GetCustom";
        var response = await _client.GetWithAcceptAsync("application/json", url);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.Contains("CustomHeader"));
        Assert.Equal("HeaderValue", response.Headers.GetValues("CustomHeader").First());

        var payload = await response.Content.ReadFromJsonAsync<TestPayload>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Number);
        Assert.Equal("IGrainHttpResult", payload.Text);
    }

    [Fact]
    public async Task RouteTest_SameUrl_DifferentMethods_BothOk()
    {
        var url = "/grains/test/00000000-0000-0000-0000-000000000000/SameUrl";

        var getResponse = await _client.GetWithAcceptAsync("application/json", url);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var postResponse = await _client.PostAsync(url, new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
    }

    [Fact]
    public async Task RouteTest_SameUrlAndMethod_BothVerbsOk()
    {
        var url = "/grains/test/00000000-0000-0000-0000-000000000000/SameUrlAndMethod";

        var getResponse = await _client.GetWithAcceptAsync("application/json", url);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var postResponse = await _client.PostAsync(url, new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
    }

    [Fact]
    public async Task RouteTest_RandomGuidProvider_ReturnsOk()
    {
        var url = "/grains/test/get6";
        var response = await _client.GetWithAcceptAsync("application/json", url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RouteTest_FailingProvider_ReturnsInternalServerError()
    {
        var url = "/grains/test/get7";
        var response = await _client.GetWithAcceptAsync("application/json", url);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task JsonTest_PostBody_ReturnsSameBody()
    {
        var payload = new TestPayload { Number = 12340000, Text = "Test text" };
        var url = "/grains/test/00000000-0000-0000-0000-000000000000/JsonTest";

        var response = await _client.PostAsJsonAsync(url, payload);
        Assert.True(response.IsSuccessStatusCode);

        var resp = await response.Content.ReadFromJsonAsync<TestPayload>();
        Assert.NotNull(resp);
        Assert.Equal(payload.Number, resp!.Number);
        Assert.Equal(payload.Text, resp.Text);
    }

    [Fact]
    public async Task FormsTest_PostForm_ReturnsOk()
    {
        var url = "/grains/test/00000000-0000-0000-0000-000000000000/FormTest";
        var dic = new Dictionary<string, string> { ["Test"] = "testing dic" };

        var response = await _client.PostAsync(url, new FormUrlEncodedContent(dic));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtobufTest_PostProtobuf_ReturnsProtobuf()
    {
        var payload = new TestPayload { Number = 12340000, Text = "Test text" };
        var url = "/grains/Test/Orleans.Http.Test.ITestGrain/00000000-0000-0000-0000-000000000000/post7";

        using var stream = new MemoryStream();
        Serializer.Serialize(stream, payload);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(stream.ToArray())
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/protobuf"));

        var response = await _client.SendAsync(request);
        var respStream = await response.Content.ReadAsStreamAsync();
        var respPayload = Serializer.Deserialize<TestPayload>(respStream);

        Assert.Equal(payload.Number, respPayload.Number);
        Assert.Equal(payload.Text, respPayload.Text);
    }

    [Fact]
    public async Task AuthTest_WithoutToken_ReturnsUnauthorized()
    {
        var url = "/grains/test/Orleans.Http.Test.ITestGrain/00000000-0000-0000-0000-000000000000/GetWithAuth";
        var response = await _client.GetWithAcceptAsync("application/json", url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthTest_WithUserToken_ReturnsOk()
    {
        var jwt = GenerateJwt(admin: false);
        var url = "/grains/test/Orleans.Http.Test.ITestGrain/00000000-0000-0000-0000-000000000000/GetWithAuth";
        var response = await _client.GetWithAcceptAsync("application/json", url, jwt);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResp = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResp);
        Assert.Equal("TestUser", authResp!.User);
        Assert.Equal("user", authResp.Role);
    }

    [Fact]
    public async Task AuthTest_UserTokenOnAdminEndpoint_ReturnsForbidden()
    {
        var jwt = GenerateJwt(admin: false);
        var url = "/grains/test/Orleans.Http.Test.ITestGrain/00000000-0000-0000-0000-000000000000/GetWithAuthAdmin";
        var response = await _client.GetWithAcceptAsync("application/json", url, jwt);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthTest_AdminTokenOnAdminEndpoint_ReturnsOk()
    {
        var jwt = GenerateJwt(admin: true);
        var url = "/grains/test/Orleans.Http.Test.ITestGrain/00000000-0000-0000-0000-000000000000/GetWithAuthAdmin";
        var response = await _client.GetWithAcceptAsync("application/json", url, jwt);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResp = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResp);
        Assert.Equal("admin", authResp!.Role);
    }

    [Fact]
    public async Task PatchTest_ReturnsOk()
    {
        var url = "/grains/test/00000000-0000-0000-0000-000000000000/PatchTest";
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Patched", body);
    }

    [Fact]
    public async Task IResultTest_ReturnsOkWithJson()
    {
        var url = "/grains/test/00000000-0000-0000-0000-000000000000/IResultTest";
        var response = await _client.GetWithAcceptAsync("application/json", url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TestPayload>();
        Assert.NotNull(payload);
        Assert.Equal(42, payload!.Number);
        Assert.Equal("GrainResults", payload.Text);
    }

    [Fact]
    public async Task IResultNotFound_Returns404()
    {
        var url = "/grains/test/00000000-0000-0000-0000-000000000000/IResultNotFound";
        var response = await _client.GetWithAcceptAsync("application/json", url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string GenerateJwt(bool admin)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TestAppConfig.TestSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, admin ? "admin" : "user")
            }),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public static class TestExtensions
{
    public const string Protobuf = "application/protobuf";
    public const string Json = "application/json";

    public static Task<HttpResponseMessage> GetWithAcceptAsync(
        this HttpClient http, string mimeType, string path, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mimeType));
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return http.SendAsync(request);
    }
}
