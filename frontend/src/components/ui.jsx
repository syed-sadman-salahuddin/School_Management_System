import { Loader2 } from "lucide-react";

export const cn = (...a) => a.filter(Boolean).join(" ");

const buttonVariants = {
  default: "bg-primary text-primary-foreground hover:opacity-90",
  outline: "border border-border bg-card text-foreground hover:bg-muted",
  ghost: "text-foreground hover:bg-muted",
  danger: "bg-danger/10 text-danger hover:bg-danger/20",
};

const buttonSizes = {
  default: "h-10 px-4 text-sm",
  sm: "h-9 px-3 text-sm",
  icon: "h-10 w-10",
};

export function Button({ variant = "default", size = "default", loading, className, children, disabled, ...props }) {
  return (
    <button
      disabled={disabled || loading}
      className={cn(
        "inline-flex items-center justify-center gap-2 rounded-lg font-medium transition",
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary",
        "disabled:cursor-not-allowed disabled:opacity-50",
        buttonVariants[variant],
        buttonSizes[size],
        className
      )}
      {...props}
    >
      {loading && <Loader2 className="h-4 w-4 animate-spin" />}
      {children}
    </button>
  );
}

export function Input({ className, ...props }) {
  return (
    <input
      className={cn(
        "h-10 w-full rounded-lg border border-border bg-card px-3 text-sm text-foreground",
        "placeholder:text-muted-foreground",
        "focus:outline-2 focus:outline-offset-1 focus:outline-primary",
        className
      )}
      {...props}
    />
  );
}

export function Card({ className, ...props }) {
  return (
    <section
      className={cn("rounded-xl border border-border bg-card p-5 shadow-sm", className)}
      {...props}
    />
  );
}

export function CardTitle({ icon: Icon, children, right }) {
  return (
    <div className="mb-4 flex items-center justify-between gap-3">
      <h2 className="flex items-center gap-2 text-base font-semibold">
        {Icon && <Icon className="h-5 w-5 text-primary" />}
        {children}
      </h2>
      {right}
    </div>
  );
}

const statusStyles = {
  HELD: "bg-amber-100 text-amber-800 dark:bg-amber-400/15 dark:text-amber-300",
  CONFIRMED: "bg-emerald-100 text-emerald-800 dark:bg-emerald-400/15 dark:text-emerald-300",
  EXPIRED: "bg-slate-200 text-slate-700 dark:bg-slate-400/15 dark:text-slate-300",
  CANCELLED: "bg-red-100 text-red-800 dark:bg-red-400/15 dark:text-red-300",
};

export function StatusBadge({ status }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold tracking-wide",
        statusStyles[status]
      )}
    >
      {status}
    </span>
  );
}

export function Skeleton({ className }) {
  return <div className={cn("animate-pulse rounded-lg bg-muted", className)} />;
}

export function EmptyState({ icon: Icon, title, text }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-border px-4 py-10 text-center">
      {Icon && <Icon className="h-8 w-8 text-muted-foreground" />}
      <p className="font-medium">{title}</p>
      {text && <p className="text-sm text-muted-foreground">{text}</p>}
    </div>
  );
}

export function ErrorBanner({ children }) {
  return (
    <div className="mb-4 rounded-lg border border-danger/30 bg-danger/10 px-3 py-2 text-sm text-danger">
      {children}
    </div>
  );
}

export function LiveDot({ text }) {
  return (
    <span className="inline-flex items-center gap-2 text-xs text-muted-foreground">
      <span className="relative flex h-2 w-2">
        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-success opacity-60" />
        <span className="relative inline-flex h-2 w-2 rounded-full bg-success" />
      </span>
      {text}
    </span>
  );
}