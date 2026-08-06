"use client";

import { useState, useRef } from "react";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Download, Loader2, FileText, Calendar, User, Sparkles, Printer, Copy, Check, ChevronDown } from "lucide-react";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import Markdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { toast } from "sonner";
import { type GeneratedReportDto } from "../types/Report";
import { cn } from "@/lib/utils";
import { stripMarkdownCodeFence } from "./stripMarkdownCodeFence";

interface ReportPreviewDialogProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    report: GeneratedReportDto | null;
}

const QOS_BACKEND_URL =
    process.env.QOS_BACKEND_URL ?? process.env.NEXT_PUBLIC_QOS_BACKEND_URL ?? "http://localhost:3000";

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

export default function ReportPreviewDialog({
    open,
    onOpenChange,
    report,
}: ReportPreviewDialogProps) {
    const [isDownloading, setIsDownloading] = useState(false);
    const [copied, setCopied] = useState(false);
    const [isPromptExpanded, setIsPromptExpanded] = useState(false);
    const contentRef = useRef<HTMLDivElement>(null);

    const handleDownload = async (format: "pdf" | "markdown") => {
        if (!report?.id) return;

        setIsDownloading(true);
        try {
            const token = getSessionFromCookie();
            const response = await fetch(`${QOS_BACKEND_URL}/genai/reports/${report.id}/download?format=${format}`, {
                method: "GET",
                headers: {
                    Accept: format === "pdf" ? "application/pdf" : "text/markdown",
                    ...(token ? { Authorization: `Bearer ${token}` } : {}),
                },
            });

            if (!response.ok) {
                throw new Error(`Download failed: ${response.status}`);
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            const fileExtension = format === "pdf" ? "pdf" : "md";
            a.download = `${report.title.replace(/[^a-z0-9]/gi, "_")}.${fileExtension}`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
            toast.success(`Report downloaded as ${format.toUpperCase()} successfully!`);
        } catch (error) {
            console.error("Download error:", error);
            toast.error("Failed to download report. Please try again.");
        } finally {
            setIsDownloading(false);
        }
    };

    const handlePrint = () => {
        if (contentRef.current) {
            const printContent = contentRef.current.innerHTML;
            const printWindow = window.open("", "_blank");
            if (printWindow) {
                printWindow.document.write(`
          <!DOCTYPE html>
          <html>
            <head>
              <title>${report?.title || "Report"}</title>
              <style>
                body {
                  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
                  line-height: 1.6;
                  padding: 40px;
                  max-width: 800px;
                  margin: 0 auto;
                }
                h1, h2, h3, h4, h5, h6 { margin-top: 1.5em; margin-bottom: 0.5em; }
                h1 { font-size: 2em; border-bottom: 2px solid #e5e7eb; padding-bottom: 0.3em; }
                h2 { font-size: 1.5em; border-bottom: 1px solid #e5e7eb; padding-bottom: 0.3em; }
                p { margin: 1em 0; }
                ul, ol { margin: 1em 0; padding-left: 2em; }
                li { margin: 0.5em 0; }
                code { background: #f3f4f6; padding: 0.2em 0.4em; border-radius: 3px; font-size: 0.9em; }
                pre { background: #f3f4f6; padding: 1em; border-radius: 6px; overflow-x: auto; }
                blockquote { border-left: 4px solid #e5e7eb; margin: 1em 0; padding-left: 1em; color: #6b7280; }
                table { border-collapse: collapse; width: 100%; margin: 1em 0; }
                th, td { border: 1px solid #e5e7eb; padding: 0.75em; text-align: left; }
                th { background: #f9fafb; }
                @media print {
                  body { padding: 20px; }
                }
              </style>
            </head>
            <body>${printContent}</body>
          </html>
        `);
                printWindow.document.close();
                printWindow.print();
            }
        }
    };

    const handleCopyContent = async () => {
        if (report?.content) {
            try {
                await navigator.clipboard.writeText(report.content);
                setCopied(true);
                toast.success("Content copied to clipboard!");
                setTimeout(() => setCopied(false), 2000);
            } catch {
                toast.error("Failed to copy content");
            }
        }
    };

    if (!report) return null;

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-4xl w-[95vw] max-h-[90vh] flex flex-col">
                <DialogHeader className="flex-shrink-0">
                    <DialogTitle className="sr-only">{report.title}</DialogTitle>
                    <DialogDescription className="sr-only">AI-generated report preview</DialogDescription>
                </DialogHeader>

                <div className="flex flex-col h-full min-h-0">
                    {/* Report Header */}
                    <div className="flex-shrink-0 pb-4 border-b border-gray-200 dark:border-gray-700 space-y-4">
                        {/* Title and Icon */}
                        <div className="flex items-start gap-3">
                            <div className="p-2 sm:p-3 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex-shrink-0">
                                <Sparkles className="h-5 w-5 sm:h-6 sm:w-6 text-white" />
                            </div>
                            <div className="min-w-0 flex-1">
                                <h2 className="text-lg sm:text-xl font-bold">{report.title}</h2>
                                <div className="flex flex-wrap items-center gap-3 mt-2 text-xs text-gray-500 dark:text-gray-400">
                                    <span className="flex items-center gap-1">
                                        <Calendar className="h-3 w-3" />
                                        {new Date(report.generatedAt).toLocaleString()}
                                    </span>
                                    {report.filesAnalyzed > 0 && (
                                        <span className="flex items-center gap-1">
                                            <FileText className="h-3 w-3" />
                                            {report.filesAnalyzed} files analyzed
                                        </span>
                                    )}
                                </div>
                            </div>
                        </div>

                        {/* Original Prompt Section */}
                        {report.prompt && (
                            <div className="bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800 rounded-lg overflow-hidden">
                                <button
                                    onClick={() => setIsPromptExpanded(!isPromptExpanded)}
                                    className="w-full p-3 flex items-center justify-between hover:bg-blue-100 dark:hover:bg-blue-900/30 transition-colors"
                                >
                                    <div className="flex items-center gap-2">
                                        <User className="h-4 w-4 text-blue-600 dark:text-blue-400 flex-shrink-0" />
                                        <p className="text-xs font-semibold text-blue-900 dark:text-blue-100">Original Prompt</p>
                                    </div>
                                    <ChevronDown
                                        className={cn(
                                            "h-4 w-4 text-blue-600 dark:text-blue-400 transition-transform duration-200 flex-shrink-0",
                                            isPromptExpanded && "transform rotate-180"
                                        )}
                                    />
                                </button>
                                {isPromptExpanded && (
                                    <div className="px-3 pb-3 max-h-[30vh] overflow-y-auto">
                                        <div className={cn(
                                            "prose prose-sm dark:prose-invert max-w-none",
                                            "prose-p:text-blue-800 dark:prose-p:text-blue-200 prose-p:my-1",
                                            "prose-strong:text-blue-900 dark:prose-strong:text-blue-100",
                                            "prose-code:bg-blue-100 dark:prose-code:bg-blue-900/30 prose-code:text-blue-900 dark:prose-code:text-blue-100",
                                            "prose-ul:my-1 prose-ol:my-1 prose-li:my-0.5",
                                            "prose-li:text-blue-800 dark:prose-li:text-blue-200"
                                        )}>
                                            <Markdown remarkPlugins={[remarkGfm]}>
                                                {stripMarkdownCodeFence(report.prompt)}
                                            </Markdown>
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}

                        {/* Action Buttons - Desktop */}
                        <div className="hidden sm:flex items-center gap-2 flex-wrap">
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handleCopyContent}
                            >
                                {copied ? (
                                    <Check className="h-4 w-4 mr-2 text-green-500" />
                                ) : (
                                    <Copy className="h-4 w-4 mr-2" />
                                )}
                                {copied ? "Copied!" : "Copy"}
                            </Button>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handlePrint}
                            >
                                <Printer className="h-4 w-4 mr-2" />
                                Print
                            </Button>
                            <DropdownMenu>
                                <DropdownMenuTrigger asChild>
                                    <Button
                                        disabled={isDownloading}
                                        className="bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700"
                                        size="sm"
                                    >
                                        {isDownloading ? (
                                            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                        ) : (
                                            <Download className="h-4 w-4 mr-2" />
                                        )}
                                        Download
                                        <ChevronDown className="h-4 w-4 ml-2" />
                                    </Button>
                                </DropdownMenuTrigger>
                                <DropdownMenuContent align="end">
                                    <DropdownMenuItem onClick={() => handleDownload("pdf")}>
                                        <Download className="h-4 w-4 mr-2" />
                                        Download as PDF
                                    </DropdownMenuItem>
                                    <DropdownMenuItem onClick={() => handleDownload("markdown")}>
                                        <Download className="h-4 w-4 mr-2" />
                                        Download as Markdown
                                    </DropdownMenuItem>
                                </DropdownMenuContent>
                            </DropdownMenu>
                        </div>
                    </div>

                    {/* Report Content */}
                    <div className="flex-1 overflow-y-auto mt-4 min-h-0">
                        {report.content ? (
                            <div
                                ref={contentRef}
                                className={cn(
                                    "prose prose-sm sm:prose-base dark:prose-invert max-w-none",
                                    "prose-headings:font-bold prose-headings:text-gray-900 dark:prose-headings:text-gray-100",
                                    "prose-h1:text-2xl prose-h1:border-b prose-h1:border-gray-200 dark:prose-h1:border-gray-700 prose-h1:pb-2",
                                    "prose-h2:text-xl prose-h2:border-b prose-h2:border-gray-100 dark:prose-h2:border-gray-800 prose-h2:pb-1",
                                    "prose-p:text-gray-700 dark:prose-p:text-gray-300",
                                    "prose-li:text-gray-700 dark:prose-li:text-gray-300",
                                    "prose-code:bg-gray-100 dark:prose-code:bg-gray-800 prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded prose-code:text-sm",
                                    "prose-pre:bg-gray-100 dark:prose-pre:bg-gray-800 prose-pre:border prose-pre:border-gray-200 dark:prose-pre:border-gray-700",
                                    "prose-blockquote:border-l-blue-500 prose-blockquote:bg-blue-50/50 dark:prose-blockquote:bg-blue-950/20 prose-blockquote:py-1 prose-blockquote:px-4 prose-blockquote:rounded-r",
                                    "prose-table:border-collapse prose-th:bg-gray-100 dark:prose-th:bg-gray-800",
                                    "prose-td:border prose-td:border-gray-200 dark:prose-td:border-gray-700 prose-td:px-3 prose-td:py-2",
                                    "prose-th:border prose-th:border-gray-200 dark:prose-th:border-gray-700 prose-th:px-3 prose-th:py-2",
                                    "prose-a:text-blue-600 dark:prose-a:text-blue-400 prose-a:no-underline hover:prose-a:underline"
                                )}
                            >
                                <Markdown remarkPlugins={[remarkGfm]}>
                                    {stripMarkdownCodeFence(report.content)}
                                </Markdown>
                            </div>
                        ) : (
                            <div className="flex flex-col items-center justify-center py-12 text-gray-500">
                                <FileText className="h-16 w-16 mb-4 text-gray-300 dark:text-gray-600" />
                                <p className="text-lg font-medium">No content available</p>
                                <p className="text-sm text-gray-400 dark:text-gray-500 mt-1">
                                    The report content is still being generated or is unavailable.
                                </p>
                            </div>
                        )}
                    </div>

                    {/* Mobile Action Buttons */}
                    <div className="flex-shrink-0 pt-4 border-t border-gray-200 dark:border-gray-700 sm:hidden">
                        <div className="flex items-center gap-2">
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handleCopyContent}
                                className="flex-1"
                            >
                                {copied ? <Check className="h-4 w-4 mr-2 text-green-500" /> : <Copy className="h-4 w-4 mr-2" />}
                                {copied ? "Copied" : "Copy"}
                            </Button>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handlePrint}
                                className="flex-1"
                            >
                                <Printer className="h-4 w-4 mr-2" />
                                Print
                            </Button>
                        </div>
                    </div>
                </div>
            </DialogContent>
        </Dialog >
    );
}
