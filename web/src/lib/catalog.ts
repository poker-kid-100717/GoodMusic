import "server-only";

import { api } from "./api";
import type { Artist, Composer, Song } from "./types";

export const getArtists = () => api<Artist[]>("/api/Artist");
export const getSongs = () => api<Song[]>("/api/Music");
export const getComposers = () => api<Composer[]>("/api/Composer");

/** MongoDB ObjectIds start with their creation time in seconds, so ids double as "added" dates. */
export function addedAt(id: string): Date | null {
  return /^[0-9a-f]{24}$/i.test(id) ? new Date(parseInt(id.slice(0, 8), 16) * 1000) : null;
}

export function matches(query: string, ...values: string[]): boolean {
  const needle = query.trim().toLowerCase();
  return !needle || values.some((value) => value.toLowerCase().includes(needle));
}
