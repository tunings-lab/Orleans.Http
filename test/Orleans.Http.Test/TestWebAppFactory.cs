using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Orleans.Http;
using Orleans.Http.MediaTypes.Protobuf;

namespace Orleans.Http.Test;

/// <summary>
/// Test web application factory that configures Orleans + HTTP grain routing in-process.
/// Uses the modern .NET 10 minimal-host pattern (no Startup class).
/// </summary>
public class TestWebAppFactory : IAsyncDisposable
{
    private WebApplication? _app;
    private HttpClient? _client;

    public bool UseRandomGuidDefaultGrainProvider { get; set; }

    public HttpClient Client => _client ?? throw new InvalidOperationException("Not started");

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();

        // Orleans in-process silo
        builder.Host.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering();
        });

        // HttpContextAccessor (needed by grains and default route grain provider)
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Auth
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(TestSecret)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        builder.Services.AddAuthorization();

        // Grain router + media types
        builder.Services
            .AddGrainRouter()
            .AddJsonMediaType()
            .AddProtobufMediaType()
            .AddFormsMediaType();

        var app = builder.Build();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGrains("grains");

        app.UseRouteGrainProviders(rgppb =>
        {
            rgppb.RegisterRouteGrainProvider<RandomGuidRouteGrainProvider>(nameof(RandomGuidRouteGrainProvider));
            rgppb.RegisterRouteGrainProvider<FailingRouteGrainProvider>(nameof(FailingRouteGrainProvider));

            if (UseRandomGuidDefaultGrainProvider)
            {
                rgppb.SetDefaultRouteGrainProviderPolicy(nameof(RandomGuidRouteGrainProvider));
            }
        });

        await app.StartAsync();

        _app = app;
        _client = new HttpClient { BaseAddress = new Uri("http://localhost:9090") };
    }

    public async Task StopAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
        _client = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public const string TestSecret = "THIS IS OUR AWESOME SUPER SECRET!!!";
}
