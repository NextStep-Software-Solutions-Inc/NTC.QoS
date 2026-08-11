import { type APIResponse, type MutationBlueprintFromTemplate } from "@/types/reusable/api";
import { request } from "@/services/fetch";
import { type SendMessageRequest, type ChatResponseDto, type SendMessageResponse } from "../types/ChatMessage";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";

const endpoint = "/genai/chat" as const;

export const sendChatMessageRequest: MutationBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<SendMessageResponse>,
  undefined,
  SendMessageRequest
> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    if (!payload) {
      throw new Error("Payload is required");
    }

    // Create FormData for multipart/form-data request
    const formData = new FormData();
    formData.append("prompt", payload.prompt);
    
    // Append files if provided
    if (payload.files && payload.files.length > 0) {
      payload.files.forEach((file) => {
        formData.append("files", file);
      });
    }

    // Send multipart/form-data request
    // Pass null as payload and FormData in body to avoid JSON.stringify
    const response = await request.post<APIResponse<ChatResponseDto>>(
      finalUrl,
      null,
      { body: formData, signal: signal || undefined }
    );

    // Transform response to match SendMessageResponse interface
    if (response.data) {
      const transformedData: SendMessageResponse = {
        response: response.data.response,
        timestamp: new Date(response.data.timestamp),
        filesProcessed: response.data.filesProcessed,
      };
      
      return {
        ...response,
        data: transformedData,
      } as APIResponse<SendMessageResponse>;
    }

    return response as unknown as APIResponse<SendMessageResponse>;
  },
};

// Create React Query hook
export const useSendChatMessage = createMutationHookFromTemplate(sendChatMessageRequest);
