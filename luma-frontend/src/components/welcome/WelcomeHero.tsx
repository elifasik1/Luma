"use client";

import Link from "next/link";

export function WelcomeHero() {
  return (
    <section className="relative isolate mx-auto flex min-h-[calc(100svh-4rem)] w-full max-w-7xl flex-col overflow-x-clip px-6 py-8 sm:px-10 lg:px-14">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute left-1/2 top-12 -z-10 h-72 w-72 -translate-x-1/2 rounded-full bg-luma-peach/15 blur-3xl sm:h-96 sm:w-96 lg:left-[68%] lg:top-20"
      />

      <header className="flex items-center justify-between">
        <Link
          href="/"
          className="font-serif text-2xl tracking-[0.16em] text-luma-primary"
          aria-label="LUMA home"
        >
          LUMA
        </Link>
        <div className="flex items-center gap-4 sm:gap-6">
          <Link
            href="/login"
            className="text-xs sm:text-sm font-medium text-luma-primary/80 transition-opacity hover:opacity-100 hover:text-luma-primary flex items-center gap-1.5"
          >
            <span>Log in / Demo</span>
            <span aria-hidden="true" className="text-luma-accent">✦</span>
          </Link>
        </div>
      </header>

      <div className="grid flex-1 items-center gap-8 py-12 md:grid-cols-[1.05fr_0.95fr] md:gap-12 lg:gap-20">
        <div className="animate-luma-fade-in">
          <p className="mb-6 flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.2em] text-luma-text-secondary sm:text-sm">
            <span aria-hidden="true" className="text-luma-accent">✦</span>
            A little space to imagine
          </p>

          <h1 className="max-w-2xl font-serif text-5xl leading-[1.08] tracking-tight text-luma-primary sm:text-6xl lg:text-7xl">
            See the life you want.
            <span className="mt-2 block text-luma-accent">Build it.</span>
          </h1>

          <div className="mt-8 max-w-lg space-y-3 text-lg leading-relaxed text-luma-text-secondary sm:mt-10 sm:text-xl">
            <p>You don&apos;t have to know exactly where you&apos;re going yet.</p>
            <p className="font-serif text-2xl italic text-luma-primary sm:text-3xl">
              Let&apos;s imagine it.
            </p>
          </div>

          <div className="mt-9 flex flex-wrap items-center gap-4 sm:mt-11">
            <Link
              href="/discover"
              className="group inline-flex items-center gap-3 rounded-full bg-luma-primary px-7 py-4 text-base font-medium tracking-wide text-luma-cream shadow-[0_12px_30px_rgba(75,46,77,0.14)] transition duration-300 hover:-translate-y-0.5 hover:shadow-[0_16px_36px_rgba(75,46,77,0.2)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-luma-accent focus-visible:ring-offset-2 sm:px-8 sm:text-lg"
            >
              Start imagining
              <span aria-hidden="true" className="transition-transform duration-300 group-hover:translate-x-1">
                →
              </span>
            </Link>

            <Link
              href="/login"
              className="inline-flex items-center gap-2 rounded-full border border-luma-primary/15 bg-white/60 px-6 py-4 text-base font-medium text-luma-primary transition duration-300 hover:bg-white hover:border-luma-accent/50 shadow-sm backdrop-blur-sm sm:text-lg"
            >
              <span className="text-luma-accent">✦</span>
              <span>Try Demo</span>
            </Link>
          </div>

          <p className="mt-6 text-sm text-luma-text-secondary/80">
            No pressure. Just a quiet moment for you.
          </p>
        </div>

        <div
          aria-hidden="true"
          className="relative mx-auto aspect-[4/3] w-full max-w-xl animate-luma-fade-in"
        >
          <div className="absolute inset-[8%] rounded-[48%] bg-gradient-to-br from-luma-peach/30 via-luma-surface/35 to-luma-muted/40 blur-2xl" />
          <div className="absolute inset-[8%] overflow-hidden rounded-[48%] border border-white/70 bg-gradient-to-br from-white/65 via-luma-bg-secondary/65 to-luma-surface/35 shadow-[0_28px_90px_rgba(75,46,77,0.09)]">
            <div className="absolute left-[17%] top-[16%] h-[42%] aspect-square rounded-full bg-luma-peach/65 shadow-[0_0_70px_rgba(247,166,149,0.45)]" />
            <div className="absolute left-[12%] top-[37%] h-px w-[76%] bg-luma-primary/15" />
            <div className="absolute -bottom-[38%] -left-[12%] h-[86%] w-[82%] rotate-[-12deg] rounded-[50%] bg-luma-surface/70" />
            <div className="absolute -bottom-[43%] -right-[14%] h-[88%] w-[82%] rotate-[12deg] rounded-[50%] bg-luma-muted/75" />
            <div className="absolute bottom-[20%] left-[24%] h-2 w-2 rounded-full bg-white/90 shadow-[0_0_24px_8px_rgba(255,255,255,0.7)]" />
            <span className="absolute right-[22%] top-[23%] font-serif text-2xl text-luma-accent/80">
              ✦
            </span>
            <span className="absolute right-[31%] top-[37%] text-sm text-luma-primary/45">
              ✧
            </span>
          </div>
          <div className="absolute bottom-[13%] left-[2%] rounded-full border border-white/80 bg-white/75 px-4 py-2 text-xs tracking-wide text-luma-text-secondary shadow-sm backdrop-blur-sm sm:left-[4%] sm:px-5 sm:py-2.5 sm:text-sm">
            A future, taking shape
          </div>
        </div>
      </div>

      <p className="pb-2 text-center text-xs tracking-[0.16em] text-luma-text-secondary/60 sm:text-left">
        YOUR NEXT CHAPTER STARTS WITH A POSSIBILITY
      </p>
    </section>
  );
}
