import { cookies } from "next/headers";
import { NextResponse } from "next/server";

export const AUTH_COOKIE_NAME = "luma_access_token";
export const BACKEND_URL = process.env.BACKEND_URL ?? "http://localhost:5192";

export function sameOriginFailure(request: Request): NextResponse | null {
  const requestOrigin = new URL(request.url).origin;
  const originHeader = request.headers.get("origin");
  const refererHeader = request.headers.get("referer");

  try {
    const sourceOrigin = originHeader
      ? new URL(originHeader).origin
      : refererHeader
        ? new URL(refererHeader).origin
        : null;
    if (sourceOrigin === requestOrigin) return null;
  } catch {
    // An invalid Origin or Referer is rejected below.
  }

  return NextResponse.json({ error: "Invalid request origin." }, { status: 403 });
}

export async function authenticatedBackendFetch(
  request: Request,
  path: string,
  init: RequestInit = {}
): Promise<Response> {
  const cookieStore = await cookies();
  const token = cookieStore.get(AUTH_COOKIE_NAME)?.value;
    const headers = new Headers(init.headers);

  if (token) headers.set("Authorization", `Bearer ${token}`);
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  return fetch(`${BACKEND_URL}${path}`, {
    ...init,
    headers,
    cache: "no-store",
  });
}

export async function relayJson(response: Response): Promise<NextResponse> {
  const body = await response.text();
  if (!body) return new NextResponse(null, { status: response.status });

  return new NextResponse(body, {
    status: response.status,
    headers: {
      "Content-Type": response.headers.get("Content-Type") ?? "application/json",
    },
  });
}
