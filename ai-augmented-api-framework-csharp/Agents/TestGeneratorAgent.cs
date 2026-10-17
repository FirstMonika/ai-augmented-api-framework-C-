namespace AiAugmentedApiFramework.Agents;

public sealed record EndpointSpec(string Method, string Path, string Description, string RequestSchema, string ResponseSchema);

/// <summary>Turns an endpoint specification into structured positive / negative / edge / security test cases.</summary>
public sealed class TestGeneratorAgent : BaseAgent
{
    public static readonly EndpointSpec[] RestfulBookerSpec =
    {
        new("POST",   "/auth",         "Create an auth token",         "{username, password}", "{token} or {reason}"),
        new("GET",    "/booking",      "List booking ids (filterable by firstname, lastname, checkin, checkout)", "query string", "[{bookingid}]"),
        new("GET",    "/booking/{id}", "Get one booking",              "path id", "booking object"),
        new("POST",   "/booking",      "Create a booking",             "{firstname,lastname,totalprice,depositpaid,bookingdates{checkin,checkout},additionalneeds?}", "{bookingid, booking}"),
        new("PUT",    "/booking/{id}", "Replace a booking (Cookie: token=...)", "full booking object", "booking object"),
        new("PATCH",  "/booking/{id}", "Partially update a booking (Cookie: token=...)", "any subset of booking fields", "booking object"),
        new("DELETE", "/booking/{id}", "Delete a booking (Cookie: token=...)", "path id", "201 Created"),
        new("GET",    "/ping",         "Health check",                 "none", "201 Created"),
    };

    public async Task<string?> GenerateAsync(EndpointSpec spec)
    {
        const string system =
            "You are a senior API test designer. Respond with JSON only: " +
            "{ \"endpoint\": string, \"testCases\": [ { \"id\": string, \"category\": \"positive|negative|edge|security\", " +
            "\"title\": string, \"steps\": string[], \"expected\": string } ] }.";
        var user = $"Design 8-12 test cases for {spec.Method} {spec.Path} - {spec.Description}.\n" +
                   $"Request: {spec.RequestSchema}\nResponse: {spec.ResponseSchema}\n" +
                   "Cover positive, negative, edge/boundary and security scenarios. Assert more than status codes.";
        var raw = await ChatAsync(system, user, jsonMode: true);
        return raw is null ? null : Pretty(raw);
    }

    public async Task<string?> GenerateAllAsync()
    {
        var parts = new List<string>();
        foreach (var spec in RestfulBookerSpec)
        {
            var json = await GenerateAsync(spec);
            if (json is null) return null;      // no API key
            parts.Add(json);
        }
        return "[\n" + string.Join(",\n", parts) + "\n]";
    }
}
