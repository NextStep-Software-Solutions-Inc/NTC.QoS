"use client";

import { motion } from "framer-motion";
import { Bot, User } from "lucide-react";
import Markdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { cn } from "@/lib/utils";
import { type ChatMessage } from "../types/ChatMessage";

interface MessageBubbleProps {
  message: ChatMessage;
}

export function MessageBubble({ message }: MessageBubbleProps) {
  const isUser = message.role === "user";
  const isSystem = message.role === "system";

  if (isSystem) {
    return (
      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex justify-center my-4"
      >
        <div className="px-4 py-2 bg-gray-100 dark:bg-gray-800 rounded-full text-sm text-gray-600 dark:text-gray-400">
          {message.content}
        </div>
      </motion.div>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className={cn(
        "flex gap-3 mb-6",
        isUser ? "flex-row-reverse" : "flex-row"
      )}
    >
      {/* Avatar */}
      <div
        className={cn(
          "flex-shrink-0 h-10 w-10 rounded-full flex items-center justify-center",
          isUser
            ? "bg-gradient-to-br from-blue-500 to-purple-600"
            : "bg-gradient-to-br from-emerald-500 to-teal-600"
        )}
      >
        {isUser ? (
          <User className="h-5 w-5 text-white" />
        ) : (
          <Bot className="h-5 w-5 text-white" />
        )}
      </div>

      {/* Message Content */}
      <div
        className={cn(
          "flex flex-col",
          isUser ? "items-end max-w-[75%]" : "items-start max-w-[85%]"
        )}
      >
        <div
          className={cn(
            "px-4 py-3 rounded-2xl shadow-sm w-full",
            isUser
              ? "bg-gradient-to-br from-blue-500 to-purple-600 text-white rounded-tr-sm"
              : "bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-tl-sm"
          )}
        >
          {isUser ? (
            // User messages: plain text
            <p className="text-sm leading-relaxed whitespace-pre-wrap text-white">
              {message.content}
            </p>
          ) : (
            // Assistant messages: render markdown
            <div
              className={cn(
                "prose prose-sm dark:prose-invert max-w-none break-words",
                "text-gray-900 dark:text-gray-100",
                // Paragraphs
                "prose-p:my-3 prose-p:leading-relaxed prose-p:text-gray-900 dark:prose-p:text-gray-100 prose-p:break-words",
                // Headings
                "prose-headings:mt-6 prose-headings:mb-3 prose-headings:font-bold prose-headings:text-gray-900 dark:prose-headings:text-gray-100 prose-headings:break-words",
                "prose-h1:text-xl prose-h2:text-lg prose-h3:text-base",
                // Lists - proper spacing and markers
                "prose-ul:my-4 prose-ul:list-disc prose-ul:pl-6 prose-ul:space-y-1.5",
                "prose-ol:my-4 prose-ol:list-decimal prose-ol:pl-6 prose-ol:space-y-1.5",
                "prose-li:my-1 prose-li:text-gray-900 dark:prose-li:text-gray-100 prose-li:leading-relaxed prose-li:break-words",
                // Nested lists - circle markers and proper indentation
                "prose-li>ul:mt-2 prose-li>ul:mb-1 prose-li>ul:list-[circle] prose-li>ul:pl-5",
                "prose-li>ol:mt-2 prose-li>ol:mb-1 prose-li>ol:pl-5",
                // Code
                "prose-code:bg-gray-100 dark:prose-code:bg-gray-700 prose-code:text-pink-600 dark:prose-code:text-pink-400",
                "prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded prose-code:text-xs",
                "prose-code:before:content-none prose-code:after:content-none",
                "prose-pre:bg-gray-900 dark:prose-pre:bg-black prose-pre:text-gray-100",
                "prose-pre:rounded-lg prose-pre:overflow-x-auto prose-pre:my-3 prose-pre:p-4",
                // Links
                "prose-a:text-blue-600 dark:prose-a:text-blue-400 prose-a:font-medium prose-a:no-underline hover:prose-a:underline",
                // Blockquotes
                "prose-blockquote:border-l-4 prose-blockquote:border-gray-300 dark:prose-blockquote:border-gray-600",
                "prose-blockquote:pl-4 prose-blockquote:italic prose-blockquote:my-3 prose-blockquote:text-gray-700 dark:prose-blockquote:text-gray-300",
                // Tables
                "prose-table:text-sm prose-table:my-3",
                "prose-th:p-2 prose-th:bg-gray-100 dark:prose-th:bg-gray-800 prose-th:font-semibold",
                "prose-td:p-2 prose-td:border prose-td:border-gray-200 dark:prose-td:border-gray-700",
                // Strong/Bold
                "prose-strong:font-bold prose-strong:text-gray-900 dark:prose-strong:text-gray-100",
                // Horizontal rules
                "prose-hr:my-4 prose-hr:border-gray-300 dark:prose-hr:border-gray-700"
              )}
            >
              <Markdown remarkPlugins={[remarkGfm]}>
                {message.content || " "}
              </Markdown>
              {/* Blinking cursor for streaming */}
              {message.isStreaming && (
                <span className="inline-block w-0.5 h-4 ml-0.5 bg-gray-900 dark:bg-gray-100 animate-pulse" />
              )}
            </div>
          )}

          {/* Attachments */}
          {message.attachments && message.attachments.length > 0 && (
            <div className="mt-3 space-y-2">
              {message.attachments.map((attachment) => (
                <div
                  key={attachment.id}
                  className={cn(
                    "p-2 rounded-lg text-xs",
                    isUser
                      ? "bg-white/20 backdrop-blur-sm"
                      : "bg-gray-100 dark:bg-gray-700"
                  )}
                >
                  {attachment.name}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Timestamp */}
        <span className="text-xs text-gray-500 dark:text-gray-400 mt-1 px-2">
          {new Date(message.timestamp).toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
          })}
        </span>
      </div>
    </motion.div>
  );
}
