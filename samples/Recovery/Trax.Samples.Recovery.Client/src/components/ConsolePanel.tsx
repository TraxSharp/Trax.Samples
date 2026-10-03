import { useEffect, useRef } from "react";
import type { ConsoleLine } from "../types";

export function ConsolePanel({ lines }: { lines: ConsoleLine[] }) {
  const end = useRef<HTMLDivElement>(null);
  const sorted = [...lines].sort((a, b) => a.at.localeCompare(b.at));
  useEffect(() => {
    end.current?.scrollIntoView({ block: "nearest" });
  }, [lines.length]);

  return (
    <section className="panel console-panel">
      <h2>Console</h2>
      <div className="console">
        {sorted.length === 0 && <div className="hint">Pick a scenario and press Run.</div>}
        {sorted.map((l) => (
          <div key={l.key} className={`console-line ${l.tone}`}>
            <span className="time">{new Date(l.at).toLocaleTimeString()}</span> {l.text}
          </div>
        ))}
        <div ref={end} />
      </div>
    </section>
  );
}
