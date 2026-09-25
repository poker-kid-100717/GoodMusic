import type { Metadata } from "next";
import Link from "next/link";

import { SearchBox } from "@/components/search-box";
import { Vinyl } from "@/components/vinyl";
import { getArtists, matches } from "@/lib/catalog";
import { getCurrentUser } from "@/lib/session";

import { NewArtistForm } from "./new-artist-form";

export const metadata: Metadata = { title: "Artists" };

export default async function ArtistsPage({ searchParams }: PageProps<"/artists">) {
  const [artists, user, params] = await Promise.all([getArtists(), getCurrentUser(), searchParams]);
  const query = typeof params.q === "string" ? params.q : "";
  const shown = artists.filter((artist) => matches(query, artist.name));

  return (
    <div className="grid gap-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="grid gap-1">
          <h1 className="text-4xl font-semibold">Artists</h1>
          <p className="text-muted tabular-nums">
            {artists.length} {artists.length === 1 ? "artist" : "artists"} in the catalog
          </p>
        </div>
        <SearchBox placeholder="Search artists" label="Search artists" />
      </div>

      {user && <NewArtistForm />}

      {shown.length === 0 ? (
        <p className="rounded-xl border border-dashed border-line p-10 text-center text-muted">
          {query ? `No artists match “${query}”.` : "No artists yet."}
        </p>
      ) : (
        <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          {shown.map((artist) => (
            <li key={artist.id}>
              <Link
                href={`/artists/${artist.id}`}
                className="spin-on-hover flex items-center gap-4 rounded-xl border border-line bg-surface p-4 transition hover:border-accent"
              >
                <Vinyl seed={artist.name} size={56} />
                <span className="min-w-0">
                  <span className="block truncate font-medium">{artist.name}</span>
                  <span className="text-sm text-muted tabular-nums">
                    {artist.songCount ?? 0} {artist.songCount === 1 ? "song" : "songs"}
                  </span>
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
