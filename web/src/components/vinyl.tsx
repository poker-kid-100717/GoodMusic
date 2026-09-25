/**
 * A record drawn in CSS: grooves plus a label whose color comes from the
 * artist's name, so every artist gets a stable, recognizable cover without
 * any artwork being stored.
 */
function hue(seed: string): number {
  let hash = 0;
  for (const char of seed) hash = (hash * 31 + char.charCodeAt(0)) | 0;
  return Math.abs(hash) % 360;
}

export function Vinyl({ seed, size = 64, className = "" }: { seed: string; size?: number; className?: string }) {
  const h = hue(seed);
  return (
    <span
      aria-hidden="true"
      className={`vinyl relative inline-block shrink-0 rounded-full shadow-sm ${className}`}
      style={{
        width: size,
        height: size,
        background: `radial-gradient(circle, hsl(${h} 72% 56%) 0 31%, var(--groove) 32%),
          repeating-radial-gradient(circle, rgb(255 255 255 / 0.07) 0 1px, transparent 1px 4px)`,
        backgroundBlendMode: "screen",
      }}
    >
      <span
        className="absolute inset-0 m-auto rounded-full"
        style={{ width: size * 0.07, height: size * 0.07, background: "var(--bg)" }}
      />
    </span>
  );
}
