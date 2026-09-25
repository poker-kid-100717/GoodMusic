"use server";

import { redirect } from "next/navigation";

import { api, ApiError } from "@/lib/api";
import { text } from "@/lib/form";
import { endSession, startSession } from "@/lib/session";
import type { FormState, TokenResponse } from "@/lib/types";

/** Only same-site paths, so a crafted ?next= can't bounce users elsewhere. */
function safeNext(value: string): string {
  return value.startsWith("/") && !value.startsWith("//") ? value : "/";
}

export async function login(_: FormState, formData: FormData): Promise<FormState> {
  const username = text(formData, "username");
  const password = String(formData.get("password") ?? "");
  try {
    await startSession(await api<TokenResponse>("/api/User/authenticate", { method: "POST", body: { username, password } }));
  } catch (error) {
    if (error instanceof ApiError) return { message: error.message, fieldErrors: error.fieldErrors };
    throw error;
  }
  redirect(safeNext(text(formData, "next")));
}

export async function register(_: FormState, formData: FormData): Promise<FormState> {
  const body = {
    username: text(formData, "username"),
    password: String(formData.get("password") ?? ""),
    firstName: text(formData, "firstName"),
    lastName: text(formData, "lastName"),
  };
  try {
    await startSession(await api<TokenResponse>("/api/User/register", { method: "POST", body }));
  } catch (error) {
    if (error instanceof ApiError) return { message: error.message, fieldErrors: error.fieldErrors };
    throw error;
  }
  redirect(safeNext(text(formData, "next")));
}

export async function logout(): Promise<void> {
  await endSession();
  redirect("/");
}
