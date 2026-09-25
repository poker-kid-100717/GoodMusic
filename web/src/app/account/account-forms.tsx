"use client";

import { useActionState } from "react";

import { deleteAccount, updateAccount } from "@/app/actions/account";
import { ConfirmDelete } from "@/components/confirm-delete";
import { Field, FormMessage, initialState, SubmitButton } from "@/components/form";
import type { User } from "@/lib/types";

export function AccountForm({ user }: { user: User }) {
  const [state, action] = useActionState(updateAccount, initialState);
  return (
    <form action={action} className="grid gap-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="First name" name="firstName" defaultValue={user.firstName} required maxLength={50} errors={state.fieldErrors?.firstName} />
        <Field label="Last name" name="lastName" defaultValue={user.lastName} required maxLength={50} errors={state.fieldErrors?.lastName} />
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <Field
          label="New password"
          name="password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          hint="Leave blank to keep your current password."
          errors={state.fieldErrors?.password}
        />
        <Field label="Confirm new password" name="confirmPassword" type="password" autoComplete="new-password" errors={state.fieldErrors?.confirmPassword} />
      </div>
      <FormMessage state={state} />
      <div>
        <SubmitButton>Save changes</SubmitButton>
      </div>
    </form>
  );
}

export function DeleteAccount() {
  return (
    <ConfirmDelete action={deleteAccount} label="Delete my account" confirmLabel="Delete account" question="This can't be undone." size="md" />
  );
}
