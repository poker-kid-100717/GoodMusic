namespace MyMusic.Core.Models;

/// <summary>
/// A song. The artist's name is stored alongside the artist id so listing
/// songs never needs a second query (a join, in relational terms).
/// ArtistService keeps the copy in sync when an artist is renamed.
/// </summary>
public class Music
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;
}
