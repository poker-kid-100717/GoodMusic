import "server-only";

import { redirect } from "next/navigation";

import { ApiError } from "./api";
import type { FormState } from "./types";

export function text(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}

/**
 * Runs an API mutation for a form and turns API failures into form state:
 * validation problems become field errors, 409s and 404s become a message,
 * and a rejected session sends the user to sign in again.
 */
export async function submit(returnTo: string, mutation: () => Promise<void>): Promise<FormState> {
  try {
    await mutation();
    return { ok: true };
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 401) redirect(`/login?next=${encodeURIComponent(returnTo)}&expired=1`);
      return { message: error.message, fieldErrors: error.fieldErrors };
    }
    throw error;
  }
}
