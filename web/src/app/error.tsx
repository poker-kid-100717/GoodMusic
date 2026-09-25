"use client";

import { buttonClass } from "@/components/form";

export default function Error({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div role="alert" className="grid justify-items-center gap-4 py-16 text-center">
      <h1 className="text-3xl font-semibold">The catalog didn&apos;t load</h1>
      <p className="max-w-[50ch] text-muted">
        The API may be waking up; it sleeps after a while without visitors and takes a few seconds to start.
      </p>
      <button type="button" onClick={reset} className={buttonClass("primary")}>
        Try again
      </button>
    </div>
  );
}
