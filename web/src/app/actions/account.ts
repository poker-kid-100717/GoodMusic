"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { api } from "@/lib/api";
import { submit, text } from "@/lib/form";
import { endSession, requireToken } from "@/lib/session";
import type { FormState } from "@/lib/types";

export async function updateAccount(_: FormState, formData: FormData): Promise<FormState> {
  const token = await requireToken("/account");
  const password = String(formData.get("password") ?? "");
  if (password && password !== String(formData.get("confirmPassword") ?? "")) {
    return { fieldErrors: { confirmPassword: ["The passwords don't match."] } };
  }

  const state = await submit("/account", () =>
    api("/api/User/me", {
      method: "PUT",
      token,
      body: { firstName: text(formData, "firstName"), lastName: text(formData, "lastName"), password: password || null },
    }),
  );
  if (state.ok) {
    revalidatePath("/", "layout");
    return { ok: true, message: password ? "Profile and password updated." : "Profile updated." };
  }
  return state;
}

export async function deleteAccount(_: FormState): Promise<FormState> {
  const token = await requireToken("/account");
  const state = await submit("/account", () => api("/api/User/me", { method: "DELETE", token }));
  if (!state.ok) return state;
  await endSession();
  redirect("/?deleted=1");
}
