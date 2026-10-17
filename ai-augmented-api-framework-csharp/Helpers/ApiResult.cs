using System.Text.Json;
using Microsoft.Playwright;

namespace AiAugmentedApiFramework.Helpers;

/// <summary>Eagerly-read response: status, raw text, headers and (when parseable) JSON body.</summary>
public sealed class ApiResult
{
    public int Status { get; init; }
    public string Text { get; init; } = "";
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public JsonElement? Json { get; init; }

    /// <summary>JSON body, or a clear assertion failure if the response was not JSON.</summary>
    public JsonElement Body =>
        Json ?? throw new AssertionException($"Response was not JSON (HTTP {Status}): '{Text}'");

    public string? Header(string name) =>
        Headers.FirstOrDefault(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;

    public static async Task<ApiResult> FromAsync(IAPIResponse res)
    {
        var text = await res.TextAsync();
        JsonElement? json = null;
        if (!string.IsNullOrWhiteSpace(text))
        {
            try { using var d = JsonDocument.Parse(text); json = d.RootElement.Clone(); }
            catch (JsonException) { /* non-JSON body, e.g. "Forbidden" */ }
        }
        return new ApiResult { Status = res.Status, Text = text, Headers = res.Headers, Json = json };
    }
}
