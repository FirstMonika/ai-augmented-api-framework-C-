using System.Text;
using System.Xml.Linq;

namespace AiAugmentedApiFramework.Agents;

public sealed record TestOutcome(string Name, string Outcome, string? Message);

/// <summary>Reads a .trx results file and produces an executive summary in Markdown.</summary>
public sealed class ReportAnalyzerAgent : BaseAgent
{
    public static List<TestOutcome> ReadTrx(string path)
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var doc = XDocument.Load(path);
        return doc.Descendants(ns + "UnitTestResult")
            .Select(r => new TestOutcome(
                (string?)r.Attribute("testName") ?? "?",
                (string?)r.Attribute("outcome") ?? "?",
                r.Descendants(ns + "Message").FirstOrDefault()?.Value))
            .ToList();
    }

    public async Task<string> AnalyzeAsync(string trxPath)
    {
        var results = ReadTrx(trxPath);
        int passed = results.Count(r => r.Outcome == "Passed");
        int failed = results.Count(r => r.Outcome == "Failed");
        var rate = results.Count == 0 ? 0 : Math.Round(100.0 * passed / results.Count, 1);

        var sb = new StringBuilder();
        sb.AppendLine($"Environment: {Cfg.Name}\nTotal: {results.Count}  Passed: {passed}  Failed: {failed}  Pass rate: {rate}%");
        foreach (var f in results.Where(r => r.Outcome == "Failed"))
            sb.AppendLine($"- FAILED {f.Name}: {f.Message?.Split('\n')[0]}");

        var ai = await ChatAsync(
            "You are a QA lead writing an executive summary in Markdown with sections: Overview, Failure Analysis, " +
            "Risk Patterns, Recommended Actions. Be concise and specific.",
            sb.ToString(), jsonMode: false);

        return ai ?? $"# Test Summary (local, no AI)\n\n```\n{sb}```\n";
    }
}
