import { useMemo, useState } from "react";
import { SOURCES } from "../sources";
import type { Scenario, Step } from "../types";
import { useRecoveryRun } from "../useRecoveryRun";
import { CodePanel } from "./CodePanel";
import { ConsolePanel } from "./ConsolePanel";
import { Controls } from "./Controls";
import { TimelinePanel } from "./TimelinePanel";

export function App() {
  const [scenario, setScenario] = useState<Scenario>("RESEARCH");
  const recovery = useRecoveryRun();
  const shown = recovery.run?.scenario ?? scenario;

  // The step the code panel highlights: the running junction, else the latest step of the latest attempt.
  const current: Step | null = useMemo(() => {
    const last = recovery.attempts[recovery.attempts.length - 1];
    if (!last) return null;
    const steps = Object.values(last.steps).sort((a, b) => a.position - b.position);
    return steps.find((s) => s.state === "IN_PROGRESS") ?? steps[steps.length - 1] ?? null;
  }, [recovery.attempts]);

  return (
    <div className="app">
      <header className="header">
        <div>
          <h1>Watch a train recover without asking the model again</h1>
          <p className="subtitle">
            Real Trax trains, real retries, live junction events. A step after the model calls crashes; the
            manifest's retry replays the recorded decisions instead of paying for them twice.
          </p>
        </div>
        <a className="dashboard-link" href="http://localhost:5230/trax" target="_blank" rel="noreferrer">
          Dashboard
        </a>
      </header>
      <Controls
        scenario={scenario}
        onScenario={setScenario}
        phase={recovery.phase}
        attempts={recovery.attempts.length}
        onRun={recovery.start}
        onAskAfresh={recovery.askAfresh}
        onChangeData={recovery.changeData}
        onReset={recovery.reset}
      />
      <main className="panels">
        <CodePanel source={SOURCES[shown]} current={current} />
        <ConsolePanel lines={recovery.lines} />
        <TimelinePanel attempts={recovery.attempts} maxRetries={recovery.run?.maxRetries ?? 2} labelOf={recovery.labelOf} />
      </main>
    </div>
  );
}
