import { useState, useCallback } from "react";
import { PlusCircle, LayoutGrid, School } from "lucide-react";
import { api, errorText } from "../api";
import { usePolling } from "../usePolling";
import { Button, Input, Card, CardTitle, Skeleton, EmptyState, ErrorBanner, LiveDot } from "../components/ui";
import SeatStats from "../components/SeatStats";

export default function AdminPage({ notify }) {
  const [name, setName] = useState("");
  const [seats, setSeats] = useState("");
  const [classes, setClasses] = useState(null);
  const [loadError, setLoadError] = useState(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      setClasses(await api.getClasses());
      setLoadError(null);
    } catch (e) {
      setLoadError(errorText(e));
    }
  }, []);

  usePolling(load, 3000, [load]);

  async function submit(e) {
    e.preventDefault();
    const n = name.trim();
    const s = Number(seats);
    if (!n) return notify("error", "Please enter a class name.");
    if (!Number.isInteger(s) || s <= 0)
      return notify("error", "Total seats must be a whole number greater than 0.");

    setSaving(true);
    try {
      await api.createClass(n, s);
      notify("success", `Created "${n}" with ${s} seats.`);
      setName("");
      setSeats("");
      await load();
    } catch (err) {
      notify("error", errorText(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardTitle icon={PlusCircle}>Create Class / Shift</CardTitle>
        <form className="grid gap-3 sm:grid-cols-[1fr_160px_auto]" onSubmit={submit}>
          <Input
            placeholder="Class name, e.g. Class 6 - Morning Shift"
            value={name}
            onChange={(e) => setName(e.target.value)}
            aria-label="Class name"
          />
          <Input
            type="number"
            min="1"
            placeholder="Total seats"
            value={seats}
            onChange={(e) => setSeats(e.target.value)}
            aria-label="Total seats"
          />
          <Button type="submit" loading={saving}>
            {saving ? "Creating..." : "Create class"}
          </Button>
        </form>
      </Card>

      <div>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="flex items-center gap-2 text-base font-semibold">
            <LayoutGrid className="h-5 w-5 text-primary" /> All Classes
          </h2>
          <LiveDot text="Live · updates every 3s" />
        </div>

        {loadError && <ErrorBanner>Could not refresh classes: {loadError}</ErrorBanner>}

        {classes === null && !loadError && (
          <div className="grid gap-4 md:grid-cols-2">
            <Skeleton className="h-40" />
            <Skeleton className="h-40" />
          </div>
        )}

        {classes && classes.length === 0 && (
          <EmptyState icon={School} title="No classes yet" text="Create your first class above." />
        )}

        {classes && classes.length > 0 && (
          <div className="grid gap-4 md:grid-cols-2">
            {classes.map((c) => (
              <Card key={c.id}>
                <h3 className="mb-3 font-semibold">{c.name}</h3>
                <SeatStats c={c} />
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}