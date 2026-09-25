import type { Metadata } from "next";
import { GeistMono } from "geist/font/mono";
import { GeistSans } from "geist/font/sans";
import Link from "next/link";

import { logout } from "@/app/actions/auth";
import { buttonClass } from "@/components/button-class";
import { NavLink } from "@/components/nav-link";
import { Vinyl } from "@/components/vinyl";
import { getCurrentUser } from "@/lib/session";

import "./globals.css";

export const metadata: Metadata = {
  title: { default: "GoodMusic", template: "%s · GoodMusic" },
  description: "A music catalog of artists, songs and composers, built on Next.js, ASP.NET Core 10 and MongoDB.",
};

async function Header() {
  const user = await getCurrentUser();
  return (
    <header className="sticky top-0 z-10 border-b border-line bg-bg/85 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-6xl items-center gap-2 px-4 sm:gap-6">
        <Link href="/" className="spin-on-hover flex items-center gap-2.5 font-display text-lg font-semibold">
          <Vinyl seed="GoodMusic" size={30} />
          GoodMusic
        </Link>
        <nav aria-label="Catalog" className="hidden items-center gap-1 sm:flex">
          <NavLink href="/artists">Artists</NavLink>
          <NavLink href="/songs">Songs</NavLink>
          <NavLink href="/composers">Composers</NavLink>
        </nav>
        <div className="ml-auto flex items-center gap-2">
          {user ? (
            <>
              <Link href="/account" className="hidden text-[15px] text-muted hover:text-ink sm:block">
                {user.firstName || user.username}
              </Link>
              <form action={logout}>
                <button className={buttonClass("secondary", "sm")}>Sign out</button>
              </form>
            </>
          ) : (
            <>
              <Link href="/login" className={buttonClass("ghost", "sm")}>
                Sign in
              </Link>
              <Link href="/register" className={buttonClass("primary", "sm")}>
                Create account
              </Link>
            </>
          )}
        </div>
      </div>
      <nav aria-label="Catalog" className="flex gap-1 border-t border-line px-2 py-1.5 sm:hidden">
        <NavLink href="/artists">Artists</NavLink>
        <NavLink href="/songs">Songs</NavLink>
        <NavLink href="/composers">Composers</NavLink>
      </nav>
    </header>
  );
}

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={`${GeistSans.variable} ${GeistMono.variable} h-full antialiased`}>
      <body className="flex min-h-full flex-col font-sans">
        <Header />
        <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:py-12">{children}</main>
        <footer className="border-t border-line">
          <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-6 text-sm text-muted">
            <p>Next.js 16 · ASP.NET Core 10 · MongoDB · Cloudflare Workers and Containers</p>
            <div className="flex gap-4">
              <a href="/swagger" className="hover:text-ink">
                API docs
              </a>
              <a href="https://github.com/poker-kid-100717/GoodMusic" className="hover:text-ink">
                Source
              </a>
            </div>
          </div>
        </footer>
      </body>
    </html>
  );
}
