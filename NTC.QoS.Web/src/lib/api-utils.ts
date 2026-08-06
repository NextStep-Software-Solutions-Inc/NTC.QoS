/**
 * Get the backend API URL for client-side requests
 * Checks for NEXT_PUBLIC_QOS_BACKEND_URL first, then falls back to window.location.origin
 */
export function getBackendUrl(): string {
  // In browser environment
  if (typeof window !== "undefined") {
    // Try to get from environment variable first
    const envUrl = process.env.NEXT_PUBLIC_QOS_BACKEND_URL;
    if (envUrl) {
      return envUrl;
    }
    // Fallback to current origin (assumes API is proxied)
    return window.location.origin;
  }
  // Server-side fallback
  return process.env.QOS_BACKEND_URL ?? process.env.NEXT_PUBLIC_QOS_BACKEND_URL ?? "";
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
