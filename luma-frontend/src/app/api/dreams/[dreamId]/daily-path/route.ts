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
    const planDate = url.searchParams.get("planDate");
    const searchParams = new URLSearchParams();
    if (refresh === "true") searchParams.set("refresh", "true");
    if (planDate) searchParams.set("planDate", planDate);
    const queryString = searchParams.toString() ? `?${searchParams.toString()}` : "";

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

    const body = await backendResponse.text();
    return new NextResponse(body || null, {
      status: backendResponse.status,
      headers: {
        "Content-Type": backendResponse.headers.get("Content-Type") ?? "application/json",
      },
    });
  } catch (error) {
    console.error("[LUMA API Proxy] GET /dreams/{id}/daily-path failed:", error);

    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
