namespace AiAugmentedApiFramework.Helpers;

/// <summary>Deterministic test data. Payloads are dictionaries so tests can remove or retype fields freely.</summary>
public static class DataFactory
{
    public static Dictionary<string, object?> Credentials(string username, string password) =>
        new() { ["username"] = username, ["password"] = password };

    public static Dictionary<string, string> Dates(string checkin, string checkout) =>
        new() { ["checkin"] = checkin, ["checkout"] = checkout };

    public static Dictionary<string, object?> ValidBooking() => new()
    {
        ["firstname"] = "Jim",
        ["lastname"] = "Brown",
        ["totalprice"] = 150,
        ["depositpaid"] = true,
        ["bookingdates"] = Dates("2025-06-01", "2025-06-10"),
        ["additionalneeds"] = "Breakfast",
    };

    /// <summary>Every field differs from ValidBooking - used for PUT.</summary>
    public static Dictionary<string, object?> AlternateBooking() => new()
    {
        ["firstname"] = "Alice",
        ["lastname"] = "Walker",
        ["totalprice"] = 999,
        ["depositpaid"] = false,
        ["bookingdates"] = Dates("2025-09-01", "2025-09-05"),
        ["additionalneeds"] = "Late checkout",
    };

    public static Dictionary<string, object?> MinimalBooking()
    {
        var p = ValidBooking();
        p.Remove("additionalneeds");
        return p;
    }

    public static Dictionary<string, object?> Without(this Dictionary<string, object?> p, string key)
    {
        var copy = new Dictionary<string, object?>(p);
        copy.Remove(key);
        return copy;
    }

    public static string Unique(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)];

    public static string Long(int length, char c = 'x') => new(c, length);
}
