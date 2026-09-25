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
    public async Task Artist_song_count_follows_song_writes()
    {
        var (client, _) = await fixture.CreateSignedInClientAsync();
        var first = await Created<ArtistResponse>(await client.PostAsJsonAsync("/api/Artist", new SaveArtistRequest(Unique("First"))));
        var second = await Created<ArtistResponse>(await client.PostAsJsonAsync("/api/Artist", new SaveArtistRequest(Unique("Second"))));

        var song = await Created<MusicResponse>(await client.PostAsJsonAsync("/api/Music", new SaveMusicRequest("Track", first.Id)));
        Assert.Equal(1, (await GetArtist(client, first.Id)).SongCount);

        // Moving the song moves the count.
        (await client.PutAsJsonAsync($"/api/Music/{song.Id}", new SaveMusicRequest("Track", second.Id))).EnsureSuccessStatusCode();
        Assert.Equal(0, (await GetArtist(client, first.Id)).SongCount);
        Assert.Equal(1, (await GetArtist(client, second.Id)).SongCount);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Artist/{first.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/Artist/{second.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/Music/{song.Id}")).StatusCode);
        Assert.Equal(0, (await GetArtist(client, second.Id)).SongCount);
    }

    [Fact]
    public async Task Concurrent_song_creation_never_leaves_an_orphan()
    {
        var (client, _) = await fixture.CreateSignedInClientAsync();

        for (var round = 0; round < 10; round++)
        {
            var artist = await Created<ArtistResponse>(await client.PostAsJsonAsync("/api/Artist", new SaveArtistRequest(Unique("Racer"))));

            var create = client.PostAsJsonAsync("/api/Music", new SaveMusicRequest("Race", artist.Id));
            var delete = client.DeleteAsync($"/api/Artist/{artist.Id}");
            var (created, deleted) = (await create, await delete);

            // Exactly one side wins: either the song exists and the artist too,
            // or the artist is gone and the song was rejected.
            if (deleted.StatusCode == HttpStatusCode.NoContent)
            {
                Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
                Assert.Empty((await client.GetFromJsonAsync<List<MusicResponse>>($"/api/Music/artist/{artist.Id}"))!);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
                Assert.Equal(HttpStatusCode.Created, created.StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/Artist/{artist.Id}")).StatusCode);
            }
        }
    }

    [Fact]
    public async Task Legacy_composers_collection_is_migrated()
    {
        var context = fixture.Factory.Services.GetRequiredService<MongoContext>();
        var legacyId = ObjectId.GenerateNewId();
        await context.Database.GetCollection<BsonDocument>("Composers").InsertOneAsync(
            new BsonDocument { { "_id", legacyId }, { "FirstName", "Ennio" }, { "LastName", "Morricone" } });

        await context.MigrateLegacyComposersAsync();
        await context.MigrateLegacyComposersAsync(); // idempotent

        var composer = await fixture.CreateClient().GetFromJsonAsync<ComposerResponse>($"/api/Composer/{legacyId}");
        Assert.Equal(("Ennio", "Morricone"), (composer!.FirstName, composer.LastName));
        var names = await (await context.Database.ListCollectionNamesAsync()).ToListAsync();
        Assert.DoesNotContain("Composers", names);
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

    private static async Task<ArtistResponse> GetArtist(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<ArtistResponse>($"/api/Artist/{id}"))!;

    private static async Task<T> Created<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
