using System.Text.Json;
using AiAugmentedApiFramework.Base;
using AiAugmentedApiFramework.Helpers;

namespace AiAugmentedApiFramework.Tests;

[TestFixture, Category("Create")]
public class BookingCreateTests : ApiTestBase
{
    // ---------- Positive ----------
    [Test]
    public async Task CRT_001_FullPayloadCreatesBookingWithCorrectTypes()
    {
        var payload = DataFactory.ValidBooking();
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(200));
        BookingAssert.CreateResponse(res.Body);

        var booking = res.Body.GetProperty("booking");
        BookingAssert.Matches(booking, payload);
        Assert.That(booking.GetProperty("totalprice").ValueKind, Is.EqualTo(JsonValueKind.Number));
        Assert.That(booking.GetProperty("depositpaid").ValueKind, Is.EqualTo(JsonValueKind.True));
    }

    [Test]
    public async Task CRT_002_MinimalPayloadWithoutAdditionalNeedsCreatesBooking()
    {
        var res = await CreateAsync(DataFactory.MinimalBooking());
        Assert.That(res.Status, Is.EqualTo(200));
        BookingAssert.CreateResponse(res.Body);

        var booking = res.Body.GetProperty("booking");
        if (booking.TryGetProperty("additionalneeds", out var an) && an.ValueKind == JsonValueKind.String)
            Assert.That(an.GetString(), Is.Empty);
    }

    [Test]
    public async Task CRT_003_DepositPaidFalseIsReturnedAsBooleanFalse()
    {
        var payload = DataFactory.ValidBooking();
        payload["depositpaid"] = false;
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.GetProperty("booking").GetProperty("depositpaid").ValueKind,
            Is.EqualTo(JsonValueKind.False));
    }

    [Test]
    public async Task CRT_004_IdenticalPayloadsProduceDistinctIds()
    {
        var payload = DataFactory.ValidBooking();
        var a = await CreateAsync(payload);
        var b = await CreateAsync(payload);
        Assert.That(a.Status, Is.EqualTo(200));
        Assert.That(b.Status, Is.EqualTo(200));
        Assert.That(b.Body.GetProperty("bookingid").GetInt32(),
            Is.Not.EqualTo(a.Body.GetProperty("bookingid").GetInt32()));
    }

    [Test]
    public async Task CRT_005_TotalPriceZeroIsAcceptedAndStored()
    {
        var payload = DataFactory.ValidBooking();
        payload["totalprice"] = 0;
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(200));
        var id = res.Body.GetProperty("bookingid").GetInt32();
        Assert.That(res.Body.GetProperty("booking").GetProperty("totalprice").GetInt32(), Is.EqualTo(0));

        var get = await Api.GetBookingAsync(id);
        Assert.That(get.Body.GetProperty("totalprice").GetInt32(), Is.EqualTo(0));
    }

    // ---------- Negative ----------
    [Test]
    public async Task CRT_006_MissingFirstnameReturns500()
    {
        var res = await CreateAsync(DataFactory.ValidBooking().Without("firstname"));
        Assert.That(res.Status, Is.EqualTo(500));
        Assert.That(res.Text, Does.Not.Contain("bookingid"));
    }

    [Test]
    public async Task CRT_007_MissingBookingDatesReturns500()
    {
        var res = await CreateAsync(DataFactory.ValidBooking().Without("bookingdates"));
        Assert.That(res.Status, Is.EqualTo(500));
        Assert.That(res.Text, Does.Not.Contain("bookingid"));
    }

    [Test]
    public async Task CRT_008_StringTotalPriceIsCoercedOrRejectedNeverStoredAsString()
    {
        var payload = DataFactory.ValidBooking();
        payload["totalprice"] = "free";
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.AnyOf(200, 400, 500), $"actual status: {res.Status}");
        if (res.Status == 200)
            Assert.That(res.Body.GetProperty("booking").GetProperty("totalprice").ValueKind,
                Is.Not.EqualTo(JsonValueKind.String));
    }

    [Test]
    public async Task CRT_009_NumericFirstnameReturns500()
    {
        var payload = DataFactory.ValidBooking();
        payload["firstname"] = 12345;
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(500));
        Assert.That(res.Text, Does.Not.Contain("bookingid"));
    }

    [Test]
    public async Task CRT_010_EmptyFirstnameReturns500()
    {
        var payload = DataFactory.ValidBooking();
        payload["firstname"] = "";
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(500));
    }

    // ---------- Edge / boundary ----------
    [Test]
    public async Task CRT_011_VeryLongNamesDoNotCrashTheApi()
    {
        var payload = DataFactory.ValidBooking();
        payload["firstname"] = DataFactory.Long(200, 'a');
        payload["lastname"] = DataFactory.Long(200, 'b');
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.AnyOf(200, 400));
        if (res.Status == 200)
        {
            var b = res.Body.GetProperty("booking");
            Assert.That(b.GetProperty("firstname").GetString(), Has.Length.EqualTo(200));
            Assert.That(b.GetProperty("lastname").GetString(), Has.Length.EqualTo(200));
        }
    }

    [Test]
    public async Task CRT_012_UnicodeNamesSurviveRoundTrip()
    {
        var payload = DataFactory.ValidBooking();
        payload["firstname"] = "Ångström";
        payload["lastname"] = "日本語";
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(200));

        var get = await Api.GetBookingAsync(res.Body.GetProperty("bookingid").GetInt32());
        Assert.That(get.Status, Is.EqualTo(200));
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo("Ångström"));
        Assert.That(get.Body.GetProperty("lastname").GetString(), Is.EqualTo("日本語"));
    }

    [Test]
    public async Task CRT_013_InvertedDatesAreAcceptedOrRejectedAndDocumented()
    {
        var payload = DataFactory.ValidBooking();
        payload["bookingdates"] = DataFactory.Dates("2025-06-10", "2025-06-01");
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.AnyOf(200, 400), $"actual status: {res.Status}");
        TestContext.Progress.WriteLine($"[CRT-013] Inverted dates -> HTTP {res.Status}");
    }

    [Test]
    public async Task CRT_014_VeryLargeTotalPriceIsStoredPrecisely()
    {
        var payload = DataFactory.ValidBooking();
        payload["totalprice"] = 9_999_999;
        var res = await CreateAsync(payload);
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.GetProperty("booking").GetProperty("totalprice").GetInt32(), Is.EqualTo(9_999_999));
    }
}
