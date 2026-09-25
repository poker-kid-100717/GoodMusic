"use client";

import { useActionState, useState } from "react";

import { deleteComposer, saveComposer } from "@/app/actions/catalog";
import { ConfirmDelete } from "@/components/confirm-delete";
import { buttonClass, Field, FormMessage, initialState, SubmitButton } from "@/components/form";
import type { Composer, FormState } from "@/lib/types";

function initials(composer: Composer) {
  return `${composer.firstName.charAt(0)}${composer.lastName.charAt(0)}`.toUpperCase();
}

export function NewComposerForm() {
  const [formKey, setFormKey] = useState(0);
  const [state, action] = useActionState(async (previous: FormState, formData: FormData) => {
    const result = await saveComposer(null, previous, formData);
    if (result.ok) setFormKey((key) => key + 1);
    return result;
  }, initialState);

  return (
    <form key={formKey} action={action} className="grid gap-3 rounded-xl border border-line bg-surface p-4 sm:p-5">
      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-44 flex-1">
          <Field label="First name" name="firstName" required maxLength={50} errors={state.fieldErrors?.firstName} />
        </div>
        <div className="min-w-44 flex-1">
          <Field label="Last name" name="lastName" required maxLength={50} errors={state.fieldErrors?.lastName} />
        </div>
        <SubmitButton pendingLabel="Adding…">Add composer</SubmitButton>
      </div>
      {!state.ok && <FormMessage state={state} />}
    </form>
  );
}

function ComposerRow({ composer, editable }: { composer: Composer; editable: boolean }) {
  const [editing, setEditing] = useState(false);
  const [state, action] = useActionState(async (previous: FormState, formData: FormData) => {
    const result = await saveComposer(composer.id, previous, formData);
    if (result.ok) setEditing(false);
    return result;
  }, initialState);

  if (editing) {
    return (
      <li className="bg-surface-2/60 px-4 py-4">
        <form action={action} className="grid gap-3">
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label="First name" name="firstName" id={`first-${composer.id}`} defaultValue={composer.firstName} required maxLength={50} errors={state.fieldErrors?.firstName} />
            <Field label="Last name" name="lastName" id={`last-${composer.id}`} defaultValue={composer.lastName} required maxLength={50} errors={state.fieldErrors?.lastName} />
          </div>
          <FormMessage state={state} />
          <div className="flex gap-2">
            <SubmitButton size="sm">Save</SubmitButton>
            <button type="button" className={buttonClass("secondary", "sm")} onClick={() => setEditing(false)}>
              Cancel
            </button>
          </div>
        </form>
      </li>
    );
  }

  return (
    <li className="flex flex-wrap items-center gap-4 px-4 py-3">
      <span aria-hidden="true" className="grid size-9 place-items-center rounded-full bg-accent-soft text-sm font-semibold text-accent">
        {initials(composer)}
      </span>
      <p className="flex-1 font-medium">
        {composer.firstName} <span className="font-semibold">{composer.lastName}</span>
      </p>
      {editable && (
        <div className="flex items-start gap-1">
          <button type="button" className={buttonClass("ghost", "sm")} onClick={() => setEditing(true)} aria-label={`Edit ${composer.firstName} ${composer.lastName}`}>
            Edit
          </button>
          <ConfirmDelete action={deleteComposer.bind(null, composer.id)} question={`Delete ${composer.firstName} ${composer.lastName}?`} />
        </div>
      )}
    </li>
  );
}

export function ComposerList({ composers, editable, empty }: { composers: Composer[]; editable: boolean; empty: string }) {
  if (composers.length === 0) {
    return <p className="rounded-xl border border-dashed border-line p-10 text-center text-muted">{empty}</p>;
  }
  return (
    <ul className="divide-y divide-line overflow-hidden rounded-xl border border-line bg-surface">
      {composers.map((composer) => (
        <ComposerRow key={composer.id} composer={composer} editable={editable} />
      ))}
    </ul>
  );
}
