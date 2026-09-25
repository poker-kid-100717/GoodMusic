import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { deleteArtist } from "@/app/actions/catalog";
import { ConfirmDelete } from "@/components/confirm-delete";
import { NewSongForm, SongList } from "@/components/song-list";
import { Vinyl } from "@/components/vinyl";
import { api, ApiError } from "@/lib/api";
import { getArtists } from "@/lib/catalog";
import { getCurrentUser } from "@/lib/session";
import type { Artist, Song } from "@/lib/types";

import { RenameArtistForm } from "./rename-artist-form";

async function getArtist(id: string): Promise<Artist> {
  try {
    return await api<Artist>(`/api/Artist/${encodeURIComponent(id)}`);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound();
    throw error;
  }
}

export async function generateMetadata({ params }: PageProps<"/artists/[id]">): Promise<Metadata> {
  const { id } = await params;
  return { title: (await getArtist(id)).name };
}

export default async function ArtistPage({ params }: PageProps<"/artists/[id]">) {
  const { id } = await params;
  const [artist, songs, artists, user] = await Promise.all([
    getArtist(id),
    api<Song[]>(`/api/Music/artist/${encodeURIComponent(id)}`),
    getArtists(),
    getCurrentUser(),
  ]);
  const returnTo = `/artists/${artist.id}`;

  return (
    <div className="grid gap-10">
      <Link href="/artists" className="text-sm text-muted hover:text-ink">
        ← All artists
      </Link>

      <section className="spin-on-hover flex flex-wrap items-center gap-6">
        <Vinyl seed={artist.name} size={132} />
        <div className="grid gap-2">
          <p className="text-sm font-medium tracking-wider text-accent uppercase">Artist</p>
          <h1 className="text-4xl font-semibold sm:text-5xl">{artist.name}</h1>
          <p className="text-muted tabular-nums">
            {songs.length} {songs.length === 1 ? "song" : "songs"}
          </p>
        </div>
      </section>

      <section className="grid gap-4">
        <h2 className="text-2xl font-semibold">Songs</h2>
        {user && <NewSongForm artists={artists} fixedArtistId={artist.id} returnTo={returnTo} />}
        <SongList
          songs={songs}
          artists={artists}
          editable={!!user}
          showArtist={false}
          returnTo={returnTo}
          empty={user ? "No songs yet. Add the first one above." : "No songs yet."}
        />
      </section>

      {user && (
        <section className="grid gap-4 rounded-xl border border-line bg-surface p-5">
          <h2 className="text-xl font-semibold">Manage artist</h2>
          <RenameArtistForm id={artist.id} name={artist.name} />
          <div className="grid gap-2 border-t border-line pt-4">
            <p className="text-sm text-muted">
              An artist can only be deleted once it has no songs. Move or delete its songs first.
            </p>
            <div>
              <ConfirmDelete
                action={deleteArtist.bind(null, artist.id)}
                label="Delete artist"
                question={`Delete ${artist.name}?`}
              />
            </div>
          </div>
        </section>
      )}
    </div>
  );
}
