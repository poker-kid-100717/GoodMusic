"use client";

import { type InputHTMLAttributes, useId } from "react";
import { useFormStatus } from "react-dom";

import type { FormState } from "@/lib/types";

import { buttonClass, type ButtonVariant } from "./button-class";

export { buttonClass };

export const initialState: FormState = {};

export function Field({
  label,
  name,
  errors,
  hint,
  ...input
}: { label: string; name: string; errors?: string[]; hint?: string } & InputHTMLAttributes<HTMLInputElement>) {
  // useId keeps ids unique when several forms on a page share a field name.
  const autoId = useId();
  const id = input.id ?? autoId;
  const describedBy = errors?.length ? `${id}-error` : hint ? `${id}-hint` : undefined;
  return (
    <div className="grid gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">
        {label}
      </label>
      <input
        id={id}
        name={name}
        aria-invalid={errors?.length ? true : undefined}
        aria-describedby={describedBy}
        className="h-10 rounded-lg border border-line bg-surface px-3 text-[15px] shadow-xs outline-none transition focus:border-accent aria-invalid:border-danger"
        {...input}
      />
      {errors?.length ? (
        <p id={`${id}-error`} className="text-sm text-danger">
          {errors[0]}
        </p>
      ) : hint ? (
        <p id={`${id}-hint`} className="text-sm text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  );
}

export function SubmitButton({
  children,
  pendingLabel,
  variant = "primary",
  size = "md",
}: {
  children: React.ReactNode;
  pendingLabel?: string;
  variant?: ButtonVariant;
  size?: "sm" | "md";
}) {
  const { pending } = useFormStatus();
  return (
    <button type="submit" disabled={pending} className={buttonClass(variant, size)}>
      {pending ? (pendingLabel ?? "Saving…") : children}
    </button>
  );
}

export function FormMessage({ state }: { state: FormState }) {
  if (!state.message) return null;
  const tone = state.ok ? "bg-success-soft text-success" : "bg-danger-soft text-danger";
  return (
    <p role={state.ok ? "status" : "alert"} className={`rounded-lg px-3 py-2 text-sm ${tone}`}>
      {state.message}
    </p>
  );
}
