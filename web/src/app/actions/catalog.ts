"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { api } from "@/lib/api";
import { submit, text } from "@/lib/form";
import { requireToken } from "@/lib/session";
import type { Artist, FormState } from "@/lib/types";

// Every action checks for a session itself: server actions are reachable by
// direct POST, not only through the UI. The API enforces auth again.

function refreshCatalog() {
  revalidatePath("/", "layout");
}

export async function createArtist(_: FormState, formData: FormData): Promise<FormState> {
  const token = await requireToken("/artists");
  let created: Artist | undefined;
  const state = await submit("/artists", async () => {
    created = await api<Artist>("/api/Artist", { method: "POST", token, body: { name: text(formData, "name") } });
  });
  if (!created) return state;
  refreshCatalog();
  redirect(`/artists/${created.id}`);
}

export async function renameArtist(id: string, _: FormState, formData: FormData): Promise<FormState> {
  const token = await requireToken(`/artists/${id}`);
  const state = await submit(`/artists/${id}`, () =>
    api(`/api/Artist/${id}`, { method: "PUT", token, body: { name: text(formData, "name") } }),
  );
  if (state.ok) refreshCatalog();
  return state;
}

export async function deleteArtist(id: string, _: FormState): Promise<FormState> {
  const token = await requireToken(`/artists/${id}`);
  const state = await submit(`/artists/${id}`, () => api(`/api/Artist/${id}`, { method: "DELETE", token }));
  if (!state.ok) return state;
  refreshCatalog();
  redirect("/artists");
}

export async function saveSong(id: string | null, _: FormState, formData: FormData): Promise<FormState> {
  const returnTo = text(formData, "returnTo") || "/songs";
  const token = await requireToken(returnTo);
  const body = { name: text(formData, "name"), artistId: text(formData, "artistId") };
  const state = await submit(returnTo, () =>
    id
      ? api(`/api/Music/${id}`, { method: "PUT", token, body })
      : api("/api/Music", { method: "POST", token, body }),
  );
  if (state.ok) refreshCatalog();
  return state;
}

export async function deleteSong(id: string, returnTo: string, _: FormState): Promise<FormState> {
  const token = await requireToken(returnTo);
  const state = await submit(returnTo, () => api(`/api/Music/${id}`, { method: "DELETE", token }));
  if (state.ok) refreshCatalog();
  return state;
}

export async function saveComposer(id: string | null, _: FormState, formData: FormData): Promise<FormState> {
  const token = await requireToken("/composers");
  const body = { firstName: text(formData, "firstName"), lastName: text(formData, "lastName") };
  const state = await submit("/composers", () =>
    id
      ? api(`/api/Composer/${id}`, { method: "PUT", token, body })
      : api("/api/Composer", { method: "POST", token, body }),
  );
  if (state.ok) refreshCatalog();
  return state;
}

export async function deleteComposer(id: string, _: FormState): Promise<FormState> {
  const token = await requireToken("/composers");
  const state = await submit("/composers", () => api(`/api/Composer/${id}`, { method: "DELETE", token }));
  if (state.ok) refreshCatalog();
  return state;
}
