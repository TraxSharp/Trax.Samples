import { useEffect, useState } from "react";
import type { Attempt, Step } from "../types";

interface Props {
  attempts: Attempt[];
  maxRetries: number;
  labelOf(attempt: Attempt, index: number): string;
}

const time = (iso: string | null | undefined) => (iso ? new Date(iso).getTime() : NaN);

/** Why a decision step was or was not replayed, from the event and the attempt's decision journal. */
function badgeOf(attempt: Attempt, index: number, step: Step): { text: string; tone: string } | null {
  if (step.kind === "JUNCTION" || step.kind === "ROUTE") return null;
  if (step.replayed) return { text: "replayed: model not asked", tone: "replay" };
  const entry = attempt.journal?.decisions.find((d) => d.questionKey === step.questionKey);
  if (entry?.replayRefused) return { text: "asked afresh: state changed", tone: "afresh" };
  if (attempt.journal?.replayAbandoned) return { text: "asked afresh: replay abandoned", tone: "afresh" };
  if (index > 0 || attempt.origin === "requeue")
    return attempt.journal && attempt.journal.replayDecisionsOf == null
      ? { text: "asked afresh: on purpose", tone: "afresh" }
      : { text: "asked afresh", tone: "afresh" };
  return { text: "model asked", tone: "model" };
}

export function TimelinePanel({ attempts, maxRetries, labelOf }: Props) {
  const [now, setNow] = useState(Date.now());
  const running = attempts.some((a) => a.trainState === "IN_PROGRESS" || a.trainState === "PENDING");
  useEffect(() => {
    if (!running) return;
    const timer = setInterval(() => setNow(Date.now()), 200);
    return () => clearInterval(timer);
  }, [running]);

  if (attempts.length === 0)
    return (
      <section className="panel timeline-panel">
        <h2>Timeline</h2>
        <div className="hint">Each attempt's junctions, questions and tracks appear here as they happen.</div>
      </section>
    );

  const starts = attempts.map((a) => time(a.startTime)).filter((t) => !isNaN(t));
  const t0 = Math.min(...starts);
  const ends = attempts.flatMap((a) => [time(a.endTime), ...Object.values(a.steps).map((s) => time(s.endedAt ?? s.startedAt))]);
  const t1 = Math.max(running ? now : 0, ...ends.filter((t) => !isNaN(t)), t0 + 1000);
  const pct = (t: number) => `${((t - t0) / (t1 - t0)) * 100}%`;
  const width = (from: number, to: number) => `max(6px, ${((Math.max(to, from) - from) / (t1 - t0)) * 100}%)`;

  return (
    <section className="panel timeline-panel">
      <h2>Timeline</h2>
      {attempts.map((attempt, index) => {
        const steps = Object.values(attempt.steps).sort((a, b) => a.position - b.position);
        const status =
          attempt.trainState === "COMPLETED"
            ? index > 0 && attempt.origin === "manifest" ? "Recovered" : "Completed"
            : attempt.trainState === "FAILED"
              ? attempt.origin === "manifest" && index < maxRetries ? `Failed, retrying ${index + 1}/${maxRetries}` : "Failed"
              : "Running";
        let previousEnd = time(attempt.startTime);
        return (
          <div key={attempt.id} className="lane">
            <div className="lane-label">
              <strong>{labelOf(attempt, index)}</strong> execution {attempt.id}
              <span className={`status ${attempt.trainState.toLowerCase()}`}>{status}</span>
            </div>
            <div className="track">
              {steps.map((step) => {
                const started = time(step.startedAt);
                const ended = step.endedAt ? time(step.endedAt) : step.state === "IN_PROGRESS" ? now : started;
                // A question's bar runs from the previous step's end to its answer: the time the model took.
                const from = step.kind === "JUNCTION" ? started : Math.min(previousEnd, started);
                const to = step.kind === "JUNCTION" ? ended : started;
                previousEnd = Math.max(previousEnd, to);
                const badge = badgeOf(attempt, index, step);
                const label = step.nameWithheld ? "(withheld)" : step.kind === "ROUTE" ? `→ ${step.answer}` : step.kind === "JUNCTION" ? step.name : `${step.questionKey}?`;
                return (
                  <div
                    key={step.position}
                    className={`bar ${step.kind.toLowerCase()} ${step.state.toLowerCase()} ${step.replayed ? "replayed" : ""}`}
                    style={{ left: pct(from), width: width(from, to) }}
                    title={[
                      `#${step.position} ${step.kind} ${label}`,
                      step.answer != null ? `answer ${step.answer}` : "",
                      step.confidence != null ? `confidence ${step.confidence}` : "",
                      step.failureException ? `failed: ${step.failureException}` : "",
                      badge?.text ?? "",
                    ]
                      .filter(Boolean)
                      .join("\n")}
                  >
                    <span>{label}</span>
                  </div>
                );
              })}
            </div>
            <div className="badges">
              {steps.map((step) => {
                const badge = badgeOf(attempt, index, step);
                return badge ? (
                  <span key={step.position} className={`badge ${badge.tone}`}>
                    {step.questionKey} = {step.answer}: {badge.text}
                  </span>
                ) : null;
              })}
              {attempt.failureJunction && (
                <span className="badge failed">
                  crashed in {attempt.failureJunction}
                </span>
              )}
            </div>
          </div>
        );
      })}
    </section>
  );
}
