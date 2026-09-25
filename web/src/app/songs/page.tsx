import type { Metadata } from "next";

import { SearchBox } from "@/components/search-box";
import { NewSongForm, SongList } from "@/components/song-list";
import { getArtists, getSongs, matches } from "@/lib/catalog";
import { getCurrentUser } from "@/lib/session";

export const metadata: Metadata = { title: "Songs" };

export default async function SongsPage({ searchParams }: PageProps<"/songs">) {
  const [songs, artists, user, params] = await Promise.all([getSongs(), getArtists(), getCurrentUser(), searchParams]);
  const query = typeof params.q === "string" ? params.q : "";
  const shown = songs.filter((song) => matches(query, song.name, song.artist.name));

  return (
    <div className="grid gap-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="grid gap-1">
          <h1 className="text-4xl font-semibold">Songs</h1>
          <p className="text-muted tabular-nums">
            {query ? `${shown.length} of ${songs.length}` : songs.length} {songs.length === 1 ? "song" : "songs"}
          </p>
        </div>
        <SearchBox placeholder="Search songs or artists" label="Search songs" />
      </div>

      {user &&
        (artists.length > 0 ? (
          <NewSongForm artists={artists} returnTo="/songs" />
        ) : (
          <p className="rounded-xl border border-line bg-surface p-4 text-muted">Add an artist first, then add their songs here.</p>
        ))}

      <SongList
        songs={shown}
        artists={artists}
        editable={!!user}
        returnTo="/songs"
        empty={query ? `No songs match “${query}”.` : "No songs yet."}
      />
    </div>
  );
}
