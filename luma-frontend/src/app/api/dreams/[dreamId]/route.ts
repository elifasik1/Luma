// =============================================================================
// LUMA — API Proxy: GET /api/dreams/{dreamId}
// Forwards to the ASP.NET Core backend. Solves CORS without modifying backend.
// BACKEND_URL is server-only (no NEXT_PUBLIC_ prefix).
// =============================================================================

import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/server/authProxy";

export async function GET(
  request: Request,
  { params }: { params: Promise<{ dreamId: string }> }
) {
  try {
    const { dreamId } = await params;

    const backendResponse = await authenticatedBackendFetch(request, `/dreams/${dreamId}`);

    if (backendResponse.status === 404) {
      return NextResponse.json(
        { error: "Dream not found." },
        { status: 404 }
      );
    }

    const data = await backendResponse.json();

    return NextResponse.json(data, { status: backendResponse.status });
  } catch (error) {
    console.error("[LUMA API Proxy] GET /dreams/{id} failed:", error);

    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
