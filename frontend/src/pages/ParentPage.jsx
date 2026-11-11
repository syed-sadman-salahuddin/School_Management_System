import { useState, useEffect, useCallback, useRef } from "react";
import { UserRound, BookOpen, ClipboardList, Clock, CreditCard, XCircle, CheckCircle2 } from "lucide-react";
import { api, errorText } from "../api";
import { usePolling } from "../usePolling";
import {
  Button, Input, Card, CardTitle, StatusBadge, Skeleton, EmptyState, ErrorBanner, LiveDot, cn,
} from "../components/ui";
import SeatStats from "../components/SeatStats";

const ID_PATTERN = /^[A-Za-z0-9-]{5,32}$/;

const toMs = (iso) => new Date(/Z$|[+-]\d\d:\d\d$/.test(iso) ? iso : iso + "Z").getTime();

// A HELD reservation past its expiry time is shown as EXPIRED right away.
const effStatus = (h, now) =>
  h.status === "HELD" && toMs(h.expiresAt) <= now ? "EXPIRED" : h.status;

function formatRemaining(ms) {
  const total = Math.max(0, Math.floor(ms / 1000));
  const m = String(Math.floor(total / 60)).padStart(2, "0");
  const s = String(total % 60).padStart(2, "0");
  return `${m}:${s}`;
}

export default function ParentPage({ notify }) {
  const [studentInput, setStudentInput] = useState(() => localStorage.getItem("studentId") || "");
  const [classes, setClasses] = useState(null);
  const [classErr, setClassErr] = useState(null);
  const [holds, setHolds] = useState(null);
  const [holdErr, setHoldErr] = useState(null);
  const [now, setNow] = useState(Date.now());
  const [busy, setBusy] = useState({});
  const refreshing = useRef(false);

  const trimmed = studentInput.trim();
  const validId = ID_PATTERN.test(trimmed) ? trimmed : null;

  // Remember the student ID across refresh / browser restart
  useEffect(() => {
    localStorage.setItem("studentId", studentInput);
  }, [studentInput]);

  // 1-second clock for countdowns
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  const loadClasses = useCallback(async () => {
    try {
      setClasses(await api.getClasses());
      setClassErr(null);
    } catch (e) {
      setClassErr(errorText(e));
    }
  }, []);

  const loadHolds = useCallback(async () => {
    if (!validId) {
      setHolds([]);
      setHoldErr(null);
      return;
    }
    try {
      setHolds(await api.myHolds(validId));
      setHoldErr(null);
    } catch (e) {
      setHoldErr(errorText(e));
    }
  }, [validId]);

  usePolling(loadClasses, 3000, [loadClasses]);
  usePolling(loadHolds, 3000, [loadHolds]);

  // When a countdown reaches zero, refresh immediately (no page reload needed)
  useEffect(() => {
    if (refreshing.current || !holds) return;
    if (holds.some((h) => h.status === "HELD" && toMs(h.expiresAt) <= now)) {
      refreshing.current = true;
      Promise.all([loadHolds(), loadClasses()]).finally(() => {
        refreshing.current = false;
      });
    }
  }, [now, holds, loadHolds, loadClasses]);

  async function run(key, fn, successMsg) {
    if (busy[key]) return;
    setBusy((b) => ({ ...b, [key]: true }));
    try {
      await fn();
      notify("success", successMsg);
    } catch (e) {
      notify("error", errorText(e));
    } finally {
      setBusy((b) => ({ ...b, [key]: false }));
      await Promise.all([loadHolds(), loadClasses()]);
    }
  }

  const myHolds = holds ?? [];
  const activeClassIds = new Set(
    myHolds
      .filter((h) => {
        const s = effStatus(h, now);
        return s === "HELD" || s === "CONFIRMED";
      })
      .map((h) => h.classId)
  );

  return (
    <div className="space-y-6">
      {/* Student identification */}
      <Card>
        <CardTitle icon={UserRound}>Student Identification</CardTitle>
        <Input
          placeholder="Enter student birth registration number"
          value={studentInput}
          onChange={(e) => setStudentInput(e.target.value)}
          aria-label="Student birth registration number"
          className={trimmed && !validId ? "border-danger" : ""}
        />
        {!trimmed && (
          <p className="mt-2 text-sm text-muted-foreground">
            Enter the birth registration number to apply for admission.
          </p>
        )}
        {trimmed && !validId && (
          <p className="mt-2 text-sm text-danger">
            Invalid ID: use 5-32 characters (letters, digits or hyphens only).
          </p>
        )}
        {validId && (
          <p className="mt-2 flex items-center gap-1.5 text-sm text-success">
            <CheckCircle2 className="h-4 w-4" /> Using student ID {validId}
          </p>
        )}
      </Card>

      <div className="grid gap-6 lg:grid-cols-5">
        {/* Available classes */}
        <div className="lg:col-span-3">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="flex items-center gap-2 text-base font-semibold">
              <BookOpen className="h-5 w-5 text-primary" /> Available Classes
            </h2>
            <LiveDot text="Live · every 3s" />
          </div>

          {classErr && <ErrorBanner>Could not refresh classes: {classErr}</ErrorBanner>}
          {classes === null && !classErr && (
            <div className="space-y-4">
              <Skeleton className="h-40" />
              <Skeleton className="h-40" />
            </div>
          )}
          {classes && classes.length === 0 && (
            <EmptyState icon={BookOpen} title="No classes are open yet" text="Please check back soon." />
          )}

          <div className="space-y-4">
            {classes?.map((c) => {
              const alreadyApplied = activeClassIds.has(c.id);
              const full = c.availableSeats <= 0;
              const key = "apply-" + c.id;
              let label = "Apply";
              if (busy[key]) label = "Applying...";
              else if (alreadyApplied) label = "Applied";
              else if (full) label = "Full";

              return (
                <Card key={c.id}>
                  <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
                    <h3 className="font-semibold">{c.name}</h3>
                    <Button
                      size="sm"
                      loading={busy[key]}
                      disabled={!validId || alreadyApplied || full}
                      title={!validId ? "Enter a valid student ID first" : ""}
                      onClick={() =>
                        run(
                          key,
                          () => api.apply(c.id, validId),
                          `Seat held in ${c.name}. Pay the admission fee before it expires.`
                        )
                      }
                    >
                      {label}
                    </Button>
                  </div>
                  <SeatStats c={c} />
                </Card>
              );
            })}
          </div>
        </div>

        {/* My applications */}
        <div className="lg:col-span-2">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="flex items-center gap-2 text-base font-semibold">
              <ClipboardList className="h-5 w-5 text-primary" /> My Applications
            </h2>
          </div>

          {holdErr && <ErrorBanner>Could not refresh applications: {holdErr}</ErrorBanner>}
          {!validId && (
            <EmptyState
              icon={ClipboardList}
              title="No student selected"
              text="Enter a valid student ID to see your applications."
            />
          )}
          {validId && holds === null && !holdErr && <Skeleton className="h-32" />}
          {validId && holds && holds.length === 0 && !holdErr && (
            <EmptyState icon={ClipboardList} title="No applications yet" text="Apply to a class to get started." />
          )}

          <div className="space-y-3">
            {validId &&
              myHolds.map((h) => {
                const status = effStatus(h, now);
                const active = status === "HELD";
                const remaining = toMs(h.expiresAt) - now;
                const payKey = "pay-" + h.holdId;
                const cancelKey = "cancel-" + h.holdId;

                return (
                  <Card key={h.holdId} className={cn("p-4", !active && "opacity-90")}>
                    <div className="flex items-start justify-between gap-2">
                      <h3 className="font-medium leading-snug">{h.className}</h3>
                      <StatusBadge status={status} />
                    </div>

                    <div className="mt-2 min-h-5 text-sm">
                      {active && (
                        <span
                          className={cn(
                            "inline-flex items-center gap-1.5 font-mono",
                            remaining < 60000 ? "text-danger" : "text-muted-foreground"
                          )}
                        >
                          <Clock className="h-4 w-4" /> {formatRemaining(remaining)} left to pay
                        </span>
                      )}
                      {status === "EXPIRED" && (
                        <span className="text-muted-foreground">This reservation is no longer active.</span>
                      )}
                      {status === "CONFIRMED" && (
                        <span className="text-success">Admission confirmed. Fee received.</span>
                      )}
                      {status === "CANCELLED" && (
                        <span className="text-muted-foreground">This application was cancelled.</span>
                      )}
                    </div>

                    <div className="mt-3 flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        loading={busy[payKey]}
                        disabled={!active}
                        onClick={() =>
                          run(payKey, () => api.confirm(h.holdId, validId), "Payment received. Admission confirmed!")
                        }
                      >
                        <CreditCard className="h-4 w-4" /> Pay Admission Fee
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        loading={busy[cancelKey]}
                        disabled={!active}
                        onClick={() =>
                          run(cancelKey, () => api.cancel(h.holdId, validId), "Application cancelled.")
                        }
                      >
                        <XCircle className="h-4 w-4" /> Cancel
                      </Button>
                    </div>
                  </Card>
                );
              })}
          </div>
        </div>
      </div>
    </div>
  );
}