import { NextResponse } from "next/server";
import { authenticatedBackendFetch, relayJson } from "@/lib/server/authProxy";

export async function GET(
  request: Request,
  { params }: { params: Promise<{ dreamId: string }> }
) {
  try {
    const { dreamId } = await params;
    const url = new URL(request.url);
    const query = new URLSearchParams();
    const year = url.searchParams.get("year");
    const month = url.searchParams.get("month");
    if (year) query.set("year", year);
    if (month) query.set("month", month);

    const backendResponse = await authenticatedBackendFetch(
      request,
      `/dreams/${dreamId}/journey-calendar?${query.toString()}`
    );
    return relayJson(backendResponse);
  } catch (error) {
    console.error("[LUMA API Proxy] GET /dreams/{id}/journey-calendar failed:", error);
    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
