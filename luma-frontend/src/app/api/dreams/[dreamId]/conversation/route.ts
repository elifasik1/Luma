import { NextResponse } from "next/server";
import { authenticatedBackendFetch, relayJson, sameOriginFailure } from "@/lib/server/authProxy";

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
      `/dreams/${encodeURIComponent(dreamId)}/conversation`,
      {
        method: "POST",
        body: JSON.stringify(body),
      }
    );

    return relayJson(backendResponse);
  } catch (error) {
    console.error("[LUMA API Proxy] POST /dreams/{id}/conversation failed:", error);
    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
