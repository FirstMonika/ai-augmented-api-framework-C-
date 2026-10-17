using AiAugmentedApiFramework.Config;
using Microsoft.Playwright;

namespace AiAugmentedApiFramework;

/// <summary>Runs once before any test: fails fast if the API is unreachable.</summary>
[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public async Task HealthCheck()
    {
        var cfg = EnvConfig.Current;
        using var pw = await Playwright.CreateAsync();
        var ctx = await pw.APIRequest.NewContextAsync(new() { BaseURL = cfg.BaseUrl });
        try
        {
            var res = await ctx.GetAsync("/ping");
            if (res.Status != 201)
                throw new InvalidOperationException(
                    $"[{cfg.Name}] Health check failed: GET {cfg.BaseUrl}/ping returned {res.Status}.");
            TestContext.Progress.WriteLine($"[{cfg.Name}] API healthy at {cfg.BaseUrl}");
        }
        finally { await ctx.DisposeAsync(); }
    }
}
