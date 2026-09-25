namespace MyMusic.Core.Models;

public class Artist
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of songs referencing this artist, changed atomically with each
    /// song write. Deletion is conditional on it being zero, which is what
    /// makes "no orphaned songs" hold under concurrent requests.
    /// </summary>
    public int SongCount { get; set; }
}
