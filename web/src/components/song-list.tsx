"use client";

import Link from "next/link";
import { useActionState, useId, useState } from "react";

import { deleteSong, saveSong } from "@/app/actions/catalog";
import type { Artist, FormState, Song } from "@/lib/types";

import { ConfirmDelete } from "./confirm-delete";
import { buttonClass, Field, FormMessage, initialState, SubmitButton } from "./form";
import { Vinyl } from "./vinyl";

export function ArtistSelect({
  artists,
  defaultValue,
  errors,
  id,
}: {
  artists: Artist[];
  defaultValue?: string;
  errors?: string[];
  id?: string;
}) {
  const autoId = useId();
  id ??= autoId;
  return (
    <div className="grid gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">
        Artist
      </label>
      <select
        id={id}
        name="artistId"
        required
        defaultValue={defaultValue ?? ""}
        aria-invalid={errors?.length ? true : undefined}
        className="h-10 rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-accent aria-invalid:border-danger"
      >
        <option value="" disabled>
          Choose an artist
        </option>
        {artists.map((artist) => (
          <option key={artist.id} value={artist.id}>
            {artist.name}
          </option>
        ))}
      </select>
      {errors?.length ? <p className="text-sm text-danger">{errors[0]}</p> : null}
    </div>
  );
}

/** Adds a song; on an artist's page the artist is fixed and the picker is hidden. */
export function NewSongForm({ artists, fixedArtistId, returnTo }: { artists: Artist[]; fixedArtistId?: string; returnTo: string }) {
  const [formKey, setFormKey] = useState(0);
  const [state, action] = useActionState(async (previous: FormState, formData: FormData) => {
    const result = await saveSong(null, previous, formData);
    // Clear the inputs after a successful add so the next song starts fresh.
    if (result.ok) setFormKey((key) => key + 1);
    return result;
  }, initialState);

  return (
    <form key={formKey} action={action} className="grid gap-3 rounded-xl border border-line bg-surface p-4 sm:p-5">
      <input type="hidden" name="returnTo" value={returnTo} />
      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-56 flex-1">
          <Field label="New song" name="name" required maxLength={50} placeholder="Song title" errors={state.fieldErrors?.name} />
        </div>
        {fixedArtistId ? (
          <input type="hidden" name="artistId" value={fixedArtistId} />
        ) : (
          <div className="min-w-48">
            <ArtistSelect artists={artists} errors={state.fieldErrors?.artistId} id="new-song-artist" />
          </div>
        )}
        <SubmitButton pendingLabel="Adding…">Add song</SubmitButton>
      </div>
      {!state.ok && <FormMessage state={state} />}
    </form>
  );
}

function SongRow({ song, artists, editable, showArtist, returnTo }: { song: Song; artists: Artist[]; editable: boolean; showArtist: boolean; returnTo: string }) {
  const [editing, setEditing] = useState(false);
  const [state, action] = useActionState(async (previous: FormState, formData: FormData) => {
    const result = await saveSong(song.id, previous, formData);
    if (result.ok) setEditing(false);
    return result;
  }, initialState);

  if (editing) {
    return (
      <li className="bg-surface-2/60 px-4 py-4">
        <form action={action} className="grid gap-3">
          <input type="hidden" name="returnTo" value={returnTo} />
          <div className="grid gap-3 sm:grid-cols-[1fr_16rem]">
            <Field label="Title" name="name" id={`name-${song.id}`} defaultValue={song.name} required maxLength={50} errors={state.fieldErrors?.name} />
            <ArtistSelect artists={artists} defaultValue={song.artist.id} errors={state.fieldErrors?.artistId} id={`artist-${song.id}`} />
          </div>
          <FormMessage state={state} />
          <div className="flex gap-2">
            <SubmitButton size="sm">Save</SubmitButton>
            <button type="button" className={buttonClass("secondary", "sm")} onClick={() => setEditing(false)}>
              Cancel
            </button>
          </div>
        </form>
      </li>
    );
  }

  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-2 px-4 py-3">
      {showArtist && <Vinyl seed={song.artist.name} size={36} />}
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium">{song.name}</p>
        {showArtist && (
          <Link href={`/artists/${song.artist.id}`} className="text-sm text-muted hover:text-ink">
            {song.artist.name}
          </Link>
        )}
      </div>
      {editable && (
        <div className="flex items-start gap-1">
          <button type="button" className={buttonClass("ghost", "sm")} onClick={() => setEditing(true)} aria-label={`Edit ${song.name}`}>
            Edit
          </button>
          <ConfirmDelete action={deleteSong.bind(null, song.id, returnTo)} question={`Delete “${song.name}”?`} />
        </div>
      )}
    </li>
  );
}

export function SongList({
  songs,
  artists,
  editable,
  showArtist = true,
  returnTo,
  empty,
}: {
  songs: Song[];
  artists: Artist[];
  editable: boolean;
  showArtist?: boolean;
  returnTo: string;
  empty: string;
}) {
  if (songs.length === 0) {
    return <p className="rounded-xl border border-dashed border-line p-10 text-center text-muted">{empty}</p>;
  }
  return (
    <ul className="divide-y divide-line overflow-hidden rounded-xl border border-line bg-surface">
      {songs.map((song) => (
        <SongRow key={song.id} song={song} artists={artists} editable={editable} showArtist={showArtist} returnTo={returnTo} />
      ))}
    </ul>
  );
}
