export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  timestamp: Date;
  attachments?: ChatAttachment[];
  isStreaming?: boolean;
}

export interface ChatAttachment {
  id: string;
  name: string;
  type: string;
  size: number;
  url?: string;
  preview?: string;
}

export interface ChatConversation {
  id: string;
  title: string;
  messages: ChatMessage[];
  createdAt: Date;
  updatedAt: Date;
}

export interface SendMessageRequest {
  prompt: string;
  files?: File[];
}

export interface ChatResponseDto {
  response: string;
  timestamp: string; // ISO date string from API
  filesProcessed: number;
}

export interface SendMessageResponse {
  response: string;
  timestamp: Date;
  filesProcessed: number;
}
