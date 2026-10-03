import { useApolloClient } from "@apollo/client";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  CHANGE_CASE_DATA,
  DECISION_JOURNAL,
  EXECUTION,
  EXECUTIONS,
  JUNCTION_RUNS,
  ON_JUNCTION_EVENT,
  REQUEUE,
  START_RUN,
  TRIGGER_ASK_AFRESH,
  WORK_QUEUE_ENTRY,
} from "./graphql";
import type {
  Attempt,
  ConsoleLine,
  Journal,
  Phase,
  RunInfo,
  Scenario,
  Step,
  Tone,
} from "./types";

const POLL_MS = 500;
const RANK = { IN_PROGRESS: 0, COMPLETED: 1, FAILED: 1, CANCELLED: 1 } as const;
const isQuestion = (s: Step) => s.kind !== "JUNCTION" && s.kind !== "ROUTE";

interface ExecutionRow {
  id: number;
  trainState: string;
  startTime: string;
  endTime: string | null;
  failureJunction: string | null;
  failureReason: string | null;
}

/**
 * Follows one demo run: its manifest's attempts (found by polling operations.executions), each
 * attempt's steps (onJunctionEvent, merged with operations.junctionRuns by position), and the
 * decision journal once an attempt ends. Narrates what it sees into console lines.
 */
export function useRecoveryRun() {
  const client = useApolloClient();
  const [run, setRun] = useState<RunInfo | null>(null);
  const [attempts, setAttempts] = useState<Attempt[]>([]);
  const [lines, setLines] = useState<ConsoleLine[]>([]);
  const [starting, setStarting] = useState(false);

  const narrated = useRef(new Set<string>());
  const subscriptions = useRef(new Map<number, { unsubscribe(): void }>());
  const journaled = useRef(new Set<number>());

  const say = useCallback((key: string, at: string, tone: Tone, text: string) => {
    if (narrated.current.has(key)) return;
    narrated.current.add(key);
    setLines((all) => [...all, { key, at, tone, text }]);
  }, []);

  const narrate = useCallback(
    (attemptId: number, label: string, step: Step) => {
      const key = `${attemptId}:${step.position}:${step.state}`;
      const at = step.endedAt ?? step.startedAt;
      const name = step.nameWithheld ? "(withheld)" : step.name;
      if (step.kind === "JUNCTION") {
        if (step.state === "IN_PROGRESS")
          say(key, step.startedAt, "info", `${label} # JUNCTION ${name} is running...`);
        else if (step.state === "COMPLETED")
          say(key, at, "info", `${label} # JUNCTION ${name} completed in ${Math.round(step.durationMs ?? 0)} ms`);
        else
          say(
            key,
            at,
            "error",
            `${label} # JUNCTION ${name} failed with ${step.failureException ?? "an exception"}. The run has crashed: Trax records the failure and the manifest will retry it.`,
          );
      } else if (step.kind === "ROUTE") {
        say(key, at, "info", `${label} # ROUTE took the ${step.answer ?? "(withheld)"} track of ${step.questionKey ?? name}`);
      } else if (step.replayed) {
        say(
          key,
          at,
          "replay",
          `${label} # MODEL not asked: ${step.questionKey} = ${step.answer} replayed from the failed attempt, because the state hashes the same.`,
        );
      } else {
        const confidence = step.confidence != null ? ` (confidence ${step.confidence.toFixed(2)})` : "";
        say(key, at, "model", `${label} # MODEL asked: ${step.questionKey} = ${step.answer}${confidence}. Trax recorded the answer before acting on it.`);
      }
    },
    [say],
  );

  const mergeSteps = useCallback(
    (attemptId: number, incoming: Step[]) => {
      setAttempts((all) =>
        all.map((a) => {
          if (a.id !== attemptId) return a;
          const steps = { ...a.steps };
          for (const step of incoming) {
            const known = steps[step.position];
            if (!known || RANK[known.state] < RANK[step.state]) steps[step.position] = step;
          }
          return { ...a, steps };
        }),
      );
    },
    [],
  );

  const labelOf = useCallback((attempt: Attempt, index: number) =>
    attempt.origin === "requeue" ? `[requeue ${attempt.id}]` : `[attempt ${index + 1}]`, []);

  const readStored = useCallback(
    async (attemptId: number) => {
      const { data } = await client.query({ query: JUNCTION_RUNS, variables: { metadataId: attemptId } });
      mergeSteps(attemptId, data.operations.junctionRuns as Step[]);
    },
    [client, mergeSteps],
  );

  const follow = useCallback(
    (row: ExecutionRow, origin: Attempt["origin"], askedAfresh = false) => {
      if (subscriptions.current.has(row.id)) return;
      setAttempts((all) =>
        all.some((a) => a.id === row.id)
          ? all
          : [...all, { ...row, origin, askedAfresh, steps: {}, journal: null }],
      );
      const sub = client
        .subscribe({ query: ON_JUNCTION_EVENT, variables: { metadataId: row.id } })
        .subscribe({
          next: ({ data }) => {
            if (data?.onJunctionEvent) mergeSteps(row.id, [data.onJunctionEvent.junction as Step]);
          },
        });
      subscriptions.current.set(row.id, sub);
      // Subscribe first, then read what was stored before the subscription started.
      void readStored(row.id);
    },
    [client, mergeSteps, readStored],
  );

  const updateRow = useCallback((row: ExecutionRow) => {
    setAttempts((all) => all.map((a) => (a.id === row.id ? { ...a, ...row } : a)));
  }, []);

  // Poll the manifest's executions for new attempts and each attempt's state.
  useEffect(() => {
    if (!run) return;
    let cancelled = false;
    const tick = async () => {
      try {
        const { data } = await client.query({ query: EXECUTIONS, variables: { manifestId: run.manifestId } });
        if (cancelled) return;
        for (const row of data.operations.executions.items as ExecutionRow[]) {
          follow(row, "manifest");
          updateRow(row);
        }
        for (const id of [...subscriptions.current.keys()]) {
          const known = (data.operations.executions.items as ExecutionRow[]).some((r) => r.id === id);
          if (!known) {
            const one = await client.query({ query: EXECUTION, variables: { id } });
            if (one.data.operations.execution) updateRow(one.data.operations.execution as ExecutionRow);
          }
        }
      } catch {
        // The host may be restarting; the next tick tries again.
      }
    };
    void tick();
    const timer = setInterval(tick, POLL_MS);
    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  }, [client, run, follow, updateRow]);

  // Once an attempt ends: read its steps one last time and its decision journal.
  useEffect(() => {
    for (const a of attempts) {
      if (a.trainState === "COMPLETED" || a.trainState === "FAILED" || a.trainState === "CANCELLED") {
        if (journaled.current.has(a.id)) continue;
        journaled.current.add(a.id);
        void (async () => {
          await readStored(a.id);
          const { data } = await client.query({ query: DECISION_JOURNAL, variables: { metadataId: a.id } });
          const journal = data.discover.decisionJournal as Journal;
          setAttempts((all) => all.map((x) => (x.id === a.id ? { ...x, journal } : x)));
        })();
      }
    }
  }, [attempts, client, readStored]);

  // Narrate every step and attempt outcome once.
  useEffect(() => {
    attempts.forEach((a, index) => {
      const label = labelOf(a, index);
      if (index === 0 || a.origin === "requeue")
        say(`${a.id}:start`, a.startTime, "system", `${label} # RUN execution ${a.id} started`);
      else
        say(`${a.id}:start`, a.startTime, "system", `${label} # RETRY ${index}/${run?.maxRetries ?? 2}: the manifest's retry started as execution ${a.id}`);
      Object.values(a.steps)
        .sort((x, y) => x.position - y.position)
        .forEach((s) => narrate(a.id, label, s));
      if (a.trainState === "COMPLETED" && a.journal)
        say(`${a.id}:end`, a.endTime ?? a.startTime, "success", `${label} # COMPLETED. ${describeJournal(a.journal)}`);
      if (a.trainState === "FAILED" && a.origin === "manifest" && index < (run?.maxRetries ?? 2))
        say(`${a.id}:end`, a.endTime ?? a.startTime, "system", `${label} # FAILED. The scheduler retries after its backoff (a few seconds here), naming execution ${a.id} as the run to replay.`);
    });
  }, [attempts, labelOf, narrate, run, say]);

  const reset = useCallback(() => {
    subscriptions.current.forEach((s) => s.unsubscribe());
    subscriptions.current.clear();
    narrated.current.clear();
    journaled.current.clear();
    setAttempts([]);
    setLines([]);
    setRun(null);
  }, []);

  useEffect(() => {
    const live = subscriptions.current;
    return () => live.forEach((s) => s.unsubscribe());
  }, []);

  const start = useCallback(
    async (scenario: Scenario, crashOnce: boolean, orderId: string, topic: string) => {
      reset();
      setStarting(true);
      try {
        const { data } = await client.mutate({
          mutation: START_RUN,
          variables: {
            input: scenario === "RESEARCH" ? { scenario, crashOnce, topic } : { scenario, crashOnce, orderId },
          },
        });
        const output = data.dispatch.startRun.output;
        setRun({ ...output, scenario });
        say(
          "start",
          new Date().toISOString(),
          "system",
          `# Scheduled one-off manifest ${output.manifestExternalId} (MaxRetries ${output.maxRetries})${output.armedCrash !== "NONE" ? `, crash armed at ${output.armedCrash.toLowerCase()} for the first attempt` : ""}`,
        );
      } catch (error) {
        say(`start-error-${Date.now()}`, new Date().toISOString(), "error", `# Could not start: ${(error as Error).message}`);
      } finally {
        setStarting(false);
      }
    },
    [client, reset, say],
  );

  const phase: Phase = useMemo(() => {
    if (starting) return "starting";
    if (!run) return "idle";
    const manifestRuns = attempts.filter((a) => a.origin === "manifest");
    const last = manifestRuns[manifestRuns.length - 1];
    if (!last) return "starting";
    if (last.trainState === "COMPLETED") return "done";
    if (last.trainState === "FAILED")
      return manifestRuns.length > (run.maxRetries ?? 2) ? "dead" : "breaks";
    return manifestRuns.length > 1 ? "recovers" : "runs";
  }, [attempts, run, starting]);

  const askAfresh = useCallback(async () => {
    if (!run) return;
    const now = new Date().toISOString();
    if (phase === "breaks") {
      const { data } = await client.mutate({ mutation: TRIGGER_ASK_AFRESH, variables: { externalId: run.manifestExternalId } });
      say(`afresh-${Date.now()}`, now, "system", `# ASK AFRESH: triggerManifest(askAfresh: true) says "${data.operations.triggerManifest.message}"`);
      return;
    }
    const last = attempts[attempts.length - 1];
    if (!last) return;
    const { data } = await client.mutate({ mutation: REQUEUE, variables: { id: last.id, askAfresh: true } });
    const result = data.operations.requeueExecution;
    say(`requeue-${Date.now()}`, now, "system", `# ASK AFRESH: requeueExecution(${last.id}, askAfresh: true) says "${result.message}"`);
    if (!result.success || result.id == null) return;
    for (let i = 0; i < 60; i++) {
      const entry = await client.query({ query: WORK_QUEUE_ENTRY, variables: { id: result.id } });
      const metadataId = entry.data.operations.workQueue.workQueue?.metadataId;
      if (metadataId) {
        const one = await client.query({ query: EXECUTION, variables: { id: metadataId } });
        follow(one.data.operations.execution as ExecutionRow, "requeue", true);
        return;
      }
      await new Promise((r) => setTimeout(r, 250));
    }
  }, [attempts, client, follow, phase, run, say]);

  const changeData = useCallback(async () => {
    if (!run) return;
    const { data } = await client.mutate({ mutation: CHANGE_CASE_DATA, variables: { runId: run.runId } });
    say(`change-${Date.now()}`, new Date().toISOString(), "system", `# DATA CHANGED during the backoff: ${data.dispatch.changeCaseData.output.change}`);
  }, [client, run, say]);

  return { run, attempts, lines, phase, start, reset, askAfresh, changeData, labelOf };
}

function describeJournal(journal: Journal): string {
  const replayed = journal.decisions.filter((d) => d.replayed).length;
  const refused = journal.decisions.filter((d) => d.replayRefused).length;
  const asked = journal.decisions.length - replayed;
  const parts = [`${journal.decisions.length} decision(s) recorded`];
  if (replayed) parts.push(`${replayed} replayed without calling the model`);
  if (asked) parts.push(`${asked} asked of the model`);
  if (refused) parts.push(`${refused} replay(s) refused because the state changed`);
  return parts.join(", ") + ".";
}

export { isQuestion };
