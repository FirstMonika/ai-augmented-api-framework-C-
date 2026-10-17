using System.Text.Json;
using AiAugmentedApiFramework.Base;
using AiAugmentedApiFramework.Helpers;

namespace AiAugmentedApiFramework.Tests;

[TestFixture, Category("Auth")]
public class AuthTests : ApiTestBase
{
    // ---------- Positive ----------
    [Test]
    public async Task AUTH_001_ValidCredentialsReturnUsableToken()
    {
        var res = await Api.AuthAsync(DataFactory.Credentials(Cfg.Username, Cfg.Password));
        Assert.That(res.Status, Is.EqualTo(200));
        var token = res.Body.GetProperty("token").GetString();
        Assert.That(token, Has.Length.GreaterThanOrEqualTo(10));
        Assert.That(token, Does.Not.Contain("Bad credentials"));
        Assert.That(res.Body.TryGetProperty("reason", out _), Is.False);
    }

    [Test]
    public async Task AUTH_002_ContentTypeIsApplicationJson()
    {
        var res = await Api.AuthAsync(DataFactory.Credentials(Cfg.Username, Cfg.Password));
        Assert.That(res.Status, Is.EqualTo(200));
        Assert.That(res.Header("content-type"), Does.Contain("application/json"));
    }

    [Test]
    public async Task AUTH_003_SameCredentialsReturnSameToken()
    {
        var creds = DataFactory.Credentials(Cfg.Username, Cfg.Password);
        var first = await Api.AuthAsync(creds);
        var second = await Api.AuthAsync(creds);
        Assert.That(first.Status, Is.EqualTo(200));
        Assert.That(second.Status, Is.EqualTo(200));
        Assert.That(second.Body.GetProperty("token").GetString(),
            Is.EqualTo(first.Body.GetProperty("token").GetString()));
    }

    // ---------- Negative ----------
    [Test]
    public async Task AUTH_004_WrongPasswordReturnsBadCredentials() =>
        BookingAssert.BadCredentials(await Api.AuthAsync(DataFactory.Credentials(Cfg.Username, "WRONG")));

    [Test]
    public async Task AUTH_005_WrongUsernameReturnsBadCredentials() =>
        BookingAssert.BadCredentials(await Api.AuthAsync(DataFactory.Credentials("notauser", Cfg.Password)));

    [Test]
    public async Task AUTH_006_EmptyPasswordReturnsBadCredentials() =>
        BookingAssert.BadCredentials(await Api.AuthAsync(DataFactory.Credentials(Cfg.Username, "")));

    [Test]
    public async Task AUTH_007_EmptyUsernameReturnsBadCredentials() =>
        BookingAssert.BadCredentials(await Api.AuthAsync(DataFactory.Credentials("", Cfg.Password)));

    [Test]
    public async Task AUTH_008_MissingPasswordKeyIsRejected() =>
        BookingAssert.BadCredentials(await Api.AuthAsync(new Dictionary<string, object?> { ["username"] = Cfg.Username }));

    [Test]
    public async Task AUTH_009_MissingUsernameKeyIsRejected() =>
        BookingAssert.BadCredentials(await Api.AuthAsync(new Dictionary<string, object?> { ["password"] = Cfg.Password }));

    // ---------- Edge / boundary ----------
    [Test]
    public async Task AUTH_010_SqlInjectionInPasswordDoesNotCrash()
    {
        var res = await Api.AuthAsync(DataFactory.Credentials(Cfg.Username, "' OR '1'='1"));
        Assert.That(res.Status, Is.Not.EqualTo(500));
        BookingAssert.BadCredentials(res);
        Assert.That(res.Text, Does.Not.Contain("SQL").IgnoreCase);
        Assert.That(res.Text, Does.Not.Contain("stack").IgnoreCase);
    }

    [Test]
    public async Task AUTH_011_VeryLongPasswordHandledGracefully()
    {
        var res = await Api.AuthAsync(DataFactory.Credentials(Cfg.Username, DataFactory.Long(1000)));
        Assert.That(res.Status, Is.Not.EqualTo(500));
        Assert.That(res.Json, Is.Not.Null, "response must be valid JSON");
    }

    [Test]
    public async Task AUTH_012_EmptyBodyReturnsBadCredentials() =>
        BookingAssert.BadCredentials(await Api.AuthAsync(new Dictionary<string, object?>()));
}
