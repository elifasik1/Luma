import { NextResponse } from "next/server";
import { authenticatedBackendFetch, sameOriginFailure } from "@/lib/server/authProxy";

export async function GET(request: Request) {
  const originFailure = sameOriginFailure(request);
  if (originFailure) return originFailure;

  try {
    const backendResponse = await authenticatedBackendFetch(request, "/dreams/latest", {
      method: "GET",
    });

    const data = await backendResponse.json();
    return NextResponse.json(data, { status: backendResponse.status });
  } catch (error) {
    console.error("[LUMA API Proxy] GET /dreams/latest failed:", error);
    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}
