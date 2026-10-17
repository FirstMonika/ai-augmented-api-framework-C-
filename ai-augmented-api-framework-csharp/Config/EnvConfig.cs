using System.Text.Json;

namespace AiAugmentedApiFramework.Config;

/// <summary>
/// Environment loader. Reads the ENV variable (dev | uat | prod, default dev),
/// loads Config/env/{env}.json, then lets environment variables override values.
/// An unknown ENV value throws immediately instead of silently using the wrong config.
/// </summary>
public sealed record EnvConfig(
    string Name,
    string BaseUrl,
    string Username,
    string Password,
    int ThrottleMs,
    string? OpenAiApiKey)
{
    private static readonly string[] Known = { "dev", "uat", "prod" };
    private static EnvConfig? _cached;

    public static EnvConfig Current => _cached ??= Load();

    public static EnvConfig Load()
    {
        var env = (Environment.GetEnvironmentVariable("ENV") ?? "dev").Trim().ToLowerInvariant();
        if (!Known.Contains(env))
            throw new InvalidOperationException(
                $"Unknown ENV '{env}'. Expected one of: {string.Join(", ", Known)}.");

        var path = Path.Combine(AppContext.BaseDirectory, "Config", "env", $"{env}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Environment file not found: {path}");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var auth = root.GetProperty("auth");

        string? Env(string name) =>
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))
                ? null : Environment.GetEnvironmentVariable(name);

        return new EnvConfig(
            Name: env,
            BaseUrl: (Env("BASE_URL") ?? root.GetProperty("baseURL").GetString()!).TrimEnd('/'),
            Username: Env("BOOKER_USERNAME") ?? auth.GetProperty("username").GetString()!,
            Password: Env("BOOKER_PASSWORD") ?? auth.GetProperty("password").GetString()!,
            ThrottleMs: root.TryGetProperty("throttleMs", out var t) ? t.GetInt32() : 200,
            OpenAiApiKey: Env("OPENAI_API_KEY") ??
                (root.TryGetProperty("openAiApiKey", out var k) ? k.GetString() : null));
    }
}

public static class RepoPaths
{
    /// <summary>Walks up from the bin folder to the folder containing the .csproj.</summary>
    public static string Root
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !dir.GetFiles("*.csproj").Any()) dir = dir.Parent;
            return dir?.FullName ?? Directory.GetCurrentDirectory();
        }
    }

    public static string Reports
    {
        get { var p = Path.Combine(Root, "reports"); Directory.CreateDirectory(p); return p; }
    }
}
