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
    console.log(`[FRONTEND_AGENT_PROXY] dreamId=${dreamId} body=${JSON.stringify(body)}`);
    const backendResponse = await authenticatedBackendFetch(
      request,
      `/agent/${encodeURIComponent(dreamId)}/message`,
      {
        method: "POST",
        body: JSON.stringify(body),
      }
    );

    return relayJson(backendResponse);
  } catch (error) {
    console.error("[LUMA API Proxy] POST /agent/{id}/message failed:", error);
    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
