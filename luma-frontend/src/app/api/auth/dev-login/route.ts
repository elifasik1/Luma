import { NextResponse } from "next/server";
import { BACKEND_URL } from "@/lib/server/authProxy";

export async function POST() {
  if (process.env.NODE_ENV === "production" && process.env.LUMA_DEMO_ENABLED !== "true") {
    return NextResponse.json({ error: "Development login is disabled." }, { status: 404 });
  }

  try {
    const backendResponse = await fetch(`${BACKEND_URL}/auth/dev-token`, {
      cache: "no-store",
    });
    const result = await backendResponse.json();
    if (!backendResponse.ok) {
      return NextResponse.json(result, { status: backendResponse.status });
    }

    const response = NextResponse.json(
      { user: result.user, dreamId: result.dreamId },
      { status: 200 }
    );
    response.cookies.set("luma_access_token", result.accessToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      expires: new Date(result.expiresAt),
    });
    return response;
  } catch (error) {
    console.error("[LUMA Dev Login] Failed:", error);
    return NextResponse.json({ error: "Failed to create dev session." }, { status: 500 });
  }
}
