import { NextResponse } from "next/server";
import {
  authenticatedBackendFetch,
  relayJson,
  sameOriginFailure,
} from "@/lib/server/authProxy";

export async function GET(request: Request) {
  try {
    const backendResponse = await authenticatedBackendFetch(request, "/auth/theme");
    return await relayJson(backendResponse);
  } catch (error) {
    console.error("[LUMA API Proxy] GET /auth/theme failed:", error);
    return NextResponse.json(
      { error: "Failed to connect to LUMA backend." },
      { status: 502 }
    );
  }
}

export async function PUT(request: Request) {
  const originFailure = sameOriginFailure(request);
  if (originFailure) return originFailure;

  try {
    const body = await request.text();
    const backendResponse = await authenticatedBackendFetch(request, "/auth/theme", {
      method: "PUT",
      body,
    });
    return await relayJson(backendResponse);
  } catch (error) {
    console.error("[LUMA API Proxy] PUT /auth/theme failed:", error);
    return NextResponse.json(
      { error: "Failed to update theme on LUMA backend." },
      { status: 502 }
    );
  }
}
