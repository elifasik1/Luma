import { NextResponse } from "next/server";
import { authenticatedBackendFetch, sameOriginFailure } from "@/lib/server/authProxy";

export async function PUT(
  request: Request,
  { params }: { params: Promise<{ dreamId: string; reflectionId: string }> }
) {
  const originFailure = sameOriginFailure(request);
  if (originFailure) return originFailure;

  try {
    const { dreamId, reflectionId } = await params;
    const backendResponse = await authenticatedBackendFetch(
      request,
      `/dreams/${dreamId}/reflections/${reflectionId}`,
      { method: "PUT", body: await request.text() }
    );
    const body = await backendResponse.text();
    return new NextResponse(body || null, {
      status: backendResponse.status,
      headers: { "Content-Type": backendResponse.headers.get("Content-Type") ?? "application/json" },
    });
  } catch (error) {
    console.error("[LUMA API Proxy] PUT reflection failed:", error);
    return NextResponse.json({ error: "Failed to update reflection." }, { status: 502 });
  }
}
