using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AiAugmentedApiFramework.Config;

namespace AiAugmentedApiFramework.Agents;

/// <summary>Shared OpenAI chat client. Without OPENAI_API_KEY every agent logs a warning and no-ops.</summary>
public abstract class BaseAgent
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };
    protected EnvConfig Cfg { get; } = EnvConfig.Current;
    protected virtual string Model => "gpt-4o";

    protected async Task<string?> ChatAsync(string system, string user, bool jsonMode)
    {
        if (string.IsNullOrWhiteSpace(Cfg.OpenAiApiKey))
        {
            TestContext.Progress.WriteLine("[WARN] OPENAI_API_KEY not set - skipping AI enrichment.");
            return null;
        }

        var body = new Dictionary<string, object?>
        {
            ["model"] = Model,
            ["temperature"] = 0.2,
            ["messages"] = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            },
        };
        if (jsonMode) body["response_format"] = new { type = "json_object" };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Cfg.OpenAiApiKey);

        using var res = await Http.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI request failed ({(int)res.StatusCode}): {text}");

        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }

    /// <summary>Pretty-prints JSON returned by the model (falls back to raw text if it is not valid JSON).</summary>
    protected static string Pretty(string raw)
    {
        try
        {
            using var d = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(d.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException) { return raw; }
    }
}
