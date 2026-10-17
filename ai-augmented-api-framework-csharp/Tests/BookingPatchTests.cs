using AiAugmentedApiFramework.Base;
using AiAugmentedApiFramework.Helpers;

namespace AiAugmentedApiFramework.Tests;

[TestFixture, Category("Patch")]
public class BookingPatchTests : ApiTestBase
{
    private static Dictionary<string, object?> Patch(string key, object? value) => new() { [key] = value };

    // ---------- Positive ----------
    [Test]
    public async Task PATCH_001_ChangesOnlyTheSpecifiedField()
    {
        var booking = await NewBookingAsync();
        var res = await Api.PatchBookingAsync(booking.Id, Patch("firstname", "PatchedFirst"), AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.GetProperty("firstname").GetString(), Is.EqualTo("PatchedFirst"));
        Assert.That(res.Body.GetProperty("lastname").GetString(), Is.EqualTo((string?)booking.Payload["lastname"]));
        Assert.That(res.Body.GetProperty("totalprice").GetInt32(), Is.EqualTo(Convert.ToInt32(booking.Payload["totalprice"])));

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo("PatchedFirst"));
        Assert.That(get.Body.GetProperty("lastname").GetString(), Is.EqualTo((string?)booking.Payload["lastname"]));
    }

    [Test]
    public async Task PATCH_002_CanUpdateLastnameOnly()
    {
        var booking = await NewBookingAsync();
        var res = await Api.PatchBookingAsync(booking.Id, Patch("lastname", "PatchedLast"), AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.GetProperty("lastname").GetString(), Is.EqualTo("PatchedLast"));
        Assert.That(res.Body.GetProperty("firstname").GetString(), Is.EqualTo((string?)booking.Payload["firstname"]));
    }

    [Test]
    public async Task PATCH_003_CanUpdateTotalPriceOnly()
    {
        var booking = await NewBookingAsync();
        var res = await Api.PatchBookingAsync(booking.Id, Patch("totalprice", 777), AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Body.GetProperty("totalprice").GetInt32(), Is.EqualTo(777));
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo((string?)booking.Payload["firstname"]));
        Assert.That(get.Body.GetProperty("depositpaid").GetBoolean(), Is.EqualTo(true));
    }

    [Test]
    public async Task PATCH_004_CanUpdateNestedBookingDates()
    {
        var booking = await NewBookingAsync();
        var res = await Api.PatchBookingAsync(booking.Id,
            Patch("bookingdates", DataFactory.Dates("2025-10-01", "2025-10-07")), AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Body.GetProperty("bookingdates").GetProperty("checkin").GetString(), Is.EqualTo("2025-10-01"));
        Assert.That(get.Body.GetProperty("bookingdates").GetProperty("checkout").GetString(), Is.EqualTo("2025-10-07"));
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo((string?)booking.Payload["firstname"]));
    }

    [Test]
    public async Task PATCH_005_MultipleFieldsInOnePatch()
    {
        var booking = await NewBookingAsync();
        var patch = new Dictionary<string, object?>
        {
            ["firstname"] = "MultiFirst", ["lastname"] = "MultiLast", ["totalprice"] = 500,
        };
        var res = await Api.PatchBookingAsync(booking.Id, patch, AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.GetProperty("firstname").GetString(), Is.EqualTo("MultiFirst"));
        Assert.That(res.Body.GetProperty("lastname").GetString(), Is.EqualTo("MultiLast"));
        Assert.That(res.Body.GetProperty("totalprice").GetInt32(), Is.EqualTo(500));
        Assert.That(res.Body.GetProperty("depositpaid").GetBoolean(), Is.EqualTo(true));
        Assert.That(res.Body.GetProperty("bookingdates").GetProperty("checkin").GetString(), Is.EqualTo("2025-06-01"));
    }

    [Test]
    public async Task PATCH_006_ResponseIsFullyPopulatedBooking()
    {
        var booking = await NewBookingAsync();
        var res = await Api.PatchBookingAsync(booking.Id, Patch("firstname", "ShapeCheck"), AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));
        BookingAssert.Shape(res.Body);
    }

    [Test]
    public async Task PATCH_007_PatchedDataPersists()
    {
        var booking = await NewBookingAsync();
        await Api.PatchBookingAsync(booking.Id, Patch("lastname", "PersistedPatch"), AuthCookie);
        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Status, Is.EqualTo(200));
        Assert.That(get.Body.GetProperty("lastname").GetString(), Is.EqualTo("PersistedPatch"));
    }

    // ---------- Security ----------
    [Test]
    public async Task PATCH_008_WithoutAuthReturns403AndBookingUnchanged()
    {
        var booking = await NewBookingAsync();
        var res = await Api.PatchBookingAsync(booking.Id, Patch("firstname", "NoAuth"), null);
        Assert.That(res.Status, Is.EqualTo(403));

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo((string?)booking.Payload["firstname"]));
    }

    [Test]
    public async Task PATCH_009_InvalidTokenReturns403()
    {
        var booking = await NewBookingAsync();
        var res = await Api.PatchBookingAsync(booking.Id, Patch("firstname", "Garbage"), "token=garbage");
        Assert.That(res.Status, Is.EqualTo(403));

        var get = await Api.GetBookingAsync(booking.Id);
        BookingAssert.Matches(get.Body, booking.Payload);
    }

    // ---------- Negative ----------
    [Test]
    public async Task PATCH_010_NonExistentIdReturns405Or404()
    {
        var res = await Api.PatchBookingAsync(999999999, Patch("firstname", "Ghost"), AuthCookie);
        Assert.That(res.Status, Is.AnyOf(404, 405));
    }

    // ---------- Edge ----------
    [Test]
    public async Task PATCH_011_EmptyBodyChangesNothingAndDoesNotCrash()
    {
        var booking = await NewBookingAsync();
        var res = await Api.PatchBookingAsync(booking.Id, new Dictionary<string, object?>(), AuthCookie);
        Assert.That(res.Status, Is.AnyOf(200, 400), $"actual status: {res.Status}");

        if (res.Status == 200)
        {
            var get = await Api.GetBookingAsync(booking.Id);
            BookingAssert.Matches(get.Body, booking.Payload);
        }
    }
}
