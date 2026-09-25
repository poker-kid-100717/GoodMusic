using MyMusic.Core.Models;

namespace MyMusic.API.Contracts;

public record ArtistResponse(string Id, string Name)
{
    public static ArtistResponse From(Artist artist) => new(artist.Id, artist.Name);
}

public record SaveArtistRequest(string Name);

public record MusicResponse(string Id, string Name, ArtistResponse Artist)
{
    public static MusicResponse From(Music music) =>
        new(music.Id, music.Name, new ArtistResponse(music.ArtistId, music.ArtistName));
}

public record SaveMusicRequest(string Name, string ArtistId);

public record ComposerResponse(string Id, string FirstName, string LastName)
{
    public static ComposerResponse From(Composer composer) => new(composer.Id, composer.FirstName, composer.LastName);
}

public record SaveComposerRequest(string FirstName, string LastName);

public record RegisterRequest(string Username, string Password, string FirstName, string LastName);

public record LoginRequest(string Username, string Password);

public record UpdateUserRequest(string FirstName, string LastName, string? Password);

public record UserResponse(string Id, string Username, string FirstName, string LastName)
{
    public static UserResponse From(User user) => new(user.Id, user.Username, user.FirstName, user.LastName);
}

public record TokenResponse(string Token, DateTimeOffset ExpiresAt, UserResponse User);
