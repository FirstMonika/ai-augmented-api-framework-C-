using System.Text.Json;

namespace AiAugmentedApiFramework.Helpers;

/// <summary>Domain-specific assertions: shape, types, ranges and consistency - never just status codes.</summary>
public static class BookingAssert
{
    public static void Shape(JsonElement b)
    {
        Assert.That(b.ValueKind, Is.EqualTo(JsonValueKind.Object), "booking must be a JSON object");

        Assert.That(b.GetProperty("firstname").ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(b.GetProperty("lastname").ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(b.GetProperty("totalprice").ValueKind, Is.EqualTo(JsonValueKind.Number));
        Assert.That(b.GetProperty("totalprice").GetInt64(), Is.GreaterThanOrEqualTo(0));
        Assert.That(b.GetProperty("depositpaid").ValueKind, Is.AnyOf(JsonValueKind.True, JsonValueKind.False));

        var dates = b.GetProperty("bookingdates");
        Assert.That(dates.ValueKind, Is.EqualTo(JsonValueKind.Object));
        foreach (var key in new[] { "checkin", "checkout" })
        {
            var v = dates.GetProperty(key);
            Assert.That(v.ValueKind, Is.EqualTo(JsonValueKind.String));
            Assert.That(DateTime.TryParse(v.GetString(), out _), Is.True, $"{key} must be a parseable date");
        }
    }

    /// <summary>POST /booking returns { bookingid, booking }.</summary>
    public static void CreateResponse(JsonElement body)
    {
        var id = body.GetProperty("bookingid");
        Assert.That(id.ValueKind, Is.EqualTo(JsonValueKind.Number));
        Assert.That(id.GetInt32(), Is.GreaterThan(0));
        Shape(body.GetProperty("booking"));
    }

    /// <summary>Consistency: what you wrote is what you get back.</summary>
    public static void Matches(JsonElement b, Dictionary<string, object?> expected)
    {
        Assert.That(b.GetProperty("firstname").GetString(), Is.EqualTo((string?)expected["firstname"]));
        Assert.That(b.GetProperty("lastname").GetString(), Is.EqualTo((string?)expected["lastname"]));
        Assert.That(b.GetProperty("totalprice").GetInt32(), Is.EqualTo(Convert.ToInt32(expected["totalprice"])));
        Assert.That(b.GetProperty("depositpaid").GetBoolean(), Is.EqualTo(Convert.ToBoolean(expected["depositpaid"])));

        var dates = (Dictionary<string, string>)expected["bookingdates"]!;
        Assert.That(b.GetProperty("bookingdates").GetProperty("checkin").GetString(), Is.EqualTo(dates["checkin"]));
        Assert.That(b.GetProperty("bookingdates").GetProperty("checkout").GetString(), Is.EqualTo(dates["checkout"]));
    }

    public static int[] Ids(JsonElement array) =>
        array.EnumerateArray().Select(e => e.GetProperty("bookingid").GetInt32()).ToArray();

    public static void BadCredentials(ApiResult res)
    {
        Assert.That(res.Status, Is.EqualTo(200), "Restful-Booker signals auth failure in the body, not the status");
        Assert.That(res.Body.GetProperty("reason").GetString(), Is.EqualTo("Bad credentials"));
        Assert.That(res.Body.TryGetProperty("token", out _), Is.False, "no token may be issued");
    }
}
