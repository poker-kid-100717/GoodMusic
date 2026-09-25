import type { Metadata } from "next";
import { redirect } from "next/navigation";

import { getCurrentUser } from "@/lib/session";

import { LoginForm } from "../auth-forms";

export const metadata: Metadata = { title: "Sign in" };

export default async function LoginPage({ searchParams }: PageProps<"/login">) {
  const params = await searchParams;
  const next = typeof params.next === "string" ? params.next : "/";
  if (await getCurrentUser()) redirect(next.startsWith("/") && !next.startsWith("//") ? next : "/");

  return (
    <div className="mx-auto grid w-full max-w-md gap-6 rounded-2xl border border-line bg-surface p-6 sm:p-8">
      <div className="grid gap-1">
        <h1 className="text-3xl font-semibold">Sign in</h1>
        <p className="text-muted">Sign in to add and edit artists, songs and composers.</p>
      </div>
      <LoginForm next={next} expired={params.expired === "1"} />
    </div>
  );
}
