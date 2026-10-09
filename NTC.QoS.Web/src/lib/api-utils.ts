/**
 * Get the backend API URL for client-side requests.
 * Reads process.env.NEXT_PUBLIC_API_URL and throws an explicit error if not configured.
 */
export function getBackendUrl(): string {
  const envUrl = process.env.NEXT_PUBLIC_API_URL || process.env.NEXT_PUBLIC_QOS_BACKEND_URL;
  
  if (envUrl) {
    return envUrl.endsWith("/") ? envUrl.slice(0, -1) : envUrl;
  }

  // In browser environment fallback to window.location.origin only if explicitly in client window
  if (typeof window !== "undefined" && window.location?.origin) {
    return window.location.origin;
  }

  throw new Error("Missing required environment variable: NEXT_PUBLIC_API_URL");
}

/**
 * Get the session token from cookies
 * Used for authentication in API requests
 */
export function getSessionToken(): string | null {
  if (typeof document === "undefined") {
    return null;
  }
  const sessionCookie = document.cookie.split(";").find((c) => c.trim().startsWith("__session"));

  if (sessionCookie) {
    return sessionCookie.split("=")[1] ?? null;
  }
  return null;
}

/**
 * Asynchronously retrieve the session token.
 * First checks window.Clerk.session.getToken() for fresh client-side Clerk JWT,
 * then falls back to document.cookie (__session).
 */
export async function getAuthToken(): Promise<string | null> {
  if (typeof window !== "undefined") {
    const clerk = (window as unknown as { Clerk?: { session?: { getToken: () => Promise<string | null> } } }).Clerk;
    if (clerk?.session) {
      try {
        const token = await clerk.session.getToken();
        if (token) return token;
      } catch {
        // Fallback to cookie
      }
    }
  }

  return getSessionToken();
}

/**
 * Get authenticated headers for fetch requests (synchronous cookie-based fallback)
 * Includes Authorization header with Bearer token if available
 */
export function getAuthHeaders(): HeadersInit {
  const token = getSessionToken();
  return { ...(token ? { Authorization: `Bearer ${token}` } : {}) };
}

/**
 * Asynchronously retrieve authenticated headers for fetch requests.
 * Uses window.Clerk.session.getToken() to guarantee a fresh JWT Bearer token,
 * falling back to document.cookie.
 */
export async function getAuthHeadersAsync(): Promise<Record<string, string>> {
  const token = await getAuthToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}
