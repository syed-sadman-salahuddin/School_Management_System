import { useEffect, useRef } from "react";

// Calls fn immediately, then every `ms` milliseconds (automatic refresh).
export function usePolling(fn, ms, deps = []) {
  const ref = useRef(fn);
  ref.current = fn;

  useEffect(() => {
    let alive = true;
    const run = () => {
      if (alive) ref.current();
    };
    run();
    const id = setInterval(run, ms);
    return () => {
      alive = false;
      clearInterval(id);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);
}