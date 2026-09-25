export default function Loading() {
  return (
    <div role="status" aria-label="Loading" className="grid gap-4">
      <div className="h-10 w-56 animate-pulse rounded-lg bg-surface-2" />
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
        {Array.from({ length: 8 }, (_, i) => (
          <div key={i} className="h-24 animate-pulse rounded-xl bg-surface-2" />
        ))}
      </div>
    </div>
  );
}
