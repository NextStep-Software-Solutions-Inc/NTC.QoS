import { type APIResponse, type QueryBlueprintFromTemplate } from "@/types/reusable/api";
import { request } from "@/services/fetch";
import { type ChatConversation } from "../types/ChatMessage";

// NOTE: Conversation persistence is not currently supported by the backend API.
// The chat endpoint (/genai/chat) is stateless and doesn't maintain conversation history.
// This handler is kept for future implementation when conversation persistence is added.

const endpoint = "/genai/conversations" as const;

interface GetConversationsQueryParams {
  skip?: number;
  take?: number;
}

export const getConversationsRequest: QueryBlueprintFromTemplate<
  typeof endpoint,
  APIResponse<ChatConversation[]>,
  GetConversationsQueryParams
> = {
  endpoint,
  key: ({ url, pathParams, queryParams }) => ["get-conversations", url, endpoint, pathParams, queryParams] as const,
  fetcher: async ({ finalUrl, signal }) => {
    return await request.get<APIResponse<ChatConversation[]>>(finalUrl, { signal });
  },
};
