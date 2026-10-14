import { NextResponse } from "next/server";
import { BACKEND_URL, sameOriginFailure } from "@/lib/server/authProxy";

export async function POST(request: Request) {
  const originFailure = sameOriginFailure(request);
  if (originFailure) return originFailure;

  try {
    const backendResponse = await fetch(`${BACKEND_URL}/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: await request.text(),
      cache: "no-store",
    });
    const result = await backendResponse.json();
    if (!backendResponse.ok) {
      return NextResponse.json(result, { status: backendResponse.status });
    }

    const response = NextResponse.json(result.user, { status: backendResponse.status });
    response.cookies.set("luma_access_token", result.accessToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      expires: new Date(result.expiresAt),
    });
    return response;
  } catch (error) {
    console.error("[LUMA API Proxy] POST /auth/login failed:", error);
    return NextResponse.json({ error: "Failed to connect to LUMA backend." }, { status: 502 });
  }
}
