import { NextResponse } from "next/server";
import { authenticatedBackendFetch, sameOriginFailure } from "@/lib/server/authProxy";

export async function PUT(
  request: Request,
  context: { params: Promise<{ dreamId: string }> }
) {
  const originFailure = sameOriginFailure(request);
  if (originFailure) return originFailure;

  try {
    const { dreamId } = await context.params;
    const body = await request.json();
    const backendResponse = await authenticatedBackendFetch(request, `/dreams/${encodeURIComponent(dreamId)}/target-date`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
    const data = await backendResponse.json();
    return NextResponse.json(data, { status: backendResponse.status });
  } catch (error) {
    console.error("[LUMA API Proxy] PUT /dreams/:id/target-date failed:", error);
    return NextResponse.json({ error: "Failed to connect to LUMA backend." }, { status: 502 });
  }
}
