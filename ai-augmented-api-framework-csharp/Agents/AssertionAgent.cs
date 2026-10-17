namespace AiAugmentedApiFramework.Agents;

/// <summary>Suggests extra NUnit/Playwright assertions for a real response, and explains failures.</summary>
public sealed class AssertionAgent : BaseAgent
{
    public async Task<string?> SuggestAssertionsAsync(string responseBody, string context)
    {
        const string system =
            "You are a QA automation expert writing C# NUnit assertions (Assert.That) for System.Text.Json JsonElement bodies. " +
            "Respond with JSON only: { \"suggestions\": [ { \"field\": string, \"assertion\": string, " +
            "\"rationale\": string, \"csharpCode\": string } ] }.";
        var raw = await ChatAsync(system, $"Context: {context}\nResponse body:\n{responseBody}\n" +
            "Suggest shape, type, range and consistency assertions beyond the HTTP status code.", jsonMode: true);
        return raw is null ? null : Pretty(raw);
    }

    public Task<string?> ExplainFailureAsync(string testName, string expected, string actual) =>
        ChatAsync("You are a QA engineer. Explain in 3-5 sentences, plain language, why a test failed and the likely cause.",
                  $"Test: {testName}\nExpected: {expected}\nActual: {actual}", jsonMode: false);
}
