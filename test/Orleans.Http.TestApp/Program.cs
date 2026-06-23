using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Orleans.Hosting;
using Orleans.Http;
using Orleans.Http.MediaTypes.Protobuf;
using Orleans.Http.Test;

var builder = WebApplication.CreateBuilder(args);

// Orleans in-process silo
builder.Host.UseOrleans(silo => silo.UseLocalhostClustering());

// HttpContextAccessor
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
                Encoding.UTF8.GetBytes(TestAppConfig.TestSecret)),
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

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrains("grains");
app.MapOpenApi("/openapi/{documentName}.json");

app.UseRouteGrainProviders(rgppb =>
{
    rgppb.RegisterRouteGrainProvider<RandomGuidRouteGrainProvider>(nameof(RandomGuidRouteGrainProvider));
    rgppb.RegisterRouteGrainProvider<FailingRouteGrainProvider>(nameof(FailingRouteGrainProvider));

    if (TestAppConfig.UseRandomGuidDefaultGrainProvider)
    {
        rgppb.SetDefaultRouteGrainProviderPolicy(nameof(RandomGuidRouteGrainProvider));
    }
});

app.Run();

public partial class Program { }
