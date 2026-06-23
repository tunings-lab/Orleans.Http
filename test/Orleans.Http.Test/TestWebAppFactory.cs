using Microsoft.AspNetCore.Mvc.Testing;

namespace Orleans.Http.Test;

/// <summary>
/// WebApplicationFactory for the TestApp. Uses TestServer internally — no real port needed, works in CI.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    // Config is static so the TestApp's Program.cs can read it
    // Reset before each test run if needed
    public static void ResetConfig() => TestAppConfig.UseRandomGuidDefaultGrainProvider = false;
}
