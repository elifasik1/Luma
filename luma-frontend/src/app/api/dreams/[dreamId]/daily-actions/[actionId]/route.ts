// =============================================================================
// LUMA — API Proxy: PATCH /api/dreams/{dreamId}/daily-actions/{actionId}
// Forwards to the ASP.NET Core backend.
// =============================================================================

import { NextResponse } from "next/server";
import { authenticatedBackendFetch, sameOriginFailure } from "@/lib/server/authProxy";

export async function PATCH(
  request: Request,
  { params }: { params: Promise<{ dreamId: string; actionId: string }> }
) {
  const originFailure = sameOriginFailure(request);
  if (originFailure) return originFailure;

  try {
    const { dreamId, actionId } = await params;
    const url = new URL(request.url);
    const planDate = url.searchParams.get("planDate");
    const queryString = planDate ? `?planDate=${encodeURIComponent(planDate)}` : "";
    const body = await request.json();

    const backendResponse = await authenticatedBackendFetch(
      request,
      `/dreams/${dreamId}/daily-actions/${actionId}${queryString}`,
      {
        method: "PATCH",
        body: JSON.stringify(body),
      }
    );

    if (backendResponse.status === 404) {
      return NextResponse.json(
        { error: "Daily action or dream not found." },
        { status: 404 }
      );
    }

    const data = await backendResponse.json();

    return NextResponse.json(data, { status: backendResponse.status });
  } catch (error) {
    console.error(
      "[LUMA API Proxy] PATCH /dreams/{id}/daily-actions/{actionId} failed:",
      error
    );

    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
