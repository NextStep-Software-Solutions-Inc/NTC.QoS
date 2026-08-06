"use client";

import { useState, useRef, useEffect } from "react";
import { Send, Paperclip, Sparkles, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { ScrollArea } from "@/components/ui/scroll-area";
import { MessageBubble } from "./MessageBubble";
import { TypingIndicator } from "./TypingIndicator";
import { ThinkingIndicator } from "./ThinkingIndicator";
import { WorkflowProcessingIndicator } from "./WorkflowProcessingIndicator";
import { FileUploader } from "./FileUploader";
import { type ChatMessage } from "../types/ChatMessage";
import { cn } from "@/lib/utils";
import { useSendChatMessage } from "../handlers/sendChatMessageRequest";
import { sendChatMessageStream } from "../handlers/sendChatMessageStreamRequest";

interface ChatInterfaceProps {
  className?: string;
}

export function ChatInterface({ className }: ChatInterfaceProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: "welcome",
      role: "system",
      content: "AI Assistant is ready to help you!",
      timestamp: new Date(),
    },
    {
      id: "intro",
      role: "assistant",
      content:
        "Hello! I'm your AI assistant. I can help you with information about broadband coverage, service quality, and telecommunications infrastructure. You can also attach files for me to analyze. How can I assist you today?",
      timestamp: new Date(),
    },
  ]);
  const [inputMessage, setInputMessage] = useState("");
  const [attachedFiles, setAttachedFiles] = useState<File[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const [isThinking, setIsThinking] = useState(false); // Show thinking indicator before streaming starts
  const [isProcessingWorkflow, setIsProcessingWorkflow] = useState(false); // Show workflow processing indicator when files are being processed
  const [uploadProgress, setUploadProgress] = useState(0); // Upload progress percentage
  const [showFileUploader, setShowFileUploader] = useState(false);
  const [useStreaming, setUseStreaming] = useState(true); // Toggle for streaming vs non-streaming
  const scrollAreaRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const abortControllerRef = useRef<AbortController | null>(null);
  const lastMessageRef = useRef<HTMLDivElement>(null);

  // Use the actual API mutation hook (for non-streaming)
  const sendMessageMutation = useSendChatMessage();

  // Function to upload files with progress tracking
  const uploadWithProgress = (
    url: string,
    formData: FormData,
    onProgress: (progress: number) => void,
    signal?: AbortSignal
  ): Promise<Response> => {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      // Set up progress tracking
      xhr.upload.addEventListener('progress', (event) => {
        if (event.lengthComputable) {
          const progress = Math.round((event.loaded / event.total) * 100);
          onProgress(progress);
        }
      });

      // Set up completion
      xhr.addEventListener('load', () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          // Create a Response-like object from XMLHttpRequest
          const response = new Response(xhr.response, {
            status: xhr.status,
            statusText: xhr.statusText,
            headers: new Headers({
              'content-type': xhr.getResponseHeader('content-type') || 'application/json',
            }),
          });
          resolve(response);
        } else {
          reject(new Error(`HTTP ${xhr.status}: ${xhr.statusText}`));
        }
      });

      // Set up error handling
      xhr.addEventListener('error', () => {
        reject(new Error('Network error during upload'));
      });

      xhr.addEventListener('abort', () => {
        reject(new Error('Upload aborted'));
      });

      // Handle abort signal
      if (signal) {
        signal.addEventListener('abort', () => {
          xhr.abort();
        });
      }

      // Configure and send request
      xhr.open('POST', url);
      xhr.responseType = 'text'; // We'll parse JSON manually

      // Add authorization header if token exists
      const token = document.cookie
        .split(";")
        .find((c) => c.trim().startsWith("__session"))
        ?.split("=")[1];

      if (token) {
        xhr.setRequestHeader('Authorization', `Bearer ${token}`);
      }

      xhr.send(formData);
    });
  };

  const handleSendMessage = async () => {
    if (!inputMessage.trim() && attachedFiles.length === 0) return;

    // Add user message
    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      role: "user",
      content: inputMessage,
      timestamp: new Date(),
      attachments: attachedFiles.map((file, index) => ({
        id: `${Date.now()}-${index}`,
        name: file.name,
        type: file.type,
        size: file.size,
      })),
    };

    setMessages((prev) => [...prev, userMessage]);
    const currentMessage = inputMessage;
    const currentFiles = [...attachedFiles];
    setInputMessage("");
    setAttachedFiles([]);
    setShowFileUploader(false);

    // If files are attached, show workflow processing indicator while API processes files
    if (currentFiles.length > 0) {
      setIsProcessingWorkflow(true);
      setUploadProgress(0);
    }

    // Call the actual API
    setIsThinking(true); // Start with thinking indicator

    // Create abort controller for this request
    abortControllerRef.current = new AbortController();

    try {
      if (useStreaming) {
        // Use streaming API for real-time responses
        const aiMessageId = (Date.now() + 1).toString();
        let streamedContent = "";
        let hasReceivedFirstChunk = false;

        await sendChatMessageStream(
          {
            prompt: currentMessage,
            files: currentFiles.length > 0 ? currentFiles : undefined,
          },
          // onChunk: Update message content with each chunk
          (chunk: string) => {
            // First chunk received - hide workflow processing indicator and add the message
            if (!hasReceivedFirstChunk) {
              hasReceivedFirstChunk = true;
              setUploadProgress(100);
              setIsProcessingWorkflow(false); // Hide workflow processing when response starts
              setIsThinking(false);

              // Add the message with streaming flag
              const aiResponse: ChatMessage = {
                id: aiMessageId,
                role: "assistant",
                content: chunk,
                timestamp: new Date(),
                isStreaming: true,
              };
              setMessages((prev) => [...prev, aiResponse]);
              streamedContent = chunk;
            } else {
              // Subsequent chunks - update the existing message
              streamedContent += chunk;
              setMessages((prev) =>
                prev.map((msg) =>
                  msg.id === aiMessageId
                    ? { ...msg, content: streamedContent, isStreaming: true }
                    : msg
                )
              );
            }
          },
          // onComplete: Streaming finished
          () => {
            // Mark message as no longer streaming
            setMessages((prev) =>
              prev.map((msg) =>
                msg.id === aiMessageId
                  ? { ...msg, isStreaming: false }
                  : msg
              )
            );
          },
          // onError: Handle streaming errors
          (error: Error) => {
            console.error("Streaming error:", error);
            setUploadProgress(100);
            setIsProcessingWorkflow(false); // Hide workflow processing on error
            setIsThinking(false);

            // If we haven't received any content yet, add error message
            if (!hasReceivedFirstChunk) {
              const errorResponse: ChatMessage = {
                id: aiMessageId,
                role: "assistant",
                content: "I'm sorry, I encountered an error while processing your request. Please try again later.",
                timestamp: new Date(),
              };
              setMessages((prev) => [...prev, errorResponse]);
            } else {
              // Update the existing message to show error and stop streaming
              setMessages((prev) =>
                prev.map((msg) =>
                  msg.id === aiMessageId
                    ? {
                      ...msg,
                      content: streamedContent || "An error occurred.",
                      isStreaming: false,
                    }
                    : msg
                )
              );
            }
          },
          // onProgress: Handle upload progress for file uploads
          currentFiles.length > 0 ? (progress) => setUploadProgress(progress) : undefined,
          abortControllerRef.current.signal
        );
      } else {
        // Use non-streaming API for complete responses
        let response;
        if (currentFiles.length > 0) {
          // Use XMLHttpRequest with progress tracking for file uploads
          const formData = new FormData();
          formData.append("prompt", currentMessage);
          currentFiles.forEach((file) => {
            formData.append("files", file);
          });

          const backendUrl = process.env.QOS_BACKEND_URL ?? process.env.NEXT_PUBLIC_QOS_BACKEND_URL ?? "http://localhost:3000";
          response = await uploadWithProgress(
            `${backendUrl}/genai/chat`,
            formData,
            (progress) => setUploadProgress(progress),
            abortControllerRef.current.signal
          );

          // Parse the response
          const responseData = JSON.parse(await response.text());
          setUploadProgress(100);
          setIsProcessingWorkflow(false);
          setIsThinking(false);

          if (responseData.data) {
            const aiResponse: ChatMessage = {
              id: (Date.now() + 1).toString(),
              role: "assistant",
              content: responseData.data.response,
              timestamp: new Date(responseData.data.timestamp),
            };
            setMessages((prev) => [...prev, aiResponse]);
          }
        } else {
          // Use regular API call for non-file requests
          response = await sendMessageMutation.mutateAsync({
            pathParams: {},
            payload: {
              prompt: currentMessage,
              files: currentFiles.length > 0 ? currentFiles : undefined,
            },
          });

          setIsThinking(false);

          if (response?.data) {
            const aiResponse: ChatMessage = {
              id: (Date.now() + 1).toString(),
              role: "assistant",
              content: response.data.response,
              timestamp: response.data.timestamp,
            };
            setMessages((prev) => [...prev, aiResponse]);
          }
        }
      }
    } catch (error) {
      setUploadProgress(0);
      setIsThinking(false);
      setIsProcessingWorkflow(false); // Reset workflow processing state on error
      // Show error message to user
      const errorResponse: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: "assistant",
        content: "I'm sorry, I encountered an error while processing your request. Please try again later.",
        timestamp: new Date(),
      };
      setMessages((prev) => [...prev, errorResponse]);
      console.error("Error sending message:", error);
    } finally {
      setIsTyping(false);
      setIsThinking(false);
      setIsProcessingWorkflow(false); // Ensure workflow processing state is reset
      abortControllerRef.current = null;
    }
  };

  // Auto-scroll to bottom when new assistant response is available or file uploader is shown
  useEffect(() => {
    const lastMessage = messages[messages.length - 1];
    if (lastMessage && (lastMessage.role === 'assistant' || isThinking || isTyping || isProcessingWorkflow) || showFileUploader) {
      // Small delay to ensure DOM has updated
      setTimeout(() => {
        if (lastMessageRef.current) {
          lastMessageRef.current.scrollIntoView({ behavior: 'smooth', block: 'end' });
        } else if (scrollAreaRef.current) {
          const scrollContainer = scrollAreaRef.current.querySelector('[data-radix-scroll-area-viewport]');
          if (scrollContainer) {
            scrollContainer.scrollTop = scrollContainer.scrollHeight;
          }
        }
      }, 100);
    }
  }, [messages, isTyping, isThinking, isProcessingWorkflow, showFileUploader]);

  // Cleanup on unmount - abort any ongoing requests
  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      void handleSendMessage();
    }
  };

  return (
    <div className={cn("flex flex-col h-full min-h-0 overflow-hidden bg-gradient-to-br from-gray-50 to-blue-50/30 dark:from-gray-900 dark:to-blue-950/20", className)}>
      {/* Header */}
      <div className="flex-shrink-0 py-3 sm:py-4 border-b border-gray-200 dark:border-gray-800 bg-white/80 dark:bg-gray-900/80 backdrop-blur-sm">
        <div className="flex items-center gap-2 sm:gap-3">
          <div className="h-8 w-8 sm:h-10 sm:w-10 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center flex-shrink-0">
            <Sparkles className="h-4 w-4 sm:h-5 sm:w-5 text-white" />
          </div>
          <div className="min-w-0">
            <h2 className="text-base sm:text-lg font-semibold truncate">AI Chat Assistant</h2>
            <p className="text-xs sm:text-sm text-gray-600 dark:text-gray-400 truncate">
              Powered by Google Gen AI
            </p>
          </div>
        </div>
      </div>

      {/* Messages */}
      <ScrollArea ref={scrollAreaRef} className="flex-1 min-h-0 overflow-y-auto overflow-x-hidden py-3 sm:py-4">
        <div className="space-y-3 sm:space-y-4">
          {messages.map((message, index) => (
            <div
              key={message.id}
              ref={index === messages.length - 1 ? lastMessageRef : null}
            >
              <MessageBubble message={message} />
            </div>
          ))}
          {isProcessingWorkflow && (
            <div ref={messages.length === 0 ? lastMessageRef : null}>
              <WorkflowProcessingIndicator progress={uploadProgress} />
            </div>
          )}
          {isThinking && !isProcessingWorkflow && (
            <div ref={messages.length === 0 ? lastMessageRef : null}>
              <ThinkingIndicator />
            </div>
          )}
          {isTyping && !isThinking && !isProcessingWorkflow && (
            <div ref={messages.length > 0 ? lastMessageRef : null}>
              <TypingIndicator />
            </div>
          )}
        </div>
      </ScrollArea>

      {/* File Uploader (Collapsible) */}
      {showFileUploader && (
        <div className="flex-shrink-0 py-3 sm:py-4 bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-800">
          <div className="flex items-start justify-between mb-2">
            <h3 className="text-sm font-medium">Attach Files</h3>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => setShowFileUploader(false)}
              className="h-6 w-6 p-0"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
          <FileUploader
            onFilesChange={setAttachedFiles}
            maxFiles={5}
          />
        </div>
      )}

      {/* Input Area */}
      <div className="flex-shrink-0 py-3 sm:py-4 bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-800">
        <div className="flex gap-2 items-end">
          <Button
            type="button"
            variant="outline"
            size="icon"
            onClick={() => setShowFileUploader(!showFileUploader)}
            className={cn(
              "flex-shrink-0 h-9 w-9 sm:h-10 sm:w-10",
              showFileUploader && "bg-blue-50 dark:bg-blue-950/30 border-blue-300 dark:border-blue-700"
            )}
          >
            <Paperclip className="h-4 w-4" />
          </Button>

          <div className="flex-1 min-w-0 relative">
            <Textarea
              ref={textareaRef}
              value={inputMessage}
              onChange={(e) => setInputMessage(e.target.value)}
              onKeyDown={handleKeyPress}
              placeholder="Type your message..."
              className="min-h-[44px] max-h-[120px] resize-none w-full text-sm sm:text-base"
              rows={1}
            />
          </div>

          <Button
            type="button"
            onClick={handleSendMessage}
            disabled={!inputMessage.trim() && attachedFiles.length === 0}
            className="flex-shrink-0 h-9 w-9 sm:h-10 sm:w-10 bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 text-white p-0"
            size="icon"
          >
            <Send className="h-4 w-4" />
          </Button>
        </div>

        {attachedFiles.length > 0 && (
          <div className="mt-2 text-xs text-gray-600 dark:text-gray-400">
            {attachedFiles.length} file(s) attached
          </div>
        )}
      </div>
    </div>
  );
}
