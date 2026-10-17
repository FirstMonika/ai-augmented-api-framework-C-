using Microsoft.Playwright;

namespace AiAugmentedApiFramework.Helpers;

/// <summary>
/// Thin typed client over Playwright's APIRequestContext - one method per endpoint.
/// Auth is passed as a raw Cookie header value (null = send no auth header), which lets
/// security tests send missing, invalid, empty or forged tokens.
/// </summary>
public sealed class ApiClient
{
    private readonly IAPIRequestContext _ctx;
    public ApiClient(IAPIRequestContext ctx) => _ctx = ctx;

    public static string Cookie(string token) => $"token={token}";

    public Task<ApiResult> PingAsync() => SendAsync("GET", "/ping");
    public Task<ApiResult> AuthAsync(object credentials) => SendAsync("POST", "/auth", credentials);

    public Task<ApiResult> GetBookingIdsAsync(string? query = null) =>
        SendAsync("GET", "/booking" + (string.IsNullOrEmpty(query) ? "" : "?" + query));

    public Task<ApiResult> GetBookingAsync(object id) => SendAsync("GET", $"/booking/{id}");
    public Task<ApiResult> CreateBookingAsync(object payload) => SendAsync("POST", "/booking", payload);

    public Task<ApiResult> UpdateBookingAsync(object id, object payload, string? authCookie = null) =>
        SendAsync("PUT", $"/booking/{id}", payload, authCookie);

    public Task<ApiResult> PatchBookingAsync(object id, object payload, string? authCookie = null) =>
        SendAsync("PATCH", $"/booking/{id}", payload, authCookie);

    public Task<ApiResult> DeleteBookingAsync(object id, string? authCookie = null) =>
        SendAsync("DELETE", $"/booking/{id}", null, authCookie);

    private async Task<ApiResult> SendAsync(string method, string path, object? data = null, string? authCookie = null)
    {
        var options = new APIRequestContextOptions { Method = method };
        if (data is not null) options.DataObject = data;
        if (authCookie is not null)
            options.Headers = new Dictionary<string, string> { ["Cookie"] = authCookie };

        var response = await _ctx.FetchAsync(path, options);
        return await ApiResult.FromAsync(response);
    }
}
