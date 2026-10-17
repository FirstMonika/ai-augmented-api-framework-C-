using AiAugmentedApiFramework.Agents;
using AiAugmentedApiFramework.Config;

namespace AiAugmentedApiFramework.Tests;

/// <summary>
/// On-demand AI tooling. [Explicit] means these never run in a normal `dotnet test`;
/// select them by name (see README). They are additive - no test depends on them.
/// </summary>
[TestFixture, Category("Agent"), Explicit("Run on demand")]
public class AgentRunner
{
    [Test]
    public async Task GenerateTestCases()
    {
        var json = await new TestGeneratorAgent().GenerateAllAsync();
        if (json is null) { Assert.Ignore("OPENAI_API_KEY not set"); return; }
        var path = Path.Combine(RepoPaths.Reports, "ai-generated-test-cases.json");
        await File.WriteAllTextAsync(path, json);
        TestContext.Progress.WriteLine($"Wrote {path}");
    }

    [Test]
    public async Task AnalyzeReport()
    {
        var trx = Path.Combine(RepoPaths.Reports, "results.trx");
        if (!File.Exists(trx))
        {
            Assert.Ignore($"{trx} not found - run: dotnet test --logger \"trx;LogFileName=results.trx\" --results-directory reports");
            return;
        }
        var md = await new ReportAnalyzerAgent().AnalyzeAsync(trx);
        await File.WriteAllTextAsync(Path.Combine(RepoPaths.Reports, "ai-summary.md"), md);
        TestContext.Progress.WriteLine(md);
    }
}
