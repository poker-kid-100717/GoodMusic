import type { Metadata } from "next";

import { SearchBox } from "@/components/search-box";
import { getComposers, matches } from "@/lib/catalog";
import { getCurrentUser } from "@/lib/session";

import { ComposerList, NewComposerForm } from "./composer-forms";

export const metadata: Metadata = { title: "Composers" };

export default async function ComposersPage({ searchParams }: PageProps<"/composers">) {
  const [composers, user, params] = await Promise.all([getComposers(), getCurrentUser(), searchParams]);
  const query = typeof params.q === "string" ? params.q : "";
  const shown = composers
    .filter((composer) => matches(query, composer.firstName, composer.lastName, `${composer.firstName} ${composer.lastName}`))
    .sort((a, b) => a.lastName.localeCompare(b.lastName) || a.firstName.localeCompare(b.firstName));

  return (
    <div className="grid gap-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="grid gap-1">
          <h1 className="text-4xl font-semibold">Composers</h1>
          <p className="text-muted tabular-nums">
            {composers.length} {composers.length === 1 ? "composer" : "composers"}, sorted by last name
          </p>
        </div>
        <SearchBox placeholder="Search composers" label="Search composers" />
      </div>
      {user && <NewComposerForm />}
      <ComposerList composers={shown} editable={!!user} empty={query ? `No composers match “${query}”.` : "No composers yet."} />
    </div>
  );
}
