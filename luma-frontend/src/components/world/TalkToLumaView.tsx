import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from "react";
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
  usedTools?: string[];
  timestamp?: string;
}

interface SpeechRecognitionEventLike {
  results: {
    [index: number]: {
      [index: number]: {
        transcript: string;
      };
    };
  };
}

interface SpeechRecognitionInstance {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  onresult: (event: SpeechRecognitionEventLike) => void;
  onerror: (event: unknown) => void;
  onend: () => void;
  start: () => void;
  stop: () => void;
  abort: () => void;
}

const getSpeechRecognitionClass = (): (new () => SpeechRecognitionInstance) | null => {
  if (typeof window === "undefined") return null;
  const win = window as unknown as {
    SpeechRecognition?: new () => SpeechRecognitionInstance;
    webkitSpeechRecognition?: new () => SpeechRecognitionInstance;
  };
  return win.SpeechRecognition || win.webkitSpeechRecognition || null;
};

export function TalkToLumaView({ dreamId }: TalkToLumaViewProps) {
  const router = useRouter();
  const [dream, setDream] = useState<DreamDetailsResponse | null>(null);
  const [messages, setMessages] = useState<ConversationMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [isLoadingDream, setIsLoadingDream] = useState(true);
  const [isSending, setIsSending] = useState(false);
  const [isApplyingChange, setIsApplyingChange] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isVoiceOutputEnabled, setIsVoiceOutputEnabled] = useState(true);
  const [isListening, setIsListening] = useState(false);
  const [hasSpeechRecognitionSupport] = useState(() => Boolean(getSpeechRecognitionClass()));
  const conversationEndRef = useRef<HTMLDivElement>(null);
  const preparedPlanDreamId = useRef<string | null>(null);
  const recognitionRef = useRef<SpeechRecognitionInstance | null>(null);

  // Load dream details and restore session history
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
              content: `Hello! I’m your Alexa+ companion for “${details.title}”. I can help you review today’s path, inspect your progress toward goals, or adapt your plan whenever your energy or schedule shifts.`,
              usedTools: ["get_future_context"],
              timestamp: new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
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

  // Persist session history
  useEffect(() => {
    if (typeof window === "undefined" || !dreamId || messages.length === 0) return;
    try {
      window.sessionStorage.setItem(`luma_talk_history_${dreamId}`, JSON.stringify(messages));
    } catch {
      // ignore storage quota issues
    }
  }, [dreamId, messages]);

  // Auto-scroll to latest message
  useEffect(() => {
    conversationEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [messages, isSending]);

  // Text-to-Speech helper
  function speakResponse(text: string) {
    if (!isVoiceOutputEnabled || typeof window === "undefined" || !("speechSynthesis" in window)) {
      return;
    }
    try {
      window.speechSynthesis.cancel();
      // Strip potential markdown asterisks or special characters for cleaner speech
      const cleanedText = text.replace(/[*#_`]/g, " ").trim();
      const utterance = new SpeechSynthesisUtterance(cleanedText);
      utterance.lang = "en-US";
      utterance.rate = 1.05;
      window.speechSynthesis.speak(utterance);
    } catch {
      // Ignore audio synthesis errors on unsupported browsers
    }
  }

  // Toggle voice recognition (Push-To-Talk / Tap-To-Speak)
  function toggleListening() {
    if (isListening) {
      recognitionRef.current?.stop();
      setIsListening(false);
      return;
    }

    const SpeechClass = getSpeechRecognitionClass();
    if (!SpeechClass) {
      setError("Speech recognition is not supported in this browser. You can type your message below.");
      return;
    }

    try {
      const recognition = new SpeechClass();
      recognition.lang = "en-US";
      recognition.continuous = false;
      recognition.interimResults = false;

      recognition.onresult = (event: SpeechRecognitionEventLike) => {
        const transcript = event.results[0]?.[0]?.transcript;
        if (transcript) {
          setDraft(transcript);
          // Optional: automatically submit transcript
          void handleMessageDispatch(transcript);
        }
        setIsListening(false);
      };

      recognition.onerror = () => {
        setIsListening(false);
      };

      recognition.onend = () => {
        setIsListening(false);
      };

      recognitionRef.current = recognition;
      recognition.start();
      setIsListening(true);
      setError(null);
    } catch (err: unknown) {
      setIsListening(false);
      setError(err instanceof Error ? err.message : "Microphone access could not be started.");
    }
  }

  async function handleMessageDispatch(rawMessage: string) {
    const message = rawMessage.trim();
    if (!message || isSending) return;

    const timeString = new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });

    setMessages((current) => [
      ...current,
      { role: "user", content: message, timestamp: timeString },
    ]);
    setDraft("");
    setError(null);
    setIsSending(true);

    try {
      if (preparedPlanDreamId.current !== dreamId) {
        await getDreamGoals(dreamId);
        await getDailyPath(dreamId);
        preparedPlanDreamId.current = dreamId;
      }

      const response = await sendAgentMessage(dreamId, message);
      const actionProposal = response.actionProposal;
      const toolsUsed = response.usedTools || [];

      setMessages((current) => [
        ...current,
        {
          role: "assistant",
          content: response.message,
          proposal: actionProposal?.proposal ?? undefined,
          proposalReason: actionProposal?.reason,
          usedTools: toolsUsed,
          timestamp: new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
        },
      ]);

      // Speak response if voice output is enabled
      speakResponse(response.message);
    } catch (sendError: unknown) {
      setDraft(message);
      setError(sendError instanceof Error ? sendError.message : "Alexa+ couldn't respond just now.");
      setMessages((current) => current.slice(0, -1));
    } finally {
      setIsSending(false);
    }
  }

  async function submitMessage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await handleMessageDispatch(draft);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void handleMessageDispatch(draft);
    }
  }

  function handlePresetPrompt(promptText: string) {
    void handleMessageDispatch(promptText);
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

      const confirmation = "I have updated your plan for today. Your selected step is prioritized, and other steps are resting.";
      speakResponse(confirmation);
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
    speakResponse("Understood. Your current plan remains unchanged.");
  }

  if (!dream && isLoadingDream) {
    return (
      <main className="mx-auto flex min-h-[70vh] w-full max-w-3xl flex-col items-center justify-center px-5 text-center">
        <div className="relative flex h-14 w-14 items-center justify-center rounded-full bg-cyan-500/10 ring-2 ring-cyan-400/40 animate-pulse">
          <span className="text-xl text-cyan-400">✦</span>
        </div>
        <p className="mt-4 font-serif text-xl text-luma-primary">Connecting to Alexa+ Companion Simulator…</p>
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
      <main className="mx-auto flex min-h-[calc(100svh-2rem)] w-full max-w-4xl flex-col px-4 py-4 sm:px-8 sm:py-6">
        {/* Navigation & Header */}
        <header className="mb-4 flex flex-wrap items-center justify-between gap-3 border-b border-luma-primary/10 pb-4 sm:mb-6">
          <Link
            href={`/your-world?id=${encodeURIComponent(dreamId)}`}
            className="inline-flex items-center gap-2 text-sm text-luma-text-secondary transition-colors hover:text-luma-primary"
          >
            <span aria-hidden="true">←</span>
            Back to your world
          </Link>

          {/* Audio Controls */}
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setIsVoiceOutputEnabled((prev) => !prev)}
              aria-label={isVoiceOutputEnabled ? "Mute Alexa voice output" : "Enable Alexa voice output"}
              className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-medium transition-all ${
                isVoiceOutputEnabled
                  ? "bg-cyan-500/15 text-cyan-700 dark:text-cyan-300 border border-cyan-500/30"
                  : "bg-luma-surface/40 text-luma-text-secondary border border-luma-primary/10"
              }`}
            >
              <span>{isVoiceOutputEnabled ? "🔊 Voice: On" : "🔈 Voice: Muted"}</span>
            </button>
          </div>
        </header>

        {/* Alexa+ Hackathon Simulator Banner */}
        <section className="relative mb-5 overflow-hidden rounded-2xl border border-cyan-500/25 bg-gradient-to-r from-cyan-950/20 via-luma-bg-secondary/40 to-luma-surface/30 p-4 shadow-[var(--luma-shadow)] sm:p-5">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-start gap-3.5">
              <div className="relative flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-tr from-cyan-600 to-sky-400 text-white shadow-md shadow-cyan-500/25 ring-2 ring-cyan-400/40">
                <span className="text-base font-bold">✦</span>
                <span className="absolute -bottom-0.5 -right-0.5 flex h-3.5 w-3.5 items-center justify-center rounded-full bg-emerald-500 ring-2 ring-white dark:ring-slate-900" title="Online Simulator">
                  <span className="h-1.5 w-1.5 rounded-full bg-white animate-pulse" />
                </span>
              </div>
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h1 className="font-serif text-lg font-semibold tracking-wide text-luma-primary sm:text-xl">
                    Alexa+ Companion Simulator
                  </h1>
                  <span className="rounded-full bg-cyan-500/10 px-2.5 py-0.5 text-[11px] font-semibold text-cyan-600 dark:text-cyan-400 border border-cyan-500/20">
                    Amazon Hackathon Demo
                  </span>
                </div>
                <p className="mt-1 text-xs leading-relaxed text-luma-text-secondary sm:text-sm">
                  Simulating voice & multimodal companion intelligence for <span className="font-medium text-luma-primary">“{dream.title}”</span> using LUMA&apos;s real Model Context Protocol (MCP) tool orchestration.
                </p>
              </div>
            </div>

            <div className="flex shrink-0 items-center gap-2 text-xs text-luma-text-secondary/80 sm:flex-col sm:items-end">
              <span className="inline-flex items-center gap-1.5 text-[11px] font-medium text-luma-primary/80">
                <span className="h-2 w-2 rounded-full bg-cyan-400" />
                Streamable MCP Tools Active
              </span>
              <span className="text-[10px] text-luma-text-secondary/70">
                Web Simulation (No Echo required)
              </span>
            </div>
          </div>
        </section>

        {/* Quick Action Preset Prompt Pills */}
        <section aria-label="Quick Alexa Prompts" className="mb-4">
          <p className="mb-2 text-[11px] font-semibold uppercase tracking-wider text-luma-text-secondary">
            Quick Prompts for Alexa+:
          </p>
          <div className="flex flex-wrap gap-2">
            {[
              { label: "“What should I focus on today?”", icon: "💬" },
              { label: "“I’m tired. Adjust my plan.”", icon: "⏸️" },
              { label: "“How am I progressing toward my goals?”", icon: "🎯" },
            ].map(({ label, icon }) => (
              <button
                key={label}
                type="button"
                disabled={isSending}
                onClick={() => handlePresetPrompt(label.replace(/[“”"]/g, ""))}
                className="inline-flex items-center gap-1.5 rounded-full border border-cyan-500/25 bg-luma-bg/70 px-3.5 py-1.5 text-xs font-medium text-luma-text transition-all hover:border-cyan-400 hover:bg-cyan-500/10 hover:text-cyan-700 dark:hover:text-cyan-300 disabled:opacity-50"
              >
                <span>{icon}</span>
                <span>{label}</span>
              </button>
            ))}
          </div>
        </section>

        {/* Conversation Message Stream */}
        <section
          aria-label="Alexa+ Simulated Conversation"
          aria-live="polite"
          className="flex-1 space-y-5 overflow-y-auto rounded-[1.75rem] border border-luma-primary/10 bg-luma-bg-secondary/20 p-4 sm:p-6"
        >
          {messages.map((message, index) => (
            <article
              key={`${message.role}-${index}`}
              className={`flex ${message.role === "user" ? "justify-end" : "justify-start"}`}
            >
              <div
                className={`max-w-[92%] sm:max-w-[82%] ${
                  message.role === "assistant" ? "flex items-start gap-3" : ""
                }`}
              >
                {message.role === "assistant" && (
                  <div
                    aria-hidden="true"
                    className="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-cyan-600 to-sky-500 text-xs font-bold text-white shadow-sm ring-2 ring-cyan-400/30"
                  >
                    ✦
                  </div>
                )}

                <div className="w-full space-y-2.5">
                  {/* Sender identity & Timestamp */}
                  <div className={`flex items-center gap-2 text-[11px] ${message.role === "user" ? "justify-end text-luma-text-secondary" : "text-cyan-700 dark:text-cyan-400 font-semibold"}`}>
                    <span>{message.role === "user" ? "You" : "Alexa+ (LUMA Companion)"}</span>
                    {message.timestamp && (
                      <span className="text-[10px] text-luma-text-secondary/60">· {message.timestamp}</span>
                    )}
                  </div>

                  {/* Tools Executed Badge (Proof of Real MCP Tool Orchestration) */}
                  {message.role === "assistant" && message.usedTools && message.usedTools.length > 0 && (
                    <div className="flex flex-wrap items-center gap-1.5 text-[11px]">
                      <span className="font-medium text-luma-text-secondary flex items-center gap-1">
                        <span className="text-cyan-500">⚡</span> Tools:
                      </span>
                      {message.usedTools.map((tool) => (
                        <span
                          key={tool}
                          className="rounded-md bg-cyan-500/10 px-2 py-0.5 font-mono text-[10px] font-semibold text-cyan-800 dark:text-cyan-300 border border-cyan-500/20"
                        >
                          {tool}
                        </span>
                      ))}
                    </div>
                  )}

                  {/* Message Bubble */}
                  <div
                    className={`rounded-2xl px-4 py-3 text-sm leading-relaxed sm:px-5 sm:py-3.5 sm:text-base ${
                      message.role === "user"
                        ? "rounded-br-sm bg-luma-primary text-luma-bg"
                        : "rounded-tl-sm border border-luma-primary/10 bg-luma-bg/85 text-luma-text shadow-[var(--luma-shadow)]"
                    }`}
                  >
                    {message.content}
                  </div>

                  {/* Action Proposal Card (Multi-Turn Approval Boundary) */}
                  {message.proposal && (
                    <div className="rounded-2xl border border-cyan-500/30 bg-gradient-to-br from-cyan-500/10 via-luma-bg/90 to-luma-surface/40 p-4 shadow-[var(--luma-shadow)] sm:p-5">
                      <div className="flex items-center justify-between gap-2 border-b border-cyan-500/20 pb-3">
                        <div className="flex items-center gap-2 text-cyan-700 dark:text-cyan-300">
                          <span className="flex h-5 w-5 items-center justify-center rounded-full bg-cyan-500/20 text-xs font-bold">
                            ⚡
                          </span>
                          <p className="font-serif text-base font-semibold sm:text-lg">
                            Adaptive Plan Recommendation
                          </p>
                        </div>
                        <span className="rounded-full bg-amber-500/15 px-2.5 py-0.5 text-[10px] font-semibold text-amber-700 dark:text-amber-300 border border-amber-500/25">
                          Requires Approval
                        </span>
                      </div>

                      {message.proposalReason && (
                        <p className="mt-2.5 text-xs leading-relaxed text-luma-text-secondary sm:text-sm">
                          {message.proposalReason}
                        </p>
                      )}

                      <div className="mt-3 space-y-2 rounded-xl bg-luma-surface/50 p-3 text-xs sm:text-sm">
                        <div className="flex items-start gap-2 text-luma-text">
                          <span className="text-emerald-500 font-bold">✓ Keep:</span>
                          <div>
                            <span className="font-semibold">{message.proposal.keepActionTitle}</span>
                            <span className="ml-2 inline-block rounded-md bg-luma-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-luma-primary">
                              {message.proposal.keepActionEstimatedMinutes} min
                            </span>
                          </div>
                        </div>

                        <div className="flex items-center gap-2 text-luma-text-secondary">
                          <span className="text-amber-500 font-bold">⏸ Rest:</span>
                          <span>
                            {message.proposal.restActionIds.length === 1
                              ? "1 remaining step set aside for rest today."
                              : `${message.proposal.restActionIds.length} steps set aside for rest today.`}
                          </span>
                        </div>
                      </div>

                      {/* Approval Status State */}
                      {message.proposalStatus === "approved" ? (
                        <div
                          className="mt-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10 p-3.5 space-y-2 animate-luma-fade-in"
                          role="status"
                        >
                          <div className="flex items-center gap-2 text-emerald-800 dark:text-emerald-300">
                            <span className="flex h-5 w-5 items-center justify-center rounded-full bg-emerald-600 text-white text-xs font-bold">
                              ✓
                            </span>
                            <p className="text-xs font-semibold sm:text-sm">
                              Plan change safely applied to database
                            </p>
                          </div>
                          {message.updatedDailyPath && (
                            <p className="text-xs text-luma-text-secondary">
                              Updated today: {message.updatedDailyPath.completedActions} completed ·{" "}
                              {message.updatedDailyPath.skippedActions} resting ·{" "}
                              {
                                message.updatedDailyPath.actions.filter(
                                  (action) => action.status === "Pending"
                                ).length
                              }{" "}
                              active
                            </p>
                          )}
                          <div className="pt-1">
                            <Link href={`/daily-path?id=${encodeURIComponent(dreamId)}`}>
                              <Button size="sm">
                                View Today&apos;s Path →
                              </Button>
                            </Link>
                          </div>
                        </div>
                      ) : message.proposalStatus === "kept" ? (
                        <div className="mt-3 text-xs text-luma-text-secondary" role="status">
                          <span>Plan unchanged. Your existing daily routine is maintained.</span>
                        </div>
                      ) : (
                        <div className="mt-4 flex flex-wrap gap-2.5">
                          <Button
                            type="button"
                            size="sm"
                            disabled={isApplyingChange}
                            onClick={() => void approveProposal(index, message.proposal!)}
                          >
                            {isApplyingChange ? "Applying change…" : "Approve Adjustment"}
                          </Button>
                          <button
                            type="button"
                            disabled={isApplyingChange}
                            onClick={() => keepCurrentPlan(index)}
                            className="rounded-full border border-luma-primary/15 bg-luma-bg/70 px-4 py-1.5 text-xs text-luma-text-secondary transition hover:border-luma-accent/60 hover:text-luma-primary disabled:opacity-50"
                          >
                            Keep Current Plan
                          </button>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </article>
          ))}

          {/* Real-time thinking / status indicators */}
          {isSending && (
            <div className="flex items-center gap-3 text-sm text-cyan-700 dark:text-cyan-300" role="status">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-cyan-500/15 text-cyan-500 ring-2 ring-cyan-400/30 animate-pulse">
                ✦
              </span>
              <span>Alexa+ is orchestrating tools & thinking…</span>
            </div>
          )}

          {isListening && (
            <div className="flex items-center gap-3 text-sm text-rose-600 dark:text-rose-400" role="status">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-rose-500/20 text-rose-500 animate-ping">
                🎙
              </span>
              <span>Listening to your voice… speak now.</span>
            </div>
          )}

          <div ref={conversationEndRef} />
        </section>

        {/* Input & Form */}
        <form onSubmit={submitMessage} className="mt-3">
          {error && (
            <p className="mb-2.5 rounded-xl border border-rose-200 bg-rose-50/80 px-4 py-2.5 text-xs text-rose-800" role="alert">
              {error}
            </p>
          )}

          <label htmlFor="luma-message" className="sr-only">Your message to Alexa+</label>
          <div className="relative flex items-end gap-2.5 rounded-2xl border border-cyan-500/25 bg-luma-bg/85 p-2 shadow-[var(--luma-shadow)] focus-within:border-cyan-400">
            {/* Microphone Button */}
            <button
              type="button"
              onClick={toggleListening}
              disabled={isSending}
              title={
                hasSpeechRecognitionSupport
                  ? isListening
                    ? "Stop listening"
                    : "Speak with Alexa+ (Voice Input)"
                  : "Voice input not supported in this browser"
              }
              className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full transition-all ${
                isListening
                  ? "bg-rose-500 text-white shadow-lg shadow-rose-500/30 animate-pulse"
                  : hasSpeechRecognitionSupport
                  ? "bg-cyan-500/10 text-cyan-600 dark:text-cyan-300 hover:bg-cyan-500/20 border border-cyan-500/25"
                  : "bg-luma-surface/30 text-luma-text-secondary/40 cursor-not-allowed border border-transparent"
              }`}
            >
              <span aria-hidden="true" className="text-base">
                {isListening ? "⏹" : "🎙"}
              </span>
            </button>

            {/* Message Textarea */}
            <textarea
              id="luma-message"
              rows={2}
              maxLength={2000}
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={isListening ? "Listening…" : "Ask Alexa+ anything about your day, goals, or future…"}
              className="max-h-36 min-h-11 flex-1 resize-y bg-transparent px-2.5 py-2 text-sm leading-relaxed text-luma-text outline-none placeholder:text-luma-text-secondary/60 sm:text-base"
              disabled={isSending}
            />

            {/* Send Button */}
            <Button
              type="submit"
              size="md"
              disabled={!draft.trim() || isSending}
              className="mb-0.5 shrink-0"
            >
              <span className="hidden sm:inline">Ask Alexa+</span>
              <span className="sm:hidden" aria-label="Send">↑</span>
            </Button>
          </div>

          <p className="mt-2 text-center text-[11px] text-luma-text-secondary/70">
            Simulated Alexa+ Companion for Hackathon Demo. Real agent actions require your explicit confirmation before updating your day.
          </p>
        </form>
      </main>
    </DreamThemeProvider>
  );
}
