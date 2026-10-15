import { getBackendUrl } from "@/lib/api-utils";
import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { type GenerateReportRequest, type GenerateReportResponse } from "../types/Report";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";

const QOS_BACKEND_URL = getBackendUrl();

const endpoint = "/genai/generate-report" as const;

// Helper to get session token from cookie
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

export const generateReportRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<GenerateReportResponse>,
  undefined,
  GenerateReportRequest
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) {
      throw new Error("Payload is required");
    }

    // Create FormData for multipart/form-data request
    const formData = new FormData();
    formData.append("reportTitle", payload.reportTitle);
    if (payload.reportDescription) {
      formData.append("reportDescription", payload.reportDescription);
    }
    if (payload.templateId) {
      formData.append("templateId", payload.templateId);
    }
    formData.append("prompt", payload.prompt);

    // Append files if provided
    if (payload.files && payload.files.length > 0) {
      payload.files.forEach((file) => {
        formData.append("files", file);
      });
    }

    const token = getSessionFromCookie();

    // Send multipart/form-data request with custom fetch to handle SSE streaming
    const response = await fetch(`${QOS_BACKEND_URL}${finalUrl}`, {
      method: "POST",
      body: formData,
      signal: signal || undefined,
      headers: {
        Accept: "application/json, text/event-stream",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (!response.ok) {
      const errorText = await response.text().catch(() => "Unknown error");
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    const contentType = response.headers.get("content-type");

    // Handle SSE streaming response
    if (contentType?.includes("text/event-stream")) {
      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error("No response body reader available");
      }

      const decoder = new TextDecoder();
      let result: APIResponse<GenerateReportResponse> | null = null;

      try {
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          const chunk = decoder.decode(value, { stream: true });
          const lines = chunk.split("\n");

          for (const line of lines) {
            if (line.startsWith("data:")) {
              const jsonData = line.slice(5).trim();
              if (jsonData && jsonData !== "[DONE]") {
                try {
                  const parsed = JSON.parse(jsonData);
                  // The backend may send partial updates or a final result
                  // We capture the final successful response
                  if (parsed.succeeded !== undefined || parsed.data) {
                    result = parsed as APIResponse<GenerateReportResponse>;
                  }
                } catch {
                  // Continue parsing next chunks
                }
              }
            }
          }
        }
      } finally {
        reader.releaseLock();
      }

      if (result) {
        return result;
      }

      // If no valid JSON was parsed from SSE, return a default success response
      return {
        succeeded: true,
        data: {
          id: "",
          title: payload.reportTitle,
          promptPreview: payload.reportDescription || null,
          prompt: payload.prompt,
          content: null,
          filesAnalyzed: payload.files?.length || 0,
          fileMetadata: null,
          generatedAt: new Date().toISOString(),
          templateId: payload.templateId || null,
        },
        message: "Report generation started",
        errors: {},
      };
    }

    // Handle standard JSON response
    return await response.json() as APIResponse<GenerateReportResponse>;
  },
};

// Create React Query hook
export const useGenerateReport = createMutationHookFromTemplate(generateReportRequest);
