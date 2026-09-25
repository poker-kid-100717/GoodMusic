import type { Metadata } from "next";
import { redirect } from "next/navigation";

import { getCurrentUser } from "@/lib/session";

import { RegisterForm } from "../auth-forms";

export const metadata: Metadata = { title: "Create account" };

export default async function RegisterPage({ searchParams }: PageProps<"/register">) {
  const params = await searchParams;
  const next = typeof params.next === "string" ? params.next : "/";
  if (await getCurrentUser()) redirect("/");

  return (
    <div className="mx-auto grid w-full max-w-lg gap-6 rounded-2xl border border-line bg-surface p-6 sm:p-8">
      <div className="grid gap-1">
        <h1 className="text-3xl font-semibold">Create an account</h1>
        <p className="text-muted">Anyone can browse. An account lets you add to the catalog.</p>
      </div>
      <RegisterForm next={next} />
    </div>
  );
}
