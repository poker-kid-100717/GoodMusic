"use client";

import Link from "next/link";
import { useActionState } from "react";

import { login, register } from "@/app/actions/auth";
import { Field, FormMessage, initialState, SubmitButton } from "@/components/form";

export function LoginForm({ next, expired }: { next: string; expired: boolean }) {
  const [state, action] = useActionState(login, initialState);
  return (
    <form action={action} className="grid gap-4">
      <input type="hidden" name="next" value={next} />
      {expired && !state.message && (
        <p role="status" className="rounded-lg bg-accent-soft px-3 py-2 text-sm">
          Your session ended. Sign in again to continue.
        </p>
      )}
      <Field label="Username" name="username" autoComplete="username" required errors={state.fieldErrors?.username} />
      <Field label="Password" name="password" type="password" autoComplete="current-password" required errors={state.fieldErrors?.password} />
      <FormMessage state={state} />
      <SubmitButton pendingLabel="Signing in…">Sign in</SubmitButton>
      <p className="text-sm text-muted">
        New here?{" "}
        <Link href={`/register?next=${encodeURIComponent(next)}`} className="font-medium text-accent hover:underline">
          Create an account
        </Link>
      </p>
    </form>
  );
}

export function RegisterForm({ next }: { next: string }) {
  const [state, action] = useActionState(register, initialState);
  return (
    <form action={action} className="grid gap-4">
      <input type="hidden" name="next" value={next} />
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="First name" name="firstName" autoComplete="given-name" required maxLength={50} errors={state.fieldErrors?.firstName} />
        <Field label="Last name" name="lastName" autoComplete="family-name" required maxLength={50} errors={state.fieldErrors?.lastName} />
      </div>
      <Field
        label="Username"
        name="username"
        autoComplete="username"
        required
        maxLength={50}
        pattern="[A-Za-z0-9._\-]+"
        hint="Letters, digits, dots, dashes and underscores."
        errors={state.fieldErrors?.username}
      />
      <Field
        label="Password"
        name="password"
        type="password"
        autoComplete="new-password"
        required
        minLength={8}
        hint="At least 8 characters."
        errors={state.fieldErrors?.password}
      />
      <FormMessage state={state} />
      <SubmitButton pendingLabel="Creating account…">Create account</SubmitButton>
      <p className="text-sm text-muted">
        Already have an account?{" "}
        <Link href={`/login?next=${encodeURIComponent(next)}`} className="font-medium text-accent hover:underline">
          Sign in
        </Link>
      </p>
    </form>
  );
}
