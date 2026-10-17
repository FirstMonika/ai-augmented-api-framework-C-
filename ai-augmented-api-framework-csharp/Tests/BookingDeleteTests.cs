using AiAugmentedApiFramework.Base;
using AiAugmentedApiFramework.Helpers;

namespace AiAugmentedApiFramework.Tests;

[TestFixture, Category("Delete")]
public class BookingDeleteTests : ApiTestBase
{
    // ---------- Positive ----------
    [Test]
    public async Task DEL_001_AuthenticatedDeleteReturns201AndBookingIsGone()
    {
        var booking = await NewBookingAsync();
        var del = await Api.DeleteBookingAsync(booking.Id, AuthCookie);
        Assert.That(del.Status, Is.EqualTo(201), "Restful-Booker returns 201, not 204");

        Assert.That((await Api.GetBookingAsync(booking.Id)).Status, Is.EqualTo(404));
        var list = await Api.GetBookingIdsAsync();
        Assert.That(BookingAssert.Ids(list.Body), Does.Not.Contain(booking.Id));
    }

    [Test]
    public async Task DEL_002_DeletedBookingAbsentFromList()
    {
        var booking = await NewBookingAsync();
        await Api.DeleteBookingAsync(booking.Id, AuthCookie);
        var list = await Api.GetBookingIdsAsync();
        Assert.That(BookingAssert.Ids(list.Body), Does.Not.Contain(booking.Id));
    }

    [Test]
    public async Task DEL_003_DeletedBookingReturns404OnGet()
    {
        var booking = await NewBookingAsync();
        await Api.DeleteBookingAsync(booking.Id, AuthCookie);
        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Status, Is.EqualTo(404));
        Assert.That(get.Text, Does.Not.Contain("firstname"));
    }

    // ---------- Security ----------
    [Test]
    public async Task DEL_004_WithoutAuthReturns403AndBookingSurvives()
    {
        var booking = await NewBookingAsync();
        var del = await Api.DeleteBookingAsync(booking.Id, null);
        Assert.That(del.Status, Is.EqualTo(403));

        var get = await Api.GetBookingAsync(booking.Id);
        Assert.That(get.Status, Is.EqualTo(200));
        BookingAssert.Matches(get.Body, booking.Payload);
    }

    [Test]
    public async Task DEL_005_InvalidTokenReturns403AndBookingSurvives()
    {
        var booking = await NewBookingAsync();
        var del = await Api.DeleteBookingAsync(booking.Id, "token=completelyinvalidtoken");
        Assert.That(del.Status, Is.EqualTo(403));
        Assert.That((await Api.GetBookingAsync(booking.Id)).Status, Is.EqualTo(200));
    }

    [Test]
    public async Task DEL_006_EmptyTokenReturns403()
    {
        var booking = await NewBookingAsync();
        var del = await Api.DeleteBookingAsync(booking.Id, "token=");
        Assert.That(del.Status, Is.EqualTo(403));
        Assert.That((await Api.GetBookingAsync(booking.Id)).Status, Is.EqualTo(200));
    }

    // ---------- Negative ----------
    [Test]
    public async Task DEL_007_NonExistentIdReturns405Or404()
    {
        var del = await Api.DeleteBookingAsync(999999999, AuthCookie);
        Assert.That(del.Status, Is.AnyOf(404, 405));
        Assert.That(del.Status, Is.Not.EqualTo(201));
    }

    // ---------- Edge ----------
    [Test]
    public async Task DEL_008_DeletingTwiceSecondAttemptIsNot201()
    {
        var booking = await NewBookingAsync();
        var first = await Api.DeleteBookingAsync(booking.Id, AuthCookie);
        var second = await Api.DeleteBookingAsync(booking.Id, AuthCookie);
        Assert.That(first.Status, Is.EqualTo(201));
        Assert.That(second.Status, Is.AnyOf(404, 405));
    }

    [Test]
    public async Task DEL_009_StringPathParameterReturns404Or405()
    {
        var del = await Api.DeleteBookingAsync("not-an-id", AuthCookie);
        Assert.That(del.Status, Is.AnyOf(404, 405));
    }
}
