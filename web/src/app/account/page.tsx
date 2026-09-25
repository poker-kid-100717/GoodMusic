import type { Metadata } from "next";
import { redirect } from "next/navigation";

import { getCurrentUser } from "@/lib/session";

import { AccountForm, DeleteAccount } from "./account-forms";

export const metadata: Metadata = { title: "Account" };

export default async function AccountPage() {
  const user = await getCurrentUser();
  if (!user) redirect("/login?next=/account");

  return (
    <div className="mx-auto grid w-full max-w-2xl gap-8">
      <div className="grid gap-1">
        <h1 className="text-4xl font-semibold">Account</h1>
        <p className="text-muted">
          Signed in as <span className="font-mono text-ink">{user.username}</span>
        </p>
      </div>
      <section className="grid gap-4 rounded-xl border border-line bg-surface p-5 sm:p-6">
        <h2 className="text-xl font-semibold">Profile</h2>
        <AccountForm user={user} />
      </section>
      <section className="grid gap-3 rounded-xl border border-danger/40 bg-surface p-5 sm:p-6">
        <h2 className="text-xl font-semibold">Delete account</h2>
        <p className="text-muted">
          Deletes your account and signs you out everywhere. Artists and songs you added stay in the catalog.
        </p>
        <div>
          <DeleteAccount />
        </div>
      </section>
    </div>
  );
}
