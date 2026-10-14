import { NextResponse } from "next/server";
import { authenticatedBackendFetch, relayJson } from "@/lib/server/authProxy";

export async function GET(request: Request) {
  try {
    const backendResponse = await authenticatedBackendFetch(request, "/auth/me");
    return await relayJson(backendResponse);
  } catch (error) {
    console.error("[LUMA API Proxy] GET /auth/me failed:", error);
    return NextResponse.json({ error: "Failed to connect to LUMA backend." }, { status: 502 });
  }
}
