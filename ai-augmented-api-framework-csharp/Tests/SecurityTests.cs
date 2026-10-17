using System.Text.RegularExpressions;
using AiAugmentedApiFramework.Base;
using AiAugmentedApiFramework.Helpers;

namespace AiAugmentedApiFramework.Tests;

[TestFixture, Category("Security")]
public class SecurityTests : ApiTestBase
{
    [Test]
    public async Task SEC_001_AllWriteVerbsEnforceAuthConsistently()
    {
        var booking = await NewBookingAsync();
        var put = await Api.UpdateBookingAsync(booking.Id, DataFactory.AlternateBooking(), null);
        var patch = await Api.PatchBookingAsync(booking.Id, new Dictionary<string, object?> { ["firstname"] = "Hacked" }, null);
        var del = await Api.DeleteBookingAsync(booking.Id, null);

        Assert.Multiple(() =>
        {
            Assert.That(put.Status, Is.EqualTo(403), "PUT");
            Assert.That(patch.Status, Is.EqualTo(403), "PATCH");
            Assert.That(del.Status, Is.EqualTo(403), "DELETE");
        });

        var get = await Api.GetBookingAsync(booking.Id);
        BookingAssert.Matches(get.Body, booking.Payload);
    }

    [Test]
    public async Task SEC_002_ForgedReversedTokenIsRejected()
    {
        var booking = await NewBookingAsync();
        var forged = new string(Token.Reverse().ToArray());
        var res = await Api.UpdateBookingAsync(booking.Id, DataFactory.AlternateBooking(), ApiClient.Cookie(forged));
        Assert.That(res.Status, Is.EqualTo(403));

        var get = await Api.GetBookingAsync(booking.Id);
        BookingAssert.Matches(get.Body, booking.Payload);
    }

    [Test]
    public async Task SEC_003_SqlInjectionInFirstnameDoesNotCrashApi()
    {
        var payload = DataFactory.ValidBooking();
        payload["firstname"] = "Robert'); DROP TABLE bookings;--";
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.AnyOf(200, 500));
        Assert.That(res.Text, Does.Not.Contain("SQL").IgnoreCase);

        var list = await Api.GetBookingIdsAsync();
        Assert.That(list.Status, Is.EqualTo(200), "API must still be healthy afterwards");
        Assert.That(list.Body.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Array));
    }

    [Test]
    public async Task SEC_004_XssPayloadStoredAsPlainTextAndServedAsJson()
    {
        const string xss = "<script>alert(\"xss\")</script>";
        var payload = DataFactory.ValidBooking();
        payload["additionalneeds"] = xss;
        var res = await CreateAsync(payload);
        if (res.Status != 200) { Assert.Pass($"API rejected XSS payload with HTTP {res.Status}"); return; }

        var get = await Api.GetBookingAsync(res.Body.GetProperty("bookingid").GetInt32());
        Assert.That(get.Body.GetProperty("additionalneeds").GetString(), Is.EqualTo(xss));
        Assert.That(get.Header("content-type"), Does.Contain("application/json"));
        Assert.That(get.Header("content-type"), Does.Not.Contain("text/html"));
    }

    [Test]
    public async Task SEC_005_PathTraversalInIdIsRejected()
    {
        var res = await Api.GetBookingAsync("../etc/passwd");
        Assert.That(res.Status, Is.AnyOf(400, 404));
        Assert.That(res.Text, Does.Not.Contain("root:"));
    }

    [Test]
    public async Task SEC_006_NullByteInStringDoesNotCause500()
    {
        var payload = DataFactory.ValidBooking();
        payload["firstname"] = "Test\u0000Null";
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.AnyOf(200, 400));
        if (res.Status == 200)
            Assert.That((await Api.GetBookingAsync(res.Body.GetProperty("bookingid").GetInt32())).Status, Is.EqualTo(200));
    }

    [Test]
    public async Task SEC_007_MassAssignmentOnPostIsNotStoredOrEchoed()
    {
        var payload = DataFactory.ValidBooking();
        payload["role"] = "admin";
        payload["internalId"] = "override";
        payload["isAdmin"] = true;
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(200));

        var get = await Api.GetBookingAsync(res.Body.GetProperty("bookingid").GetInt32());
        foreach (var key in new[] { "role", "internalId", "isAdmin" })
        {
            Assert.That(res.Body.TryGetProperty(key, out _), Is.False, $"POST response echoed '{key}'");
            Assert.That(res.Body.GetProperty("booking").TryGetProperty(key, out _), Is.False, $"POST booking echoed '{key}'");
            Assert.That(get.Body.TryGetProperty(key, out _), Is.False, $"GET returned '{key}'");
        }
    }

    [Test]
    public async Task SEC_008_AuthFailureDoesNotLeakServerInternals()
    {
        var res = await Api.AuthAsync(DataFactory.Credentials("hack", "hack"));
        Assert.That(Regex.IsMatch(res.Text, @"\b(SELECT|INSERT|FROM|WHERE)\b"), Is.False, "SQL keywords leaked");
        Assert.That(Regex.IsMatch(res.Text, @"/var/|/etc/|C:\\"), Is.False, "file path leaked");
        Assert.That(Regex.IsMatch(res.Text, @"Exception|\bat\s+\S+\(|Traceback"), Is.False, "stack trace leaked");
    }

    [Test]
    public async Task SEC_009_ApiRespondsOverHttps()
    {
        Assert.That(Cfg.BaseUrl, Does.StartWith("https://"), "base URL must use TLS");
        var res = await Api.PingAsync();
        Assert.That(res.Status, Is.EqualTo(201));
    }

    [Test]
    public async Task SEC_010_ContentTypeHeaderPresentOnAllResponses()
    {
        var booking = await NewBookingAsync();
        var get = await Api.GetBookingAsync(booking.Id);
        var auth = await Api.AuthAsync(DataFactory.Credentials(Cfg.Username, Cfg.Password));
        var del = await Api.DeleteBookingAsync(booking.Id, AuthCookie);

        Assert.Multiple(() =>
        {
            Assert.That(get.Header("content-type"), Does.Contain("application/json"), "GET");
            Assert.That(auth.Header("content-type"), Does.Contain("application/json"), "POST /auth");
            Assert.That(del.Header("content-type"), Is.Not.Null.And.Not.Empty, "DELETE");
        });
        TestContext.Progress.WriteLine($"[SEC-010] DELETE Content-Type: {del.Header("content-type")}");
    }

    [Test]
    public async Task SEC_011_MassAssignmentOnPutDoesNotInjectFields()
    {
        var booking = await NewBookingAsync();
        var update = DataFactory.AlternateBooking();
        update["createdAt"] = "2000-01-01";
        update["ownerId"] = 999;
        var res = await Api.UpdateBookingAsync(booking.Id, update, AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Body.TryGetProperty("createdAt", out _), Is.False);
        Assert.That(get.Body.TryGetProperty("ownerId", out _), Is.False);
        BookingAssert.Matches(get.Body, DataFactory.AlternateBooking());
    }
}
