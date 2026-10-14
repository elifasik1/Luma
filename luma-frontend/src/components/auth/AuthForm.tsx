"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";

interface AuthFormProps {
  mode: "login" | "register";
}

export function AuthForm({ mode }: AuthFormProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDemoSubmitting, setIsDemoSubmitting] = useState(false);
  const isRegister = mode === "register";

  async function handleDemoLogin() {
    if (isSubmitting || isDemoSubmitting) return;
    setError(null);
    setIsDemoSubmitting(true);
    try {
      const response = await fetch("/api/auth/dev-login", {
        method: "POST",
      });
      const data = await response.json();
      if (!response.ok) {
        throw new Error(data?.error || "Could not start demo session.");
      }

      const returnUrl = searchParams.get("returnUrl");
      if (returnUrl && returnUrl.startsWith("/") && !returnUrl.startsWith("//")) {
        router.push(returnUrl);
      } else if (data.dreamId) {
        router.push(`/your-world?id=${encodeURIComponent(data.dreamId)}`);
      } else {
        router.push("/discover");
      }
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not start demo session.");
      setIsDemoSubmitting(false);
    }
  }

  function safeReturnPath(): string {
    const candidate = searchParams.get("returnUrl");
    if (!candidate || !candidate.startsWith("/") || candidate.startsWith("//")) {
      return "/discover";
    }

    try {
      const target = new URL(candidate, window.location.origin);
      return target.origin === window.location.origin
        ? `${target.pathname}${target.search}${target.hash}`
        : "/discover";
    } catch {
      return "/discover";
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    setError(null);
    setIsSubmitting(true);
    try {
      const response = await fetch(`/api/auth/${mode}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(
          isRegister ? { name, email, password } : { email, password }
        ),
      });
      const result = await response.json();
      if (!response.ok) {
        setError(result.error ?? "We couldn't complete your request.");
        return;
      }
      router.replace(safeReturnPath());
    } catch {
      setError("LUMA could not connect just now. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-[calc(100svh-2rem)] flex-1 items-center justify-center px-5 py-12">
      <section className="w-full max-w-md rounded-[2rem] border border-luma-primary/10 bg-luma-bg-secondary/60 p-7 shadow-[var(--luma-shadow)] backdrop-blur-sm sm:p-10">
        <Link href="/" className="font-serif text-xl tracking-[0.16em] text-luma-primary">
          LUMA
        </Link>
        <p className="mt-8 text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary">
          {isRegister ? "A little space to imagine" : "Talk to your future"}
        </p>
        <h1 className="mt-2 font-serif text-3xl text-luma-primary">
          {isRegister ? "Start with a possibility." : "Welcome back."}
        </h1>
        <p className="mt-2 text-sm leading-relaxed text-luma-text-secondary">
          {isRegister ? "Create your account." : "Continue shaping your future."}
        </p>

        <form onSubmit={submit} className="mt-8 space-y-5">
          {isRegister && (
            <label className="block text-sm font-medium text-luma-primary">
              Name
              <input
                autoComplete="name"
                required
                maxLength={100}
                value={name}
                onChange={(event) => setName(event.target.value)}
                className="mt-2 w-full rounded-xl border border-luma-primary/15 bg-luma-bg/80 px-4 py-3 outline-none focus:border-luma-accent"
              />
            </label>
          )}
          <label className="block text-sm font-medium text-luma-primary">
            Email
            <input
              type="email"
              autoComplete="email"
              required
              maxLength={254}
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="mt-2 w-full rounded-xl border border-luma-primary/15 bg-luma-bg/80 px-4 py-3 outline-none focus:border-luma-accent"
            />
          </label>
          <label className="block text-sm font-medium text-luma-primary">
            Password
            <input
              type="password"
              autoComplete={isRegister ? "new-password" : "current-password"}
              required
              minLength={isRegister ? 12 : undefined}
              maxLength={128}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="mt-2 w-full rounded-xl border border-luma-primary/15 bg-luma-bg/80 px-4 py-3 outline-none focus:border-luma-accent"
            />
            {isRegister && (
              <span className="mt-1 block text-xs font-normal text-luma-text-secondary">
                Use at least 12 characters.
              </span>
            )}
          </label>

          {error && (
            <p role="alert" className="rounded-xl border border-rose-300/50 bg-rose-100/60 px-4 py-3 text-sm text-rose-900">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={isSubmitting || isDemoSubmitting}
            className="w-full rounded-full bg-luma-primary px-6 py-3.5 font-medium text-luma-cream transition hover:opacity-90 disabled:opacity-60"
          >
            {isSubmitting
              ? "One moment…"
              : isRegister
                ? "Create account"
                : "Log in"}
          </button>

          <div className="relative my-2 flex items-center justify-center">
            <span className="absolute inset-x-0 h-px bg-luma-primary/10" />
            <span className="relative bg-luma-bg px-3 text-[11px] uppercase tracking-wider text-luma-text-secondary">
              or explore immediately
            </span>
          </div>

          <button
            type="button"
            onClick={handleDemoLogin}
            disabled={isSubmitting || isDemoSubmitting}
            className="w-full rounded-full border border-luma-accent/35 bg-luma-peach/20 px-6 py-3.5 font-medium text-luma-primary transition hover:bg-luma-peach/30 hover:border-luma-accent shadow-sm disabled:opacity-60 flex items-center justify-center gap-2"
          >
            {isDemoSubmitting ? (
              <span>Preparing demo world…</span>
            ) : (
              <>
                <span className="text-luma-accent">✦</span>
                <span>Demo Hesabıyla Başla / Try Demo</span>
              </>
            )}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-luma-text-secondary">
          {isRegister ? "Already have an account?" : "New to LUMA?"}{" "}
          <Link
            href={`${isRegister ? "/login" : "/register"}${searchParams.get("returnUrl") ? `?returnUrl=${encodeURIComponent(searchParams.get("returnUrl")!)}` : ""}`}
            className="font-medium text-luma-primary underline underline-offset-4"
          >
            {isRegister ? "Log in" : "Create an account"}
          </Link>
        </p>
      </section>
    </main>
  );
}
