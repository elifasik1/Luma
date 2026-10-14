import { NextResponse } from "next/server";
import { authenticatedBackendFetch, relayJson } from "@/lib/server/authProxy";

export async function GET(
  request: Request,
  { params }: { params: Promise<{ dreamId: string; planDate: string }> }
) {
  try {
    const { dreamId, planDate } = await params;
    const backendResponse = await authenticatedBackendFetch(
      request,
      `/dreams/${dreamId}/journey-calendar/${encodeURIComponent(planDate)}`
    );
    return relayJson(backendResponse);
  } catch (error) {
    console.error("[LUMA API Proxy] GET /dreams/{id}/journey-calendar/{date} failed:", error);
    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
