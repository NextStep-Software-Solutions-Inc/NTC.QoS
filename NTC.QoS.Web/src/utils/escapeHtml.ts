/**
 * Escape HTML special characters to prevent XSS when interpolating
 * user-supplied values into raw HTML strings (e.g., map popup content).
 */
export function escapeHtml(str: string | undefined | null): string {
  if (!str) return "";
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}
