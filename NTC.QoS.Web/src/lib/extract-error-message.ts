export function extractErrorMessage(error: unknown): string {
  let message = "An error occurred";

  // extract error message from error response
  if (error && typeof error === "object") {
    // Prefer error.response.message if present
    if ("response" in error && error.response && typeof error.response === "object" && "message" in error.response) {
      message = (error.response as { message?: string }).message ?? message;
    } else if ("message" in error && typeof (error as { message?: unknown }).message === "string") {
      message = (error as { message: string }).message;
    }
  } else if (typeof error === "string") {
    message = error;
  }

  // Sanitize raw XML responses (e.g. from Azure Blob Storage or S3)
  if (message.includes("<?xml") || message.includes("<Error>") || message.includes("AuthorizationFailure")) {
    return "Upload failed: Access permission expired or denied. Please refresh the page and try again.";
  }

  return message;
}
