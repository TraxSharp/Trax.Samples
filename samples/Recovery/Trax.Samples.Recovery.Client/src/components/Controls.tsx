import { useState } from "react";
import type { Phase, Scenario } from "../types";

const PHASES: { key: Phase[]; label: string }[] = [
  { key: ["runs"], label: "Runs" },
  { key: ["breaks", "dead"], label: "Breaks" },
  { key: ["recovers", "done"], label: "Recovers" },
];

interface Props {
  scenario: Scenario;
  onScenario(s: Scenario): void;
  phase: Phase;
  attempts: number;
  onRun(scenario: Scenario, crashOnce: boolean, orderId: string, topic: string): void;
  onAskAfresh(): void;
  onChangeData(): void;
  onReset(): void;
}

export function Controls(props: Props) {
  const { scenario, phase } = props;
  const [crashOnce, setCrashOnce] = useState(true);
  const [orderId, setOrderId] = useState("A-1001");
  const [topic, setTopic] = useState("Durable execution research");
  const running = phase === "starting" || phase === "runs" || phase === "recovers";
  const reached = (p: Phase[]) => PHASES.findIndex((x) => x.key.some((k) => p.includes(k)));
  const step = reached([phase]);

  return (
    <section className="controls">
      <div className="scenario-picker" role="tablist">
        <button className={scenario === "RESEARCH" ? "active" : ""} onClick={() => props.onScenario("RESEARCH")} disabled={running}>
          Research agent
        </button>
        <button className={scenario === "REFUND" ? "active" : ""} onClick={() => props.onScenario("REFUND")} disabled={running}>
          Refund approval
        </button>
      </div>
      {scenario === "RESEARCH" ? (
        <label className="field">
          Topic
          <input value={topic} onChange={(e) => setTopic(e.target.value)} disabled={running} maxLength={200} />
        </label>
      ) : (
        <label className="field">
          Order
          <select value={orderId} onChange={(e) => setOrderId(e.target.value)} disabled={running}>
            <option value="A-1001">A-1001: $89, arrived broken</option>
            <option value="A-1002">A-1002: $420, never arrived</option>
            <option value="A-1003">A-1003: $35.50, changed my mind, 2 earlier refunds</option>
          </select>
        </label>
      )}
      <label className="check">
        <input type="checkbox" checked={crashOnce} onChange={(e) => setCrashOnce(e.target.checked)} disabled={running} />
        Crash once ({scenario === "RESEARCH" ? "second tool call" : "payment times out"})
      </label>
      <button className="primary" onClick={() => props.onRun(scenario, crashOnce, orderId, topic)} disabled={running}>
        Run
      </button>
      <button onClick={props.onChangeData} disabled={phase !== "breaks"} title="Edit what the run reads, while the retry waits out its backoff">
        Change the data during the backoff
      </button>
      <button
        onClick={props.onAskAfresh}
        disabled={!(phase === "breaks" || phase === "done" || phase === "dead")}
        title="During the backoff: triggerManifest(askAfresh: true). After the run: requeueExecution(id, askAfresh: true)."
      >
        Ask afresh
      </button>
      <button onClick={props.onReset} disabled={running || phase === "idle"}>
        Reset
      </button>
      <ol className="stepper">
        {PHASES.map((p, i) => (
          <li key={p.label} className={i < step ? "done" : i === step ? (phase === "dead" ? "failed" : "current") : ""}>
            {p.label}
          </li>
        ))}
      </ol>
    </section>
  );
}
