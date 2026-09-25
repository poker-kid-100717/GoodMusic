"use client";

import { useActionState } from "react";

import { createArtist } from "@/app/actions/catalog";
import { Field, FormMessage, initialState, SubmitButton } from "@/components/form";

export function NewArtistForm() {
  const [state, action] = useActionState(createArtist, initialState);
  return (
    <form action={action} className="grid gap-3 rounded-xl border border-line bg-surface p-4 sm:p-5">
      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-56 flex-1">
          <Field label="New artist" name="name" required maxLength={50} placeholder="e.g. Nina Simone" errors={state.fieldErrors?.name} />
        </div>
        <SubmitButton pendingLabel="Adding…">Add artist</SubmitButton>
      </div>
      <FormMessage state={state} />
    </form>
  );
}
