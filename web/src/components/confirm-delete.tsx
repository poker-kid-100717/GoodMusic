"use client";

import { useActionState, useState } from "react";

import type { FormState } from "@/lib/types";

import { buttonClass, FormMessage, initialState, SubmitButton } from "./form";

/**
 * Two-step delete: the first click asks, the second deletes. Errors the API
 * reports (such as "the artist still has songs") show next to the button.
 */
export function ConfirmDelete({
  action,
  label = "Delete",
  confirmLabel = "Yes, delete",
  question = "Delete this for good?",
  size = "sm",
}: {
  action: (state: FormState) => Promise<FormState>;
  label?: string;
  confirmLabel?: string;
  question?: string;
  size?: "sm" | "md";
}) {
  const [asking, setAsking] = useState(false);
  const [state, formAction] = useActionState(async (previous: FormState) => {
    const result = await action(previous);
    // Collapse back to the button so the API's reason shows beside it.
    if (!result.ok) setAsking(false);
    return result;
  }, initialState);

  if (!asking) {
    return (
      <div className="grid gap-2">
        <button type="button" className={buttonClass("ghost", size)} onClick={() => setAsking(true)}>
          {label}
        </button>
        <FormMessage state={state} />
      </div>
    );
  }

  return (
    <form action={formAction} className="flex flex-wrap items-center gap-2">
      <span className="text-sm text-muted">{question}</span>
      <SubmitButton variant="danger" size={size} pendingLabel="Deleting…">
        {confirmLabel}
      </SubmitButton>
      <button type="button" className={buttonClass("secondary", size)} onClick={() => setAsking(false)}>
        Cancel
      </button>
    </form>
  );
}
