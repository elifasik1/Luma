// =============================================================================
// LUMA — API Proxy: /api/dreams/{dreamId}/reflections
// Forwards POST and GET to the ASP.NET Core backend.
// =============================================================================

import { NextResponse } from "next/server";
import { authenticatedBackendFetch, sameOriginFailure } from "@/lib/server/authProxy";

export async function GET(
  request: Request,
  { params }: { params: Promise<{ dreamId: string }> }
) {
  try {
    const { dreamId } = await params;

    const backendResponse = await authenticatedBackendFetch(request, `/dreams/${dreamId}/reflections`);

    if (backendResponse.status === 404) {
      return NextResponse.json(
        { error: "Dream not found." },
        { status: 404 }
      );
    }

    const data = await backendResponse.json();

    return NextResponse.json(data, { status: backendResponse.status });
  } catch (error) {
    console.error("[LUMA API Proxy] GET /dreams/{id}/reflections failed:", error);

    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}

export async function POST(
  request: Request,
  { params }: { params: Promise<{ dreamId: string }> }
) {
  const originFailure = sameOriginFailure(request);
  if (originFailure) return originFailure;

  try {
    const { dreamId } = await params;
    const body = await request.json();

    const backendResponse = await authenticatedBackendFetch(
      request,
      `/dreams/${dreamId}/reflections`,
      {
        method: "POST",
        body: JSON.stringify(body),
      }
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
    console.error("[LUMA API Proxy] POST /dreams/{id}/reflections failed:", error);

    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
