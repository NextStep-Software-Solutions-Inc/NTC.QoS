/**
 * Get the backend API URL for client-side requests
 * Uses NEXT_PUBLIC_API_URL as the single source of truth
 */
export function getBackendUrl(): string {
  if (typeof window !== "undefined") {
    const envUrl = process.env.NEXT_PUBLIC_API_URL;
    if (envUrl) {
      return envUrl;
    }
    return window.location.origin;
  }
  return process.env.NEXT_PUBLIC_API_URL ?? "";
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
 * Get authenticated headers for fetch requests
 * Includes Authorization header with Bearer token if available
 */
export function getAuthHeaders(): HeadersInit {
  const token = getSessionToken();
  return { ...(token ? { Authorization: `Bearer ${token}` } : {}) };
}
