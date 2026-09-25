import Link from "next/link";

import { buttonClass } from "@/components/button-class";
import { Vinyl } from "@/components/vinyl";

export default function NotFound() {
  return (
    <div className="grid justify-items-center gap-5 py-16 text-center">
      <Vinyl seed="not-found" size={120} />
      <h1 className="text-3xl font-semibold">Nothing on this side of the record</h1>
      <p className="max-w-[45ch] text-muted">The page or artist you were looking for doesn&apos;t exist, or it was deleted.</p>
      <Link href="/" className={buttonClass("primary")}>
        Back to the catalog
      </Link>
    </div>
  );
}
