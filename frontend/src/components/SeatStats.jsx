export default function SeatStats({ c }) {
  const pct = (n) => (c.totalSeats > 0 ? Math.min(100, (n / c.totalSeats) * 100) : 0);

  const tiles = [
    { label: "Total", value: c.totalSeats, cls: "text-foreground" },
    { label: "Held", value: c.heldSeats, cls: "text-amber-600 dark:text-amber-300" },
    { label: "Confirmed", value: c.confirmedSeats, cls: "text-emerald-600 dark:text-emerald-300" },
    { label: "Available", value: c.availableSeats, cls: "text-primary" },
  ];

  return (
    <div>
      <div className="grid grid-cols-4 gap-2">
        {tiles.map((t) => (
          <div key={t.label} className="rounded-lg bg-muted px-2 py-2 text-center">
            <div className={`text-lg font-bold leading-tight ${t.cls}`}>{t.value}</div>
            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{t.label}</div>
          </div>
        ))}
      </div>

      <div
        className="mt-3 flex h-2 overflow-hidden rounded-full bg-muted"
        role="img"
        aria-label={`${c.confirmedSeats} confirmed, ${c.heldSeats} held, ${c.availableSeats} available`}
      >
        <div className="bg-emerald-500 transition-all" style={{ width: pct(c.confirmedSeats) + "%" }} />
        <div className="bg-amber-400 transition-all" style={{ width: pct(c.heldSeats) + "%" }} />
      </div>
      <div className="mt-1.5 flex gap-4 text-[11px] text-muted-foreground">
        <span className="flex items-center gap-1"><i className="h-2 w-2 rounded-full bg-emerald-500" /> Confirmed</span>
        <span className="flex items-center gap-1"><i className="h-2 w-2 rounded-full bg-amber-400" /> Held</span>
      </div>
    </div>
  );
}