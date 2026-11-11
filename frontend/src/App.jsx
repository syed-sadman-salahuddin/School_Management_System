import { useState, useCallback, useEffect } from "react";
import { GraduationCap, Users, ShieldCheck, Sun, Moon, CheckCircle2, AlertCircle, X } from "lucide-react";
import AdminPage from "./pages/AdminPage";
import ParentPage from "./pages/ParentPage";
import { cn } from "./components/ui";

function useTheme() {
  const [dark, setDark] = useState(() => {
    const saved = localStorage.getItem("theme");
    if (saved) return saved === "dark";
    return window.matchMedia?.("(prefers-color-scheme: dark)").matches ?? false;
  });

  useEffect(() => {
    document.documentElement.classList.toggle("dark", dark);
    localStorage.setItem("theme", dark ? "dark" : "light");
  }, [dark]);

  return [dark, setDark];
}

export default function App() {
  const [tab, setTab] = useState(() => localStorage.getItem("tab") || "parent");
  const [notice, setNotice] = useState(null);
  const [dark, setDark] = useTheme();

  const notify = useCallback((type, text) => setNotice({ type, text, id: Date.now() }), []);

  useEffect(() => {
    localStorage.setItem("tab", tab);
  }, [tab]);

  useEffect(() => {
    if (!notice) return;
    const t = setTimeout(() => setNotice(null), 7000);
    return () => clearTimeout(t);
  }, [notice]);

  const tabs = [
    { id: "parent", label: "Parent / Guardian", icon: Users },
    { id: "admin", label: "School Admin", icon: ShieldCheck },
  ];

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-border bg-card/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-primary-foreground">
              <GraduationCap className="h-6 w-6" />
            </div>
            <div className="leading-tight">
              <h1 className="text-lg font-bold">School Admission</h1>
              <p className="text-xs text-muted-foreground">First come, first served</p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <nav className="flex rounded-lg bg-muted p-1">
              {tabs.map((t) => (
                <button
                  key={t.id}
                  onClick={() => setTab(t.id)}
                  className={cn(
                    "flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition",
                    tab === t.id
                      ? "bg-card text-foreground shadow-sm"
                      : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <t.icon className="h-4 w-4" />
                  <span className="hidden sm:inline">{t.label}</span>
                  <span className="sm:hidden">{t.id === "parent" ? "Parent" : "Admin"}</span>
                </button>
              ))}
            </nav>
            <button
              onClick={() => setDark(!dark)}
              className="flex h-10 w-10 items-center justify-center rounded-lg border border-border bg-card hover:bg-muted"
              aria-label="Toggle theme"
            >
              {dark ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
            </button>
          </div>
        </div>
      </header>

      {notice && (
        <div className="fixed inset-x-4 top-20 z-30 mx-auto max-w-md sm:left-auto sm:right-4 sm:mx-0" role="alert">
          <div
            className={cn(
              "flex items-start gap-3 rounded-xl border bg-card p-3 shadow-lg",
              notice.type === "error" ? "border-danger/40" : "border-success/40"
            )}
          >
            {notice.type === "error" ? (
              <AlertCircle className="mt-0.5 h-5 w-5 shrink-0 text-danger" />
            ) : (
              <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0 text-success" />
            )}
            <p className="flex-1 text-sm">{notice.text}</p>
            <button onClick={() => setNotice(null)} aria-label="Dismiss" className="text-muted-foreground hover:text-foreground">
              <X className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}

      <main className="mx-auto max-w-6xl px-4 py-6">
        {tab === "admin" ? <AdminPage notify={notify} /> : <ParentPage notify={notify} />}
      </main>
    </div>
  );
}