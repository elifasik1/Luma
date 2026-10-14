// =============================================================================
// LUMA — API Proxy: GET /api/dreams/{dreamId}/daily-path
// Forwards to the ASP.NET Core backend.
// =============================================================================

import { NextResponse } from "next/server";
import { authenticatedBackendFetch } from "@/lib/server/authProxy";

export async function GET(
  request: Request,
  { params }: { params: Promise<{ dreamId: string }> }
) {
  try {
    const { dreamId } = await params;
    const url = new URL(request.url);
    const refresh = url.searchParams.get("refresh");
    const queryString = refresh === "true" ? "?refresh=true" : "";

    const backendResponse = await authenticatedBackendFetch(
      request,
      `/dreams/${dreamId}/daily-path${queryString}`
    );

    if (backendResponse.status === 404) {
      return NextResponse.json(
        { error: "Dream not found." },
        { status: 404 }
      );
    }

    const data = await backendResponse.json();

    return NextResponse.json(data, { status: backendResponse.status });
  } catch (error) {
    console.error("[LUMA API Proxy] GET /dreams/{id}/daily-path failed:", error);

    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
