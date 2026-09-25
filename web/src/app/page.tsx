import Link from "next/link";

import { buttonClass } from "@/components/button-class";
import { Vinyl } from "@/components/vinyl";
import { addedAt, getArtists, getComposers, getSongs } from "@/lib/catalog";
import { getCurrentUser } from "@/lib/session";

const dateFormat = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });

export default async function HomePage({ searchParams }: PageProps<"/">) {
  const [artists, songs, composers, user, params] = await Promise.all([
    getArtists(),
    getSongs(),
    getComposers(),
    getCurrentUser(),
    searchParams,
  ]);

  const topArtists = [...artists].sort((a, b) => (b.songCount ?? 0) - (a.songCount ?? 0)).slice(0, 6);
  const recent = [...songs].sort((a, b) => b.id.localeCompare(a.id)).slice(0, 8);

  return (
    <div className="grid gap-12">
      {params.deleted ? (
        <p role="status" className="rounded-lg bg-success-soft px-4 py-3 text-success">
          Your account was deleted.
        </p>
      ) : null}

      <section className="grid items-center gap-8 md:grid-cols-[1.3fr_1fr]">
        <div className="grid gap-5">
          <p className="text-sm font-medium tracking-wider text-accent uppercase">Music catalog</p>
          <h1 className="text-4xl leading-[1.05] font-semibold sm:text-6xl">Every artist, every song, one shelf.</h1>
          <p className="max-w-[55ch] text-lg text-muted">
            Browse the catalog freely. Sign in to add artists and songs, fix a name, or move a track to another
            artist. Song listings update across the catalog the moment an artist is renamed.
          </p>
          <div className="flex flex-wrap gap-3">
            <Link href="/artists" className={buttonClass("primary")}>
              Browse artists
            </Link>
            {!user && (
              <Link href="/register" className={buttonClass("secondary")}>
                Create an account
              </Link>
            )}
          </div>
        </div>
        <dl className="grid grid-cols-3 gap-3">
          {[
            ["Artists", artists.length, "/artists"],
            ["Songs", songs.length, "/songs"],
            ["Composers", composers.length, "/composers"],
          ].map(([label, count, href]) => (
            <Link
              key={label}
              href={href as string}
              className="grid gap-1 rounded-xl border border-line bg-surface p-4 transition hover:border-accent"
            >
              <dt className="text-sm text-muted">{label}</dt>
              <dd className="font-display text-3xl font-semibold tabular-nums">{count}</dd>
            </Link>
          ))}
        </dl>
      </section>

      <section className="grid gap-4">
        <div className="flex items-baseline justify-between">
          <h2 className="text-2xl font-semibold">Most recorded artists</h2>
          <Link href="/artists" className="text-sm text-muted hover:text-ink">
            All artists →
          </Link>
        </div>
        {topArtists.length === 0 ? (
          <p className="rounded-xl border border-dashed border-line p-8 text-center text-muted">
            The shelf is empty. {user ? "Add the first artist." : "Sign in to add the first artist."}
          </p>
        ) : (
          <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
            {topArtists.map((artist) => (
              <li key={artist.id}>
                <Link
                  href={`/artists/${artist.id}`}
                  className="spin-on-hover grid justify-items-center gap-3 rounded-xl border border-line bg-surface p-4 text-center transition hover:border-accent"
                >
                  <Vinyl seed={artist.name} size={88} />
                  <span className="line-clamp-1 font-medium">{artist.name}</span>
                  <span className="text-sm text-muted tabular-nums">
                    {artist.songCount ?? 0} {artist.songCount === 1 ? "song" : "songs"}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>

      {recent.length > 0 && (
        <section className="grid gap-4">
          <div className="flex items-baseline justify-between">
            <h2 className="text-2xl font-semibold">Recently added</h2>
            <Link href="/songs" className="text-sm text-muted hover:text-ink">
              All songs →
            </Link>
          </div>
          <ol className="divide-y divide-line rounded-xl border border-line bg-surface">
            {recent.map((song) => {
              const added = addedAt(song.id);
              return (
                <li key={song.id} className="flex items-center gap-4 px-4 py-3">
                  <Vinyl seed={song.artist.name} size={36} />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium">{song.name}</p>
                    <Link href={`/artists/${song.artist.id}`} className="text-sm text-muted hover:text-ink">
                      {song.artist.name}
                    </Link>
                  </div>
                  {added && <time className="text-sm text-muted tabular-nums">{dateFormat.format(added)}</time>}
                </li>
              );
            })}
          </ol>
        </section>
      )}
    </div>
  );
}
