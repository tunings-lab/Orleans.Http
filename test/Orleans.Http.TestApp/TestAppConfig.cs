namespace Orleans.Http.Test;

/// <summary>
/// Static configuration shared between the TestApp and the test factory.
/// </summary>
public static class TestAppConfig
{
    public static bool UseRandomGuidDefaultGrainProvider { get; set; }
    public const string TestSecret = "THIS IS OUR AWESOME SUPER SECRET!!!";
}
