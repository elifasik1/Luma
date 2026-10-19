import { useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useLumaTheme } from "@/theme/ThemeProvider";
import {
  normalizeAtmosphere,
  normalizeIllustration,
  normalizeMotion,
  normalizePalette,
} from "@/theme/normalize";
import type { PaletteKey } from "@/theme/tokens";
import { getUserTheme } from "@/lib/api";

interface AuthFormProps {
  mode: "login" | "register";
}

const REGISTRATION_THEMES: Array<{ key: PaletteKey; label: string; swatch: string }> = [
  { key: "warm_dream", label: "Warm Dream", swatch: "#e88062" },
  { key: "forest_mist", label: "Forest Mist", swatch: "#3c6e5a" },
  { key: "city_night", label: "City Night", swatch: "#24324a" },
  { key: "ocean_light", label: "Ocean Light", swatch: "#3e8c9e" },
  { key: "lavender_milk", label: "Lavender Milk", swatch: "#9683b5" },
];

export function AuthForm({ mode }: AuthFormProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { setTheme } = useLumaTheme();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [selectedPalette, setSelectedPalette] = useState<PaletteKey>("warm_dream");
  const [hasSelectedPalette, setHasSelectedPalette] = useState(false);
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

      if (data?.user?.themePreference) {
        await setTheme(
          { palette: normalizePalette(data.user.themePreference) },
          { persist: false }
        );
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
      localStorage.removeItem("luma_theme_palette");
      localStorage.removeItem("luma_theme_is_explicit");
    } catch {
      // ignore
    }
    try {
      const response = await fetch(`/api/auth/${mode}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(
          isRegister
             ? {
                 name,
                 email,
                 password,
                 themePreference: selectedPalette,
                 themePreferenceIsExplicit: hasSelectedPalette,
               }
            : { email, password }
        ),
      });
      const result = await response.json();
      if (!response.ok) {
        setError(result.error ?? "We couldn't complete your request.");
        return;
      }

      if (isRegister) {
        // Registration stores the selected palette. Keep the initial choice
        // marked as a suggestion unless the user explicitly clicked a theme;
        // onboarding may then refine the first visual recommendation.
        await setTheme(
          { palette: selectedPalette },
          { persist: false, markExplicit: hasSelectedPalette }
        );
      } else {
        const savedTheme = await getUserTheme();
        if (savedTheme) {
          await setTheme(
            {
              palette: normalizePalette(savedTheme.palette),
              atmosphere: normalizeAtmosphere(savedTheme.atmosphere),
              illustration: normalizeIllustration(savedTheme.illustration),
              motion: normalizeMotion(savedTheme.motion),
            },
            { persist: false, markExplicit: savedTheme.isExplicit ?? false }
          );
        } else if (result?.themePreference) {
          await setTheme(
            { palette: normalizePalette(result.themePreference) },
            { persist: false, markExplicit: true }
          );
        }
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

          {isRegister && (
            <div className="pt-1">
              <span className="block text-xs font-semibold uppercase tracking-wider text-luma-text-secondary mb-2.5">
                Choose your space aesthetic
              </span>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                {REGISTRATION_THEMES.map(({ key, label, swatch }) => (
                  <button
                    key={key}
                    type="button"
                     onClick={() => {
                       setSelectedPalette(key);
                       setHasSelectedPalette(true);
                     }}
                    className={`flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-medium border transition-all text-left ${
                      selectedPalette === key
                        ? "border-luma-primary bg-luma-primary/10 text-luma-primary ring-1 ring-luma-primary"
                        : "border-luma-primary/15 bg-luma-bg/50 text-luma-text-secondary hover:border-luma-primary/30"
                    }`}
                  >
                    <span
                      className="w-3 h-3 rounded-full flex-shrink-0"
                      style={{ backgroundColor: swatch }}
                      aria-hidden="true"
                    />
                    <span className="truncate">{label}</span>
                  </button>
                ))}
              </div>
            </div>
          )}

          {error && (
            <p role="alert" className="rounded-xl border border-rose-300/50 bg-rose-100/60 px-4 py-3 text-sm text-rose-900">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={isSubmitting || isDemoSubmitting}
            className="w-full rounded-full bg-luma-primary px-6 py-3.5 font-medium text-luma-bg transition hover:opacity-90 disabled:opacity-60"
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
            className="w-full rounded-full border border-luma-accent/35 bg-luma-accent/20 px-6 py-3.5 font-medium text-luma-primary transition hover:bg-luma-accent/30 hover:border-luma-accent shadow-sm disabled:opacity-60 flex items-center justify-center gap-2"
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
