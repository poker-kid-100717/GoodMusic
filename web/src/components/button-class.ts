// Shared by server and client components, so it must not live in a "use client" module.

const variants = {
  primary: "bg-accent text-accent-ink hover:brightness-105",
  secondary: "border border-line bg-surface hover:bg-surface-2",
  danger: "bg-danger text-white hover:brightness-110",
  ghost: "text-muted hover:bg-surface-2 hover:text-ink",
};

export type ButtonVariant = keyof typeof variants;

export function buttonClass(variant: ButtonVariant = "primary", size: "sm" | "md" = "md") {
  const sizing = size === "sm" ? "h-8 px-3 text-sm" : "h-10 px-4 text-[15px]";
  return `inline-flex items-center justify-center gap-2 rounded-lg font-medium transition disabled:opacity-60 ${sizing} ${variants[variant]}`;
}
