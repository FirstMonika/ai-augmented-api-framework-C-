using AiAugmentedApiFramework.Config;
using AiAugmentedApiFramework.Helpers;
using Microsoft.Playwright;

namespace AiAugmentedApiFramework.Base;

public sealed record BookingRef(int Id, Dictionary<string, object?> Payload);

/// <summary>
/// Shared fixture: one Playwright request context + auth token per test class, and automatic
/// cleanup of every booking a test created (the equivalent of the JS testBooking fixture teardown).
/// NUnit runs tests sequentially by default, which is what we want against a shared sandbox API.
/// </summary>
public abstract class ApiTestBase
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _context = null!;
    private readonly List<int> _created = new();

    protected EnvConfig Cfg { get; private set; } = null!;
    protected ApiClient Api { get; private set; } = null!;
    protected string Token { get; private set; } = null!;
    protected string AuthCookie => ApiClient.Cookie(Token);

    [OneTimeSetUp]
    public async Task BaseOneTimeSetUp()
    {
        Cfg = EnvConfig.Current;
        _playwright = await Playwright.CreateAsync();
        _context = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = Cfg.BaseUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Accept"] = "application/json",
            },
        });
        Api = new ApiClient(_context);

        var auth = await Api.AuthAsync(DataFactory.Credentials(Cfg.Username, Cfg.Password));
        Token = auth.Body.GetProperty("token").GetString()
                ?? throw new InvalidOperationException("Could not obtain an auth token - check credentials.");
    }

    [OneTimeTearDown]
    public async Task BaseOneTimeTearDown()
    {
        await _context.DisposeAsync();
        _playwright.Dispose();
    }

    [TearDown]
    public async Task CleanupBookings()
    {
        foreach (var id in _created.ToList())
            await Api.DeleteBookingAsync(id, AuthCookie);   // 405 for already-deleted bookings is fine
        _created.Clear();
    }

    /// <summary>POST /booking, remember the id for cleanup, and throttle to be kind to the API.</summary>
    protected async Task<ApiResult> CreateAsync(Dictionary<string, object?> payload)
    {
        var res = await Api.CreateBookingAsync(payload);
        if (res.Status == 200 && res.Json is { } j && j.TryGetProperty("bookingid", out var id))
            _created.Add(id.GetInt32());
        await Task.Delay(Cfg.ThrottleMs);
        return res;
    }

    /// <summary>Creates a fresh valid booking (asserting success) and returns its id and payload.</summary>
    protected async Task<BookingRef> NewBookingAsync(Dictionary<string, object?>? payload = null)
    {
        payload ??= DataFactory.ValidBooking();
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(200), "precondition: booking creation must succeed");
        return new BookingRef(res.Body.GetProperty("bookingid").GetInt32(), payload);
    }
}
