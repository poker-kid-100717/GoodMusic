using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MyMusic.API.Contracts;
using MyMusic.Mongo.Db;

namespace MyMusic.API.Tests;

[Collection(ApiCollection.Name)]
public class CatalogTests(ApiFixture fixture)
{
    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..6]}";

    [Fact]
    public async Task Reads_are_public_but_writes_require_a_token()
    {
        var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/Artist")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/Artist", new SaveArtistRequest("Nope"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/Composer", new SaveComposerRequest("No", "Pe"))).StatusCode);
    }

    [Fact]
    public async Task Song_carries_its_artist_name_and_follows_a_rename()
    {
        var (client, _) = await fixture.CreateSignedInClientAsync();

        var artist = await Created<ArtistResponse>(await client.PostAsJsonAsync("/api/Artist", new SaveArtistRequest(Unique("Queen"))));
        var song = await Created<MusicResponse>(await client.PostAsJsonAsync("/api/Music", new SaveMusicRequest("Bohemian Rhapsody", artist.Id)));

        Assert.Equal(artist.Id, song.Artist.Id);
        Assert.Equal(artist.Name, song.Artist.Name);

        var renamed = Unique("Queen (Remastered)");
        (await client.PutAsJsonAsync($"/api/Artist/{artist.Id}", new SaveArtistRequest(renamed))).EnsureSuccessStatusCode();

        var reloaded = await client.GetFromJsonAsync<MusicResponse>($"/api/Music/{song.Id}");
        Assert.Equal(renamed, reloaded!.Artist.Name);

        var byArtist = await client.GetFromJsonAsync<List<MusicResponse>>($"/api/Music/artist/{artist.Id}");
        Assert.Equal([song.Id], byArtist!.Select(m => m.Id));
    }

    [Fact]
    public async Task Artist_with_songs_cannot_be_deleted()
    {
        var (client, _) = await fixture.CreateSignedInClientAsync();

        var artist = await Created<ArtistResponse>(await client.PostAsJsonAsync("/api/Artist", new SaveArtistRequest(Unique("Muse"))));
        var song = await Created<MusicResponse>(await client.PostAsJsonAsync("/api/Music", new SaveMusicRequest("Uprising", artist.Id)));

        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/Artist/{artist.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Music/{song.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Artist/{artist.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Artist/{artist.Id}")).StatusCode);
    }

    [Fact]
    public async Task Song_for_unknown_artist_is_rejected()
    {
        var (client, _) = await fixture.CreateSignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Music", new SaveMusicRequest("Orphan", "65f000000000000000000000"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_input_returns_validation_problem()
    {
        var (client, _) = await fixture.CreateSignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Music", new SaveMusicRequest("", ""));
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", problem!.Keys);
    }

    [Fact]
    public async Task Songs_for_a_malformed_artist_id_is_an_empty_list()
    {
        var songs = await fixture.CreateClient().GetFromJsonAsync<List<MusicResponse>>("/api/Music/artist/not-an-object-id");

        Assert.Empty(songs!);
    }

    [Fact]
    public async Task Song_references_its_artist_by_object_id()
    {
        var (client, _) = await fixture.CreateSignedInClientAsync();
        var artist = await Created<ArtistResponse>(await client.PostAsJsonAsync("/api/Artist", new SaveArtistRequest(Unique("Daft Punk"))));
        var song = await Created<MusicResponse>(await client.PostAsJsonAsync("/api/Music", new SaveMusicRequest("One More Time", artist.Id)));

        var raw = await fixture.Factory.Services.GetRequiredService<MongoContext>().Database
            .GetCollection<BsonDocument>("musics")
            .Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(song.Id)))
            .SingleAsync();

        Assert.Equal(BsonType.ObjectId, raw["artistId"].BsonType);
        Assert.Equal(artist.Name, raw["artistName"].AsString);
    }

    [Theory]
    [InlineData("/api/Artist/not-an-object-id")]
    [InlineData("/api/Music/not-an-object-id")]
    [InlineData("/api/Composer/65f000000000000000000000")]
    public async Task Unknown_or_malformed_ids_are_not_found(string url)
    {
        var response = await fixture.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Composer_crud()
    {
        var (client, _) = await fixture.CreateSignedInClientAsync();

        var composer = await Created<ComposerResponse>(await client.PostAsJsonAsync("/api/Composer", new SaveComposerRequest("Hans", "Zimmer")));

        var updated = await (await client.PutAsJsonAsync($"/api/Composer/{composer.Id}", new SaveComposerRequest("Hans", "Zimmer Jr.")))
            .Content.ReadFromJsonAsync<ComposerResponse>();
        Assert.Equal("Zimmer Jr.", updated!.LastName);

        var all = await client.GetFromJsonAsync<List<ComposerResponse>>("/api/Composer");
        Assert.Contains(all!, c => c.Id == composer.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Composer/{composer.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Composer/{composer.Id}")).StatusCode);
    }

    private static async Task<T> Created<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
