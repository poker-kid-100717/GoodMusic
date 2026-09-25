import "server-only";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { cache } from "react";

import { api, ApiError } from "./api";
import type { TokenResponse, User } from "./types";

/**
 * The API's JWT lives in an httpOnly cookie, so browser JavaScript can never
 * read it. Pages and server actions read it here and attach it to API calls.
 */
const COOKIE = "gm_session";

export async function getToken(): Promise<string | undefined> {
  return (await cookies()).get(COOKIE)?.value;
}

export async function startSession(session: TokenResponse): Promise<void> {
  (await cookies()).set(COOKIE, session.token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    expires: new Date(session.expiresAt),
  });
}

export async function endSession(): Promise<void> {
  (await cookies()).delete(COOKIE);
}

/**
 * The signed-in user, or null. The API rejects tokens that have expired or
 * whose account was deleted, so a stale cookie reads as signed out.
 * Cached per request, so the header and the page share one lookup.
 */
export const getCurrentUser = cache(async (): Promise<User | null> => {
  const token = await getToken();
  if (!token) return null;
  try {
    return await api<User>("/api/User/me", { token });
  } catch (error) {
    if (error instanceof ApiError && (error.status === 401 || error.status === 404)) return null;
    throw error;
  }
});

/** For server actions that need a signed-in user: returns the token or sends them to sign in. */
export async function requireToken(returnTo: string): Promise<string> {
  const token = await getToken();
  if (!token) redirect(`/login?next=${encodeURIComponent(returnTo)}`);
  return token;
}
