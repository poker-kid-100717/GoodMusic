using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MyMusic.API.Contracts;
using Testcontainers.MongoDb;

namespace MyMusic.API.Tests;

/// <summary>
/// One MongoDB container and one in-process API for the whole test run.
/// Tests use unique names, so they don't depend on each other's data.
/// MongoDB runs as a single-node replica set, which transactions require.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8.0").WithReplicaSet().Build();
    private WebApplicationFactory<Program>? _factory;

    public WebApplicationFactory<Program> Factory => _factory!;

    public async Task InitializeAsync()
    {
        await _mongo.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("MongoDB:ConnectionString", _mongo.GetConnectionString());
            builder.UseSetting("MongoDB:Database", "GoodMusicTests");
            builder.UseSetting("Jwt:Key", "integration-test-signing-key-0123456789abcdef");
        });
    }

    public HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>Registers a fresh user and returns a client that sends its token.</summary>
    public async Task<(HttpClient Client, TokenResponse Session)> CreateSignedInClientAsync()
    {
        var client = CreateClient();
        var username = "user" + Guid.NewGuid().ToString("N")[..10];
        var response = await client.PostAsJsonAsync("/api/User/register",
            new RegisterRequest(username, "Passw0rd!", "Test", "User"));
        response.EnsureSuccessStatusCode();

        var session = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return (client, session);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
        await _mongo.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
