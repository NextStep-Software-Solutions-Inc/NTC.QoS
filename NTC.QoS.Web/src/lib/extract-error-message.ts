/**
 * Helper to safely extract messages from an `errors` map or array.
 * Supports:
 * - ASP.NET ValidationProblemDetails: { File: ["The File field is required."], Type: ["The Type field is required."] }
 * - Arrays: ["First error", "Second error"]
 * - Key-value strings: { email: "Invalid email" }
 */
function extractFromErrorsContainer(errors: unknown): string[] {
  const messages: string[] = [];

  if (!errors) return messages;

  if (Array.isArray(errors)) {
    for (const item of errors) {
      if (typeof item === "string" && item.trim()) {
        messages.push(item.trim());
      } else if (item && typeof item === "object") {
        messages.push(...extractFromErrorsContainer(item));
      }
    }
  } else if (typeof errors === "object") {
    for (const key of Object.keys(errors)) {
      const val = (errors as Record<string, unknown>)[key];
      if (typeof val === "string" && val.trim()) {
        messages.push(val.trim());
      } else if (Array.isArray(val)) {
        for (const subItem of val) {
          if (typeof subItem === "string" && subItem.trim()) {
            messages.push(subItem.trim());
          }
        }
      } else if (val && typeof val === "object") {
        messages.push(...extractFromErrorsContainer(val));
      }
    }
  }

  return messages;
}

function formatErrorMessages(messages: string[]): string {
  if (messages.length === 0) return "";
  if (messages.length === 1) return messages[0]!;

  return messages
    .map((msg) => (msg.endsWith(".") || msg.endsWith("!") || msg.endsWith("?") ? msg : `${msg}.`))
    .join(" ");
}

/**
 * Safely parses a JSON string if formatted as an object or array.
 */
function tryParseJson(str: string): unknown | null {
  const trimmed = str.trim();
  if (
    (trimmed.startsWith("{") && trimmed.endsWith("}")) ||
    (trimmed.startsWith("[") && trimmed.endsWith("]"))
  ) {
    try {
      return JSON.parse(trimmed);
    } catch {
      return null;
    }
  }
  return null;
}

/**
 * Extracts human-readable message from a parsed error object.
 */
function extractFromErrorObject(obj: Record<string, unknown>): string | null {
  // 1. Check for RFC 7807/9110 / FluentValidation `errors` dictionary
  if ("errors" in obj && obj.errors) {
    const errorMessages = extractFromErrorsContainer(obj.errors);
    if (errorMessages.length > 0) {
      return formatErrorMessages(errorMessages);
    }
  }

  // 2. Check for `detail` (RFC 7807/9110 Problem Details)
  if ("detail" in obj && typeof obj.detail === "string" && obj.detail.trim()) {
    return obj.detail.trim();
  }

  // 3. Check for `message` (standard ApiResponse or error object)
  if ("message" in obj && typeof obj.message === "string" && obj.message.trim()) {
    const msg = obj.message.trim();
    // If the message string itself is JSON, parse it
    const parsedInner = tryParseJson(msg);
    if (parsedInner && typeof parsedInner === "object" && !Array.isArray(parsedInner)) {
      const extracted = extractFromErrorObject(parsedInner as Record<string, unknown>);
      if (extracted) return extracted;
    }
    return msg;
  }

  // 4. Check for `title` (RFC 7807/9110 Problem Details)
  if ("title" in obj && typeof obj.title === "string" && obj.title.trim()) {
    return obj.title.trim();
  }

  // 5. Check for `error` / `errorMessage` / `error_description`
  if ("error_description" in obj && typeof obj.error_description === "string" && obj.error_description.trim()) {
    return obj.error_description.trim();
  }
  if ("error" in obj && typeof obj.error === "string" && obj.error.trim()) {
    return obj.error.trim();
  }
  if ("errorMessage" in obj && typeof obj.errorMessage === "string" && obj.errorMessage.trim()) {
    return obj.errorMessage.trim();
  }

  return null;
}

/**
 * Sanitizes and extracts the cleanest, human-readable error message from any error object,
 * string, API response (including RFC 9110/7807 ProblemDetails and ValidationProblemDetails),
 * or storage service XML.
 */
export function extractErrorMessage(error: unknown, fallbackMessage = "An error occurred"): string {
  if (!error) {
    return fallbackMessage;
  }

  // If error is already a string
  if (typeof error === "string") {
    const trimmed = error.trim();

    // Sanitize raw XML responses (e.g. from Azure Blob Storage or S3)
    if (trimmed.includes("<?xml") || trimmed.includes("<Error>") || trimmed.includes("AuthorizationFailure")) {
      return "Upload failed: Access permission expired or denied. Please refresh the page and try again.";
    }

    // Try parsing as JSON if it looks like a JSON string
    const parsedJson = tryParseJson(trimmed);
    if (parsedJson && typeof parsedJson === "object" && !Array.isArray(parsedJson)) {
      const extracted = extractFromErrorObject(parsedJson as Record<string, unknown>);
      if (extracted) return extracted;
    }

    return trimmed || fallbackMessage;
  }

  // If error is an object
  if (typeof error === "object") {
    const errObj = error as Record<string, unknown>;

    // Check `error.data` (ApiError)
    if ("data" in errObj && errObj.data && typeof errObj.data === "object") {
      const extracted = extractFromErrorObject(errObj.data as Record<string, unknown>);
      if (extracted) return extracted;
    }

    // Check `error.response.data` (Axios / ApiError)
    if ("response" in errObj && errObj.response && typeof errObj.response === "object") {
      const resObj = errObj.response as Record<string, unknown>;
      if ("data" in resObj && resObj.data && typeof resObj.data === "object") {
        const extracted = extractFromErrorObject(resObj.data as Record<string, unknown>);
        if (extracted) return extracted;
      }
      const extractedFromRes = extractFromErrorObject(resObj);
      if (extractedFromRes) return extractedFromRes;
    }

    // Check root error object properties
    const extractedFromRoot = extractFromErrorObject(errObj);
    if (extractedFromRoot) return extractedFromRoot;

    // Check standard `error.message`
    if ("message" in errObj && typeof errObj.message === "string" && errObj.message.trim()) {
      const msg = errObj.message.trim();

      // Sanitize raw XML responses
      if (msg.includes("<?xml") || msg.includes("<Error>") || msg.includes("AuthorizationFailure")) {
        return "Upload failed: Access permission expired or denied. Please refresh the page and try again.";
      }

      // Check if message itself is a serialized JSON string
      const parsedJson = tryParseJson(msg);
      if (parsedJson && typeof parsedJson === "object" && !Array.isArray(parsedJson)) {
        const extracted = extractFromErrorObject(parsedJson as Record<string, unknown>);
        if (extracted) return extracted;
      }

      return msg;
    }
  }

  return fallbackMessage;
}

