// =============================================================================
// LUMA — API Proxy: POST /api/dreams
// Forwards to the ASP.NET Core backend. Solves CORS without modifying backend.
// BACKEND_URL is server-only (no NEXT_PUBLIC_ prefix).
// =============================================================================

import { NextResponse } from "next/server";
import { authenticatedBackendFetch, sameOriginFailure } from "@/lib/server/authProxy";

export async function POST(request: Request) {
  const originFailure = sameOriginFailure(request);
  if (originFailure) return originFailure;

  try {
    const body = await request.json();

    const backendResponse = await authenticatedBackendFetch(request, "/dreams", {
      method: "POST",
      body: JSON.stringify(body),
    });

    const data = await backendResponse.json();

    return NextResponse.json(data, { status: backendResponse.status });
  } catch (error) {
    console.error("[LUMA API Proxy] POST /dreams failed:", error);

    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
