"use client";

import { useEffect, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";

export function DiscoverAuthGate({ children }: { children: ReactNode }) {
  const router = useRouter();
  const [isChecking, setIsChecking] = useState(true);

  useEffect(() => {
    let active = true;

    fetch("/api/auth/me", { cache: "no-store" })
      .then((response) => {
        if (!active) return;
        if (response.ok) {
          setIsChecking(false);
          return;
        }
        router.replace("/login?returnUrl=%2Fdiscover");
      })
      .catch(() => {
        if (active) router.replace("/login?returnUrl=%2Fdiscover");
      });

    return () => {
      active = false;
    };
  }, [router]);

  if (isChecking) {
    return (
      <div className="flex min-h-[70vh] items-center justify-center px-5 text-center">
        <p className="font-serif text-2xl text-luma-primary">Making space for your future…</p>
      </div>
    );
  }

  return children;
}
