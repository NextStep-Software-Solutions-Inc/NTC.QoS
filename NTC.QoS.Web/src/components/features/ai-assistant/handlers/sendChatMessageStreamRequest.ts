import { type SendMessageRequest } from "../types/ChatMessage";
import { createParser, type EventSourceMessage } from "eventsource-parser";

const QOS_BACKEND_URL =
  process.env.QOS_BACKEND_URL ?? process.env.NEXT_PUBLIC_QOS_BACKEND_URL ?? "http://localhost:3000";

const getSessionFromCookie = () => {
  let token: string | null = null;
  if (typeof document !== "undefined") {
    token =
      document.cookie
        .split(";")
        .find((c) => c.trim().startsWith("__session"))
        ?.split("=")[1] ?? null;
  }
  return token;
};

/**
 * Send a chat message with streaming response support
 * This function handles Server-Sent Events (SSE) for real-time AI responses
 * 
 * @param payload - The message request containing prompt and optional files
 * @param onChunk - Callback function that receives each chunk of the streaming response
 * @param onComplete - Callback function called when streaming is complete
 * @param onError - Callback function called if an error occurs
 * @param onProgress - Optional callback for upload progress (0-100)
 * @param signal - Optional AbortSignal to cancel the request
 */
export async function sendChatMessageStream(
  payload: SendMessageRequest,
  onChunk: (chunk: string) => void,
  onComplete: () => void,
  onError: (error: Error) => void,
  onProgress?: (progress: number) => void,
  signal?: AbortSignal
): Promise<void> {
  const token = getSessionFromCookie();

  // If files are present and progress tracking is requested, use XMLHttpRequest
  if (payload.files && payload.files.length > 0 && onProgress) {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      // Set up abort handling
      if (signal) {
        signal.addEventListener('abort', () => {
          xhr.abort();
          onError(new Error('Request aborted'));
          reject(new Error('Request aborted'));
        });
      }

      xhr.open('POST', `${QOS_BACKEND_URL}/genai/chat`);

      // Set headers
      xhr.setRequestHeader('Accept', 'text/event-stream');
      if (token) {
        xhr.setRequestHeader('Authorization', `Bearer ${token}`);
      }

      // Track upload progress
      xhr.upload.addEventListener('progress', (event) => {
        if (event.lengthComputable && onProgress) {
          const progress = Math.round((event.loaded / event.total) * 100);
          onProgress(progress);
        }
      });

      // Handle response
      xhr.onreadystatechange = () => {
        if (xhr.readyState === 4) {
          if (xhr.status >= 200 && xhr.status < 300) {
            try {
              const contentType = xhr.getResponseHeader('content-type');
              if (contentType?.includes('text/event-stream') || contentType?.includes('application/x-ndjson')) {
                // Handle streaming response
                const responseText = xhr.responseText;
                const lines = responseText.split('\n');

                for (const line of lines) {
                  if (line.startsWith('data: ')) {
                    const data = line.slice(6).trim();
                    if (data === '[DONE]') continue;

                    try {
                      const parsed = JSON.parse(data);
                      if (parsed.content) {
                        onChunk(parsed.content);
                      } else if (parsed.delta) {
                        onChunk(parsed.delta);
                      } else if (parsed.chunk) {
                        onChunk(parsed.chunk);
                      } else if (typeof parsed === 'string') {
                        onChunk(parsed);
                      }
                    } catch {
                      // If not JSON, treat as plain text
                      if (data) onChunk(data);
                    }
                  }
                }
                onComplete();
                resolve();
              } else {
                // Fallback to JSON response
                const data = JSON.parse(xhr.responseText);
                if (data.response) {
                  onChunk(data.response);
                }
                onComplete();
                resolve();
              }
            } catch (error) {
              onError(error instanceof Error ? error : new Error('Failed to parse response'));
              reject(error);
            }
          } else {
            const error = new Error(`HTTP ${xhr.status}: ${xhr.statusText}`);
            onError(error);
            reject(error);
          }
        }
      };

      xhr.onerror = () => {
        const error = new Error('Network error');
        onError(error);
        reject(error);
      };

      // Create and send FormData
      const formData = new FormData();
      formData.append('prompt', payload.prompt);
      if (payload.files) {
        payload.files.forEach((file) => {
          formData.append('files', file);
        });
      }

      xhr.send(formData);
    });
  }

  // Original fetch-based implementation for non-file requests or when no progress tracking needed
  // Create FormData for multipart/form-data request
  const formData = new FormData();
  formData.append("prompt", payload.prompt);

  // Append files if provided
  if (payload.files && payload.files.length > 0) {
    payload.files.forEach((file) => {
      formData.append("files", file);
    });
  }

  try {
    const response = await fetch(`${QOS_BACKEND_URL}/genai/chat`, {
      method: "POST",
      headers: {
        Accept: "text/event-stream",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: formData,
      signal,
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    // Check if the response is actually a stream
    const contentType = response.headers.get("content-type");
    console.log({ contentType });
    if (!contentType?.includes("text/event-stream") && !contentType?.includes("application/x-ndjson")) {
      // Fallback to regular JSON response if not streaming
      const data = await response.json();
      if (data.response) {
        onChunk(data.response);
      }
      onComplete();
      return;
    }

    // Handle streaming response
    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error("Response body is not readable");
    }

    const decoder = new TextDecoder();

    // Create SSE parser for proper event parsing
    const parser = createParser({
      onEvent: (event: EventSourceMessage) => {
        const data = event.data;

        if (data === "[DONE]") {
          return;
        }

        try {
          // Try to parse as JSON first
          const parsed = JSON.parse(data);
          if (parsed.content) {
            onChunk(parsed.content);
          } else if (parsed.delta) {
            onChunk(parsed.delta);
          } else if (parsed.chunk) {
            onChunk(parsed.chunk);
          } else if (typeof parsed === "string") {
            onChunk(parsed);
          }
        } catch {
          // If not JSON, treat as plain text
          onChunk(data);
        }
      },
    });

    try {
      while (true) {
        const { done, value } = await reader.read();

        if (done) {
          break;
        }

        // Decode and feed to parser
        const chunk = decoder.decode(value, { stream: true });
        parser.feed(chunk);
      }

      onComplete();
    } catch (error) {
      if (error instanceof Error) {
        onError(error);
      } else {
        onError(new Error("Unknown error during streaming"));
      }
    } finally {
      reader.releaseLock();
    }
  } catch (error) {
    if (error instanceof Error) {
      onError(error);
    } else {
      onError(new Error("Failed to send chat message"));
    }
  }
}
