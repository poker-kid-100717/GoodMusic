using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MyMusic.API.Contracts;
using MyMusic.Mongo.Db;

namespace MyMusic.API.Tests;

[Collection(ApiCollection.Name)]
public class UserTests(ApiFixture fixture)
{
    [Fact]
    public async Task Register_then_log_in_with_any_casing()
    {
        var client = fixture.CreateClient();
        var username = "Mixed" + Guid.NewGuid().ToString("N")[..8];

        var registered = await client.PostAsJsonAsync("/api/User/register", new RegisterRequest(username, "Passw0rd!", "Ada", "Lovelace"));
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var login = await client.PostAsJsonAsync("/api/User/authenticate", new LoginRequest(username.ToUpperInvariant(), "Passw0rd!"));
        var session = await login.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(username.ToLowerInvariant(), session!.User.Username);
        Assert.False(string.IsNullOrEmpty(session.Token));
    }

    [Fact]
    public async Task Duplicate_username_is_a_conflict()
    {
        var client = fixture.CreateClient();
        var username = "dup" + Guid.NewGuid().ToString("N")[..8];

        (await client.PostAsJsonAsync("/api/User/register", new RegisterRequest(username, "Passw0rd!", "A", "B"))).EnsureSuccessStatusCode();
        var second = await client.PostAsJsonAsync("/api/User/register", new RegisterRequest(username.ToUpperInvariant(), "Passw0rd!", "C", "D"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_is_unauthorized()
    {
        var (_, session) = await fixture.CreateSignedInClientAsync();

        var response = await fixture.CreateClient().PostAsJsonAsync("/api/User/authenticate", new LoginRequest(session.User.Username, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Short_password_is_rejected()
    {
        var response = await fixture.CreateClient().PostAsJsonAsync("/api/User/register", new RegisterRequest("shorty", "short", "A", "B"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_manages_only_their_own_account()
    {
        var (client, session) = await fixture.CreateSignedInClientAsync();

        var me = await client.GetFromJsonAsync<UserResponse>("/api/User/me");
        Assert.Equal(session.User.Id, me!.Id);

        (await client.PutAsJsonAsync("/api/User/me", new UpdateUserRequest("New", "Name", "N3wPassword!"))).EnsureSuccessStatusCode();
        var relogin = await fixture.CreateClient().PostAsJsonAsync("/api/User/authenticate", new LoginRequest(session.User.Username, "N3wPassword!"));
        Assert.Equal(HttpStatusCode.OK, relogin.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/User/me")).StatusCode);

        // The deleted user's still-unexpired token no longer works.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/Artist", new SaveArtistRequest("After deletion"))).StatusCode);
    }

    [Fact]
    public async Task Me_requires_a_token()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().GetAsync("/api/User/me")).StatusCode);
    }

    [Fact]
    public async Task Password_is_stored_hashed_and_username_is_uniquely_indexed()
    {
        var (_, session) = await fixture.CreateSignedInClientAsync();
        var context = fixture.Factory.Services.GetRequiredService<MongoContext>();

        var stored = await context.Users.Find(u => u.Id == session.User.Id).SingleAsync();
        Assert.NotEqual("Passw0rd!", stored.PasswordHash);
        Assert.StartsWith("AQAAAA", stored.PasswordHash); // ASP.NET Core Identity v3 (PBKDF2) format

        var indexes = await (await context.Users.Indexes.ListAsync()).ToListAsync();
        Assert.Contains(indexes, i => i["key"].AsBsonDocument.Contains("username") && i.GetValue("unique", false).ToBoolean());
    }

    [Fact]
    public async Task Overlapping_profile_updates_never_undo_a_password_change()
    {
        var (client, session) = await fixture.CreateSignedInClientAsync();
        var login = fixture.CreateClient();

        for (var round = 0; round < 10; round++)
        {
            var password = $"Round{round}Pass!";
            var results = await Task.WhenAll(
                client.PutAsJsonAsync("/api/User/me", new UpdateUserRequest("Name", $"Only {round}", null)),
                client.PutAsJsonAsync("/api/User/me", new UpdateUserRequest("Name", $"Password {round}", password)));
            Assert.All(results, r => r.EnsureSuccessStatusCode());

            var signIn = await login.PostAsJsonAsync("/api/User/authenticate", new LoginRequest(session.User.Username, password));
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        }
    }

    [Fact]
    public void Startup_rejects_a_signing_key_shorter_than_256_bits()
    {
        using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:Key", "too-short-key"));

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("256 bits", error.Message);
    }

    [Fact]
    public async Task Health_endpoint_checks_mongodb()
    {
        var response = await fixture.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
