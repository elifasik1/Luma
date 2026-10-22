"use client";

import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { getLatestDream } from "@/lib/api";

/** Pages where the FAB should NOT appear */
const HIDDEN_PATHS = ["/", "/login", "/register", "/talk"];

/**
 * Floating "Talk to LUMA" button – renders on authenticated app pages.
 * Resolves the latest dream ID lazily so it links directly to the conversation.
 */
export function TalkFab() {
  const pathname = usePathname();
  const router = useRouter();
  const [dreamId, setDreamId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  // Hide on public / auth / talk pages
  const hidden = HIDDEN_PATHS.some((p) => pathname === p || pathname.startsWith(p + "/"));
  // Also hide on discover (unauthenticated onboarding)
  const isDiscover = pathname.startsWith("/discover");

  useEffect(() => {
    if (hidden || isDiscover) return;
    let active = true;
    getLatestDream()
      .then((d) => { if (active && d?.id) setDreamId(d.id); })
      .catch(() => {});
    return () => { active = false; };
  }, [hidden, isDiscover, pathname]);

  if (hidden || isDiscover) return null;

  const handleClick = () => {
    if (loading) return;
    if (dreamId) {
      router.push(`/talk?id=${encodeURIComponent(dreamId)}`);
    } else {
      // No dream loaded – might be unauthenticated or still loading
      setLoading(true);
      getLatestDream()
        .then((d) => {
          if (d?.id) {
            setDreamId(d.id);
            router.push(`/talk?id=${encodeURIComponent(d.id)}`);
          } else {
            router.push("/login?returnUrl=/talk");
          }
        })
        .catch(() => {
          router.push("/login?returnUrl=/talk");
        })
        .finally(() => setLoading(false));
    }
  };

  return (
    <button
      type="button"
      onClick={handleClick}
      disabled={loading}
      aria-label="Talk to LUMA"
      title="Talk to LUMA"
      className="luma-talk-fab"
    >
      <span className="luma-talk-fab__icon" aria-hidden="true">✦</span>
      <span className="luma-talk-fab__label">Talk</span>
    </button>
  );
}
