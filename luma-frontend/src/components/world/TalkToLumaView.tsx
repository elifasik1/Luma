"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  applyDailyPathChange,
  getDailyPath,
  getDreamDetails,
  getDreamGoals,
  sendAgentMessage,
} from "@/lib/api";
import type {
  DailyPathChangeProposal,
  DailyPathResponse,
  DreamDetailsResponse,
  LumaConversationTurn,
} from "@/lib/types";
import { Button } from "@/components/ui/Button";
import { DreamThemeProvider } from "@/components/world/DreamThemeProvider";

interface TalkToLumaViewProps {
  dreamId: string;
}

interface ConversationMessage extends LumaConversationTurn {
  proposal?: DailyPathChangeProposal;
  proposalReason?: string;
  proposalStatus?: "approved" | "kept";
  updatedDailyPath?: DailyPathResponse;
}

export function TalkToLumaView({ dreamId }: TalkToLumaViewProps) {
  const router = useRouter();
  const [dream, setDream] = useState<DreamDetailsResponse | null>(null);
  const [messages, setMessages] = useState<ConversationMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [isLoadingDream, setIsLoadingDream] = useState(true);
  const [isSending, setIsSending] = useState(false);
  const [isApplyingChange, setIsApplyingChange] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const conversationEndRef = useRef<HTMLDivElement>(null);
  const preparedPlanDreamId = useRef<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    getDreamDetails(dreamId)
      .then((details) => {
        if (!isMounted) return;
        setDream(details);

        const storageKey = `luma_talk_history_${dreamId}`;
        let restored = false;
        if (typeof window !== "undefined") {
          try {
            const raw = window.sessionStorage.getItem(storageKey);
            if (raw) {
              const parsed = JSON.parse(raw);
              if (Array.isArray(parsed) && parsed.length > 0) {
                setMessages(parsed);
                restored = true;
              }
            }
          } catch {
            // ignore and fallback
          }
        }

        if (!restored) {
          setMessages([
            {
              role: "assistant",
              content: `I’m here with you as you shape “${details.title}.” We can talk about the future you imagine, the steps in front of you, or whatever feels present today.`,
            },
          ]);
        }
      })
      .catch((loadError: unknown) => {
        if (isMounted) {
          setError(loadError instanceof Error ? loadError.message : "I couldn't find this future.");
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoadingDream(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [dreamId]);

  useEffect(() => {
    if (typeof window === "undefined" || !dreamId || messages.length === 0) return;
    try {
      window.sessionStorage.setItem(`luma_talk_history_${dreamId}`, JSON.stringify(messages));
    } catch {
      // ignore storage quota or private browsing issues
    }
  }, [dreamId, messages]);

  useEffect(() => {
    conversationEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [messages, isSending]);

  async function submitMessage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const message = draft.trim();
    if (!message || isSending) return;

    setMessages((current) => [...current, { role: "user", content: message }]);
    setDraft("");
    setError(null);
    setIsSending(true);

    try {
      if (preparedPlanDreamId.current !== dreamId) {
        await getDreamGoals(dreamId);
        await getDailyPath(dreamId);
        preparedPlanDreamId.current = dreamId;
      }

      const urlDreamId = typeof window !== "undefined" ? new URLSearchParams(window.location.search).get("id") : null;
      console.log(`[FRONTEND_AGENT] urlDreamId=${urlDreamId} resolvedDreamId=${dreamId} sendAgentMessageDreamId=${dreamId}`);
      const response = await sendAgentMessage(dreamId, message);
      const actionProposal = response.actionProposal;
      setMessages((current) => [
        ...current,
        {
          role: "assistant",
          content: response.message,
          proposal: actionProposal?.proposal ?? undefined,
          proposalReason: actionProposal?.reason,
        },
      ]);
    } catch (sendError: unknown) {
      setDraft(message);
      setError(sendError instanceof Error ? sendError.message : "LUMA couldn't respond just now.");
      setMessages((current) => current.slice(0, -1));
    } finally {
      setIsSending(false);
    }
  }

  function askPrompt(prompt: string) {
    setDraft(prompt);
  }

  async function approveProposal(messageIndex: number, proposal: DailyPathChangeProposal) {
    if (isApplyingChange) return;
    setError(null);
    setIsApplyingChange(true);

    try {
      const updatedDailyPath = await applyDailyPathChange(dreamId, proposal);
      setMessages((current) =>
        current.map((message, index) =>
          index === messageIndex
            ? { ...message, proposalStatus: "approved", updatedDailyPath }
            : message
        )
      );
    } catch (applyError: unknown) {
      setError(
        applyError instanceof Error
          ? applyError.message
          : "Today's path could not be updated."
      );
    } finally {
      setIsApplyingChange(false);
    }
  }

  function keepCurrentPlan(messageIndex: number) {
    setMessages((current) =>
      current.map((message, index) =>
        index === messageIndex ? { ...message, proposalStatus: "kept" } : message
      )
    );
  }

  if (!dream && isLoadingDream) {
    return (
      <main className="mx-auto flex min-h-[70vh] w-full max-w-3xl items-center justify-center px-5">
        <p className="font-serif text-2xl text-luma-primary">Making space for your conversation…</p>
      </main>
    );
  }

  if (!dream) {
    return (
      <main className="mx-auto flex min-h-[70vh] w-full max-w-3xl flex-col items-center justify-center px-5 text-center">
        <h1 className="font-serif text-3xl text-luma-primary">This future isn’t here yet.</h1>
        <p className="mt-3 text-luma-text-secondary">{error ?? "Open a world to begin talking with LUMA."}</p>
        <Button className="mt-6" onClick={() => router.push("/discover")}>Start imagining</Button>
      </main>
    );
  }

  return (
    <DreamThemeProvider dream={dream}>
    <main className="mx-auto flex min-h-[calc(100svh-2rem)] w-full max-w-4xl flex-col px-4 py-5 sm:px-8 sm:py-8">
      <header className="mb-5 flex items-center justify-between border-b border-luma-primary/10 pb-4 sm:mb-8">
        <Link
          href={`/your-world?id=${encodeURIComponent(dreamId)}`}
          className="inline-flex items-center gap-2 text-sm text-luma-text-secondary transition-colors hover:text-luma-primary"
        >
          <span aria-hidden="true">←</span>
          Back to your world
        </Link>
        <div className="text-right">
          <p className="font-serif text-lg tracking-wide text-luma-primary">LUMA</p>
          <p className="text-xs text-luma-text-secondary">Talk to your future ✦</p>
        </div>
      </header>

      <section className="mb-5 rounded-[1.5rem] border border-luma-primary/10 bg-luma-bg-secondary/45 px-5 py-4 sm:mb-7 sm:px-7 sm:py-5">
        <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
          A conversation within
        </p>
        <h1 className="mt-1 font-serif text-2xl text-luma-primary sm:text-3xl">{dream.title}</h1>
        {dream.futureProfile && (
          <p className="mt-1 text-sm text-luma-text-secondary">
            Your possible future as {dream.futureProfile.identity}
          </p>
        )}
      </section>

      <section
        aria-label="Conversation with LUMA"
        aria-live="polite"
        className="flex-1 space-y-5 overflow-y-auto rounded-[1.75rem] border border-luma-primary/10 bg-luma-bg-secondary/25 p-4 sm:p-7"
      >
        {messages.map((message, index) => (
          <article
            key={`${message.role}-${index}`}
            className={`flex ${message.role === "user" ? "justify-end" : "justify-start"}`}
          >
            <div className={`max-w-[88%] sm:max-w-[78%] ${message.role === "assistant" ? "flex items-start gap-3" : ""}`}>
              {message.role === "assistant" && (
                <span
                  aria-hidden="true"
                  className="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-luma-accent/70 to-luma-surface text-sm text-luma-primary"
                >
                  ✦
                </span>
              )}
              <div className="space-y-3">
              <div
                className={`rounded-3xl px-4 py-3 text-sm leading-relaxed sm:px-5 sm:py-4 sm:text-base ${
                  message.role === "user"
                    ? "rounded-br-md bg-luma-primary text-luma-cream"
                    : "rounded-tl-md border border-luma-primary/10 bg-luma-bg/80 text-luma-text shadow-[var(--luma-shadow)]"
                }`}
              >
                {message.content}
              </div>
              {message.proposal && (
                <div className="rounded-[1.5rem] border border-luma-accent/30 bg-gradient-to-br from-luma-accent/25 via-luma-bg/85 to-luma-surface/30 p-4 shadow-[var(--luma-shadow)] sm:p-5">
                  <p className="font-serif text-lg text-luma-primary">I&apos;d make today lighter.</p>
                  {message.proposalReason && (
                    <p className="mt-2 text-sm leading-relaxed text-luma-text-secondary">
                      {message.proposalReason}
                    </p>
                  )}
                  <p className="mt-2 text-sm leading-relaxed text-luma-text">
                    Keep: {message.proposal.keepActionTitle} — {message.proposal.keepActionEstimatedMinutes} min
                  </p>
                  <p className="mt-1 text-sm text-luma-text-secondary">
                    {message.proposal.restActionIds.length === 1
                      ? "Let the other step rest for today."
                      : `Let ${message.proposal.restActionIds.length} other steps rest for today.`}
                  </p>
                  {message.proposalStatus === "approved" ? (
                    <div
                      className="mt-4 rounded-2xl border border-emerald-500/25 bg-emerald-500/10 p-4 space-y-3 animate-luma-fade-in"
                      role="status"
                    >
                      <div className="flex items-center gap-2 text-emerald-800">
                        <span
                          className="flex h-5 w-5 items-center justify-center rounded-full bg-emerald-600 text-white text-xs font-bold"
                          aria-hidden="true"
                        >
                          ✓
                        </span>
                        <p className="text-sm font-semibold text-emerald-950">
                          Yeni planınız Today ekranına başarıyla uygulandı
                        </p>
                      </div>
                      <p className="text-xs text-luma-text-secondary leading-relaxed">
                        Seçilen aksiyon korundu, diğer adımlar dinlenmeye alındı. Bugünkü adımlarınızı kontrol etmek için Today ekranına geçebilirsiniz.
                      </p>
                      {message.updatedDailyPath && (
                        <p className="text-xs text-luma-primary/80 font-medium">
                          {message.updatedDailyPath.completedActions} tamamlandı ·{" "}
                          {message.updatedDailyPath.skippedActions} dinleniyor ·{" "}
                          {
                            message.updatedDailyPath.actions.filter(
                              (action) => action.status === "Pending"
                            ).length
                          }{" "}
                          açık adım
                        </p>
                      )}
                      <div className="pt-1">
                        <Link href={`/daily-path?id=${encodeURIComponent(dreamId)}`}>
                          <Button size="sm" className="shadow-sm">
                            Today Ekranına Git →
                          </Button>
                        </Link>
                      </div>
                    </div>
                  ) : message.proposalStatus === "kept" ? (
                    <p className="mt-4 text-sm text-luma-text-secondary" role="status">
                      Of course — your current plan stays as it is.
                    </p>
                  ) : (
                    <div className="mt-4 flex flex-wrap gap-2">
                      <Button
                        type="button"
                        size="sm"
                        disabled={isApplyingChange}
                        onClick={() => void approveProposal(index, message.proposal!)}
                      >
                        {isApplyingChange ? "Making space…" : "Adjust my day"}
                      </Button>
                      <button
                        type="button"
                        disabled={isApplyingChange}
                        onClick={() => keepCurrentPlan(index)}
                        className="rounded-full border border-luma-primary/15 bg-luma-bg/60 px-4 py-2 text-sm text-luma-text-secondary transition hover:border-luma-accent/60 hover:text-luma-primary disabled:opacity-50"
                      >
                        Not now
                      </button>
                    </div>
                  )}
                </div>
              )}
              </div>
            </div>
          </article>
        ))}

        {isSending && (
          <div className="flex items-center gap-3 text-sm text-luma-text-secondary" role="status">
            <span className="flex h-8 w-8 items-center justify-center rounded-full bg-luma-surface/60 text-luma-primary">✦</span>
            LUMA is thinking with you…
          </div>
        )}
        <div ref={conversationEndRef} />
      </section>

      {messages.length === 1 && !isSending && (
        <div className="flex flex-wrap gap-2 py-4">
          {["I only have 30 minutes today.", "I’m feeling a little stuck."].map((prompt) => (
            <button
              key={prompt}
              type="button"
              onClick={() => askPrompt(prompt)}
              className="rounded-full border border-luma-primary/15 bg-luma-bg/50 px-4 py-2 text-xs text-luma-text-secondary transition hover:border-luma-accent/60 hover:text-luma-primary sm:text-sm"
            >
              {prompt}
            </button>
          ))}
        </div>
      )}

      <form onSubmit={submitMessage} className="mt-4">
        {error && (
          <p className="mb-3 rounded-xl border border-rose-200 bg-rose-50/80 px-4 py-3 text-sm text-rose-800" role="alert">
            {error}
          </p>
        )}
        <label htmlFor="luma-message" className="sr-only">Your message to LUMA</label>
        <div className="flex items-end gap-3 rounded-[1.5rem] border border-luma-primary/15 bg-luma-bg/80 p-2 shadow-[var(--luma-shadow)] focus-within:border-luma-accent/70">
          <textarea
            id="luma-message"
            rows={2}
            maxLength={2000}
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            placeholder="What’s on your mind today?"
            className="max-h-40 min-h-12 flex-1 resize-y bg-transparent px-3 py-2 text-sm leading-relaxed text-luma-text outline-none placeholder:text-luma-text-secondary/60 sm:text-base"
            disabled={isSending}
          />
          <Button type="submit" size="md" disabled={!draft.trim() || isSending} className="mb-0.5 shrink-0">
            <span className="hidden sm:inline">Send</span>
            <span className="sm:hidden" aria-label="Send">↑</span>
          </Button>
        </div>
        <p className="mt-2 text-center text-[11px] text-luma-text-secondary/70">
          A gentle conversation, grounded in the future you’re shaping. No steps change unless you choose them.
        </p>
      </form>
    </main>
    </DreamThemeProvider>
  );
}
