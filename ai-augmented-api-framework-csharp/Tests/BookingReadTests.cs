using System.Text.Json;
using AiAugmentedApiFramework.Base;
using AiAugmentedApiFramework.Helpers;

namespace AiAugmentedApiFramework.Tests;

[TestFixture, Category("Read")]
public class BookingReadTests : ApiTestBase
{
    // =============== GET /booking (list) ===============
    [Test]
    public async Task READ_001_ListReturnsArrayOfBookingIdObjects()
    {
        await NewBookingAsync();
        var res = await Api.GetBookingIdsAsync();
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.ValueKind, Is.EqualTo(JsonValueKind.Array));
        foreach (var item in res.Body.EnumerateArray())
        {
            Assert.That(item.EnumerateObject().Count(), Is.EqualTo(1), "each element has exactly one key");
            Assert.That(item.GetProperty("bookingid").GetInt32(), Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task READ_002_ListContentTypeIsJson()
    {
        var res = await Api.GetBookingIdsAsync();
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Header("content-type"), Does.Contain("application/json"));
    }

    [Test]
    public async Task READ_003_FilterByFirstnameReturnsTargetBooking()
    {
        var payload = DataFactory.ValidBooking();
        payload["firstname"] = DataFactory.Unique("Filter");
        var booking = await NewBookingAsync(payload);

        var res = await Api.GetBookingIdsAsync($"firstname={payload["firstname"]}");
        Assert.That(res.Status, Is.EqualTo(200));
        var ids = BookingAssert.Ids(res.Body);
        Assert.That(ids, Does.Contain(booking.Id));
        Assert.That(ids.Length, Is.LessThan(50), "filter must narrow the result set");
    }

    [Test]
    public async Task READ_004_FilterByCheckinReturnsValidArray()
    {
        await NewBookingAsync();
        var res = await Api.GetBookingIdsAsync("checkin=2025-06-01");
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.ValueKind, Is.EqualTo(JsonValueKind.Array));
    }

    [Test]
    public async Task READ_005_FilterWithNoMatchReturnsEmptyArray()
    {
        var res = await Api.GetBookingIdsAsync("firstname=ZZZNOMATCH_99999");
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(res.Body.GetArrayLength(), Is.EqualTo(0));
    }

    // =============== GET /booking/{id} ===============
    [Test]
    public async Task READ_006_ValidIdReturnsFullyTypedBooking()
    {
        var booking = await NewBookingAsync();
        var res = await Api.GetBookingAsync(booking.Id);
        Assert.That(res.Status, Is.EqualTo(200));
        BookingAssert.Shape(res.Body);
        BookingAssert.Matches(res.Body, booking.Payload);
    }

    [Test]
    public async Task READ_007_ResponseContainsAllExpectedFields()
    {
        var booking = await NewBookingAsync();
        var res = await Api.GetBookingAsync(booking.Id);
        foreach (var key in new[] { "firstname", "lastname", "totalprice", "depositpaid", "bookingdates" })
            Assert.That(res.Body.TryGetProperty(key, out _), Is.True, $"missing '{key}'");
        foreach (var key in new[] { "checkin", "checkout" })
            Assert.That(res.Body.GetProperty("bookingdates").TryGetProperty(key, out _), Is.True, $"missing '{key}'");
    }

    [Test]
    public async Task READ_008_ListAndDirectGetAreConsistent()
    {
        var booking = await NewBookingAsync();
        var list = await Api.GetBookingIdsAsync();
        Assert.That(BookingAssert.Ids(list.Body), Does.Contain(booking.Id));

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo((string?)booking.Payload["firstname"]));
        Assert.That(get.Body.GetProperty("totalprice").GetInt32(), Is.EqualTo(Convert.ToInt32(booking.Payload["totalprice"])));
    }

    [Test]
    public async Task READ_009_NewBookingIsImmediatelyRetrievable()
    {
        var create = await CreateAsync(DataFactory.ValidBooking());
        var get = await Api.GetBookingAsync(create.Body.GetProperty("bookingid").GetInt32());
        Assert.That(get.Status, Is.EqualTo(200));
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo("Jim"));
    }

    [Test]
    public async Task READ_010_NonExistentIdReturns404()
    {
        var res = await Api.GetBookingAsync(999999999);
        Assert.That(res.Status, Is.EqualTo(404));
        Assert.That(res.Text, Does.Not.Contain("firstname"));
    }

    [Test]
    public async Task READ_011_NegativeIdReturns404Or400()
    {
        var res = await Api.GetBookingAsync(-1);
        Assert.That(res.Status, Is.AnyOf(404, 400));
    }

    [Test]
    public async Task READ_012_StringIdReturns404Or400()
    {
        var res = await Api.GetBookingAsync("not-a-number");
        Assert.That(res.Status, Is.AnyOf(404, 400));
    }
}
