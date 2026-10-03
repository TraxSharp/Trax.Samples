import { useEffect, useMemo, useRef } from "react";
import { lineOf } from "../sources";
import type { Step } from "../types";

interface Props {
  source: { file: string; code: string };
  current: Step | null;
}

export function CodePanel({ source, current }: Props) {
  const lines = useMemo(() => source.code.replace(/\r/g, "").split("\n"), [source.code]);
  const highlighted = current ? lineOf(source.code.replace(/\r/g, ""), current) : -1;
  const marked = useRef<HTMLDivElement>(null);
  const pane = useRef<HTMLPreElement>(null);
  // Keep the running line in view without scrolling the page.
  useEffect(() => {
    if (marked.current && pane.current)
      pane.current.scrollTop = marked.current.offsetTop - pane.current.clientHeight / 2;
  }, [highlighted]);
  const tone = current?.state === "FAILED" ? "failed" : current?.state === "IN_PROGRESS" ? "running" : "ran";

  return (
    <section className="panel code-panel">
      <h2>
        Code <span className="file">{source.file}</span>
      </h2>
      <pre ref={pane}>
        {lines.map((line, i) => (
          <div
            key={i}
            ref={i === highlighted ? marked : undefined}
            className={i === highlighted ? `line highlight ${tone}` : "line"}
          >
            <span className="gutter">{i + 1}</span>
            {line || " "}
          </div>
        ))}
      </pre>
    </section>
  );
}
