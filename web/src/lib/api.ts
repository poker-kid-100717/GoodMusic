import "server-only";

import { getCloudflareContext } from "@opennextjs/cloudflare";

/**
 * Server-side client for the ASP.NET Core API.
 *
 * On Cloudflare the call goes straight to the API container through its
 * Durable Object binding, never over the public internet. In `next dev` it
 * goes to API_URL (the API running locally, http://localhost:5080 by default).
 * The browser never talks to the API directly: pages and actions call it here,
 * with the user's token taken from an httpOnly cookie.
 */

interface ContainerBinding {
  getByName(name: string): { fetch(request: Request): Promise<Response> };
}

function transport(): (request: Request) => Promise<Response> {
  try {
    const env = getCloudflareContext().env as unknown as { API?: ContainerBinding };
    if (env.API) {
      const api = env.API.getByName("api");
      return (request) => api.fetch(request);
    }
  } catch {
    // Not running on Workers (next dev, tests): fall through to HTTP.
  }
  return (request) => fetch(request);
}

function baseUrl(): string {
  return process.env.API_URL ?? "http://localhost:5080";
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly fieldErrors?: Record<string, string[]>,
  ) {
    super(message);
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Turns "Name" / "ArtistId" style keys from ProblemDetails into form field names. */
function toFieldName(key: string): string {
  return key.charAt(0).toLowerCase() + key.slice(1);
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {};
  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    // Not JSON; fall back to the status text.
  }

  const fieldErrors = problem.errors
    ? Object.fromEntries(Object.entries(problem.errors).map(([key, messages]) => [toFieldName(key), messages]))
    : undefined;

  const message =
    problem.detail ??
    (fieldErrors ? "Please fix the highlighted fields." : undefined) ??
    problem.title ??
    `The request failed (${response.status}).`;

  return new ApiError(response.status, message, fieldErrors);
}

export interface ApiOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  token?: string;
}

export async function api<T = void>(path: string, { method = "GET", body, token }: ApiOptions = {}): Promise<T> {
  const headers = new Headers({ accept: "application/json" });
  if (body !== undefined) headers.set("content-type", "application/json");
  if (token) headers.set("authorization", `Bearer ${token}`);

  const request = new Request(new URL(path, baseUrl()), {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    cache: "no-store",
  });

  const response = await transport()(request);
  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204 || response.headers.get("content-length") === "0") {
    return undefined as T;
  }
  return (await response.json()) as T;
}
