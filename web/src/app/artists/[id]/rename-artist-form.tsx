"use client";

import { useActionState } from "react";

import { renameArtist } from "@/app/actions/catalog";
import { Field, FormMessage, initialState, SubmitButton } from "@/components/form";

export function RenameArtistForm({ id, name }: { id: string; name: string }) {
  const [state, action] = useActionState(renameArtist.bind(null, id), initialState);
  return (
    <form action={action} className="grid gap-3">
      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-56 flex-1">
          <Field
            label="Name"
            name="name"
            defaultValue={name}
            required
            maxLength={50}
            errors={state.fieldErrors?.name}
            hint="Renaming updates the artist name on every song."
          />
        </div>
        <SubmitButton>Rename</SubmitButton>
      </div>
      {state.ok ? <FormMessage state={{ ok: true, message: "Renamed." }} /> : <FormMessage state={state} />}
    </form>
  );
}
