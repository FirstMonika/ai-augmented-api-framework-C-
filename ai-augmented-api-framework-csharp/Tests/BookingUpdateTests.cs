using AiAugmentedApiFramework.Base;
using AiAugmentedApiFramework.Helpers;

namespace AiAugmentedApiFramework.Tests;

[TestFixture, Category("Update")]
public class BookingUpdateTests : ApiTestBase
{
    // ---------- Positive ----------
    [Test]
    public async Task PUT_001_AuthenticatedFullUpdateReturnsUpdatedBooking()
    {
        var booking = await NewBookingAsync();
        var update = DataFactory.AlternateBooking();
        var res = await Api.UpdateBookingAsync(booking.Id, update, AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));
        BookingAssert.Shape(res.Body);
        BookingAssert.Matches(res.Body, update);
    }

    [Test]
    public async Task PUT_002_UpdatedDataIsPersisted()
    {
        var booking = await NewBookingAsync();
        var update = DataFactory.AlternateBooking();
        update["firstname"] = "PersistenceCheck";
        await Api.UpdateBookingAsync(booking.Id, update, AuthCookie);

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Status, Is.EqualTo(200));
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo("PersistenceCheck"));
        Assert.That(get.Body.GetProperty("totalprice").GetInt32(), Is.EqualTo(Convert.ToInt32(update["totalprice"])));
    }

    [Test]
    public async Task PUT_003_CanUpdateTotalPriceToZero()
    {
        var booking = await NewBookingAsync();
        var update = DataFactory.ValidBooking();
        update["totalprice"] = 0;
        var res = await Api.UpdateBookingAsync(booking.Id, update, AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.GetProperty("totalprice").GetInt32(), Is.EqualTo(0));
    }

    [Test]
    public async Task PUT_004_CanFlipDepositPaidToFalse()
    {
        var booking = await NewBookingAsync();   // depositpaid = true
        var update = DataFactory.ValidBooking();
        update["depositpaid"] = false;
        var res = await Api.UpdateBookingAsync(booking.Id, update, AuthCookie);
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Body.GetProperty("depositpaid").GetBoolean(), Is.False);
    }

    // ---------- Security ----------
    [Test]
    public async Task PUT_005_WithoutAuthReturns403AndBookingUnchanged()
    {
        var booking = await NewBookingAsync();
        var res = await Api.UpdateBookingAsync(booking.Id, DataFactory.AlternateBooking(), null);
        Assert.That(res.Status, Is.EqualTo(403));

        var get = await Api.GetBookingAsync(booking.Id);
        BookingAssert.Matches(get.Body, booking.Payload);
    }

    [Test]
    public async Task PUT_006_InvalidTokenReturns403AndBookingUnchanged()
    {
        var booking = await NewBookingAsync();
        var res = await Api.UpdateBookingAsync(booking.Id, DataFactory.AlternateBooking(), "token=completelyinvalidtoken");
        Assert.That(res.Status, Is.EqualTo(403));

        var get = await Api.GetBookingAsync(booking.Id);
        BookingAssert.Matches(get.Body, booking.Payload);
    }

    [Test]
    public async Task PUT_007_MalformedTokenReturns403Not500()
    {
        var booking = await NewBookingAsync();
        var res = await Api.UpdateBookingAsync(booking.Id, DataFactory.AlternateBooking(), "token=!@#$%^&*()");
        Assert.That(res.Status, Is.EqualTo(403));
    }

    // ---------- Negative ----------
    [Test]
    public async Task PUT_008_NonExistentIdReturns405Or404()
    {
        var res = await Api.UpdateBookingAsync(999999999, DataFactory.ValidBooking(), AuthCookie);
        Assert.That(res.Status, Is.AnyOf(404, 405));
    }

    [Test]
    public async Task PUT_009_MissingRequiredFieldReturns400Or500AndBookingNotCorrupted()
    {
        var booking = await NewBookingAsync();
        var res = await Api.UpdateBookingAsync(booking.Id, DataFactory.AlternateBooking().Without("firstname"), AuthCookie);
        Assert.That(res.Status, Is.AnyOf(400, 500));

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Body.GetProperty("firstname").GetString(), Is.EqualTo((string?)booking.Payload["firstname"]));
    }

    // ---------- Edge ----------
    [Test]
    public async Task PUT_010_IsIdempotent()
    {
        var booking = await NewBookingAsync();
        var update = DataFactory.AlternateBooking();
        var first = await Api.UpdateBookingAsync(booking.Id, update, AuthCookie);
        var second = await Api.UpdateBookingAsync(booking.Id, update, AuthCookie);

        Assert.That(first.Status, Is.EqualTo(200));
        Assert.That(second.Status, Is.EqualTo(200));
        Assert.That(second.Text, Is.EqualTo(first.Text));

        var get = await Api.GetBookingAsync(booking.Id);
        BookingAssert.Matches(get.Body, update);
    }
}
