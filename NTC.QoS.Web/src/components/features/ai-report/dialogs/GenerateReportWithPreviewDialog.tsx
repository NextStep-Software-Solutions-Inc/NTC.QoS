"use client";

import { useState, useEffect, useRef, useCallback, useMemo } from "react";
import { toast } from "sonner";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { FileUploader } from "../../ai-assistant/components/FileUploader";
import {
    Sparkles,
    FileText,
    Loader2,
    Download,
    Copy,
    Check,
    ChevronLeft,
    X,
    ChevronDown,
} from "lucide-react";
import Markdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { cn } from "@/lib/utils";
import { type GeneratedReportDto } from "../types/Report";
import { generatePDFFromMarkdown } from "@/utils/pdfGenerator";
import { getReportTemplatesRequest } from "@/components/features/report-template/handlers/getReportTemplatesRequest";
import { getReportTemplateRequest } from "@/components/features/report-template/handlers/getReportTemplateRequest";
import { type ReportTemplateListItem } from "@/components/features/report-template/types/ReportTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { buildEndpoint } from "@/lib/endpoint-builder";
import { request } from "@/services/fetch";
import { stripMarkdownCodeFence } from "./stripMarkdownCodeFence";

interface GenerateReportWithPreviewDialogProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    onGenerated?: () => void;
}

import { getBackendUrl } from "@/lib/api-utils";

const QOS_BACKEND_URL = getBackendUrl();

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

type ViewState = "form" | "generating" | "complete";

// Create the query hook using the factory pattern
const useGetReportTemplates = createQueryHookFromTemplate(getReportTemplatesRequest);

export default function GenerateReportWithPreviewDialog({
    open,
    onOpenChange,
    onGenerated,
}: GenerateReportWithPreviewDialogProps) {
    // Form state
    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [prompt, setPrompt] = useState("");
    const [selectedTemplate, setSelectedTemplate] = useState<string>("");
    const [attachedFiles, setAttachedFiles] = useState<File[]>([]);
    const [isLoadingTemplateDetails, setIsLoadingTemplateDetails] = useState(false);

    // Generation state
    const [viewState, setViewState] = useState<ViewState>("form");
    const [streamingContent, setStreamingContent] = useState("");
    const [generatedReport, setGeneratedReport] = useState<GeneratedReportDto | null>(null);
    const [isDownloading, setIsDownloading] = useState(false);
    const [copied, setCopied] = useState(false);

    const contentRef = useRef<HTMLDivElement>(null);
    const abortControllerRef = useRef<AbortController | null>(null);

    // Load templates from API using the query hook pattern
    const { data: templatesData, isLoading: isLoadingTemplates } = useGetReportTemplates({ pathParams: {}, queryParams: {} });
    const templates = useMemo<ReportTemplateListItem[]>(() => templatesData?.data ?? [], [templatesData?.data]);

    // Auto-scroll to bottom during streaming
    useEffect(() => {
        if (contentRef.current && viewState === "generating") {
            contentRef.current.scrollTop = contentRef.current.scrollHeight;
        }
    }, [streamingContent, viewState]);

    // Reset state when dialog closes
    useEffect(() => {
        if (!open) {
            // Cancel any ongoing request
            if (abortControllerRef.current) {
                abortControllerRef.current.abort();
                abortControllerRef.current = null;
            }
        }
    }, [open]);

    const resetForm = useCallback(() => {
        setTitle("");
        setDescription("");
        setPrompt("");
        setSelectedTemplate("");
        setAttachedFiles([]);
        setStreamingContent("");
        setGeneratedReport(null);
        setViewState("form");
    }, []);

    const handleTemplateSelect = async (templateId: string) => {
        setSelectedTemplate(templateId);
        const template = templates.find((t) => t.id === templateId);
        if (template?.title) {
            setTitle(template.title);
        }

        // Fetch full template details to get the instruction
        setIsLoadingTemplateDetails(true);
        try {
            const url = buildEndpoint(getReportTemplateRequest.endpoint, { id: templateId }, undefined);
            const response = await request.get<{ data: { instruction: string | null } }>(url);
            if (response.data.instruction) {
                setPrompt(response.data.instruction);
            }
        } catch {
            console.error("Failed to load template instruction");
        } finally {
            setIsLoadingTemplateDetails(false);
        }
    };

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (!title.trim()) {
            toast.error("Please enter a report title");
            return;
        }

        if (!prompt.trim()) {
            toast.error("Please describe what you want in the report");
            return;
        }

        // Start generating
        setViewState("generating");
        setStreamingContent("");

        const formData = new FormData();
        formData.append("reportTitle", title);
        if (description) formData.append("reportDescription", description);
        if (selectedTemplate) formData.append("templateId", selectedTemplate);
        formData.append("prompt", prompt);
        attachedFiles.forEach((file) => formData.append("files", file));

        const token = getSessionFromCookie();
        abortControllerRef.current = new AbortController();

        try {
            const response = await fetch(`${QOS_BACKEND_URL}/genai/generate-report`, {
                method: "POST",
                body: formData,
                signal: abortControllerRef.current.signal,
                headers: {
                    Accept: "text/event-stream, application/json",
                    ...(token ? { Authorization: `Bearer ${token}` } : {}),
                },
            });

            if (!response.ok) {
                const errorText = await response.text().catch(() => "Unknown error");
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }

            const contentType = response.headers.get("content-type");

            if (contentType?.includes("text/event-stream")) {
                // Handle SSE streaming
                const reader = response.body?.getReader();
                if (!reader) throw new Error("No response body reader available");

                const decoder = new TextDecoder();
                let accumulatedContent = "";
                let finalReport: GeneratedReportDto | null = null;
                let buffer = ""; // Buffer for incomplete data lines

                try {
                    while (true) {
                        const { done, value } = await reader.read();
                        if (done) break;

                        const chunk = decoder.decode(value, { stream: true });
                        buffer += chunk; // Add to buffer
                        const lines = buffer.split("\n");

                        // Keep the last incomplete line in the buffer
                        buffer = lines.pop() || "";

                        for (const line of lines) {
                            if (line.startsWith("data:")) {
                                const jsonData = line.slice(5).trim();
                                if (jsonData && jsonData !== "[DONE]") {
                                    try {
                                        const parsed = JSON.parse(jsonData);
                                        if (process.env.NODE_ENV === "development") {
                                            console.log("SSE event received:", parsed);
                                        }

                                        // Handle plain string chunks (backend sends raw strings as SSE data)
                                        if (typeof parsed === "string") {
                                            accumulatedContent += parsed;
                                            setStreamingContent(accumulatedContent);
                                            if (process.env.NODE_ENV === "development") {
                                                console.log("String chunk added, total length:", accumulatedContent.length);
                                            }
                                        }
                                        // Handle different SSE event object types
                                        else if (parsed.type === "content" && parsed.content) {
                                            // Streaming content chunk
                                            accumulatedContent += parsed.content;
                                            setStreamingContent(accumulatedContent);
                                            if (process.env.NODE_ENV === "development") {
                                                console.log("Content chunk added, total length:", accumulatedContent.length);
                                            }
                                        } else if (parsed.type === "complete" && parsed.data) {
                                            // Final report data
                                            finalReport = parsed.data as GeneratedReportDto;
                                            if (process.env.NODE_ENV === "development") {
                                                console.log("Complete event received with report:", finalReport);
                                            }
                                        } else if (parsed.content) {
                                            // Direct content (alternative format)
                                            accumulatedContent += parsed.content;
                                            setStreamingContent(accumulatedContent);
                                            if (process.env.NODE_ENV === "development") {
                                                console.log("Direct content added, total length:", accumulatedContent.length);
                                            }
                                        } else if (parsed.data?.content) {
                                            // Nested content
                                            accumulatedContent = parsed.data.content;
                                            setStreamingContent(accumulatedContent);
                                            if (process.env.NODE_ENV === "development") {
                                                console.log("Nested content set, length:", accumulatedContent.length);
                                            }
                                            finalReport = parsed.data as GeneratedReportDto;
                                        } else if (parsed.succeeded && parsed.data) {
                                            // Full response object
                                            finalReport = parsed.data as GeneratedReportDto;
                                            if (finalReport.content) {
                                                setStreamingContent(finalReport.content);
                                                if (process.env.NODE_ENV === "development") {
                                                    console.log("Final report content set, length:", finalReport.content.length);
                                                }
                                            }
                                        }
                                    } catch (parseError) {
                                        console.error("Failed to parse SSE event:", parseError, jsonData);
                                        // If not JSON, treat as raw content
                                        if (jsonData.length > 0 && !jsonData.startsWith("{")) {
                                            accumulatedContent += jsonData;
                                            setStreamingContent(accumulatedContent);
                                        }
                                    }
                                }
                            }
                        }
                    }
                } finally {
                    reader.releaseLock();
                }

                if (finalReport) {
                    setGeneratedReport(finalReport);
                } else if (accumulatedContent) {
                    // Create a synthetic report if we only got streaming content
                    setGeneratedReport({
                        id: "",
                        title: title,
                        promptPreview: description || null,
                        prompt,
                        content: accumulatedContent,
                        filesAnalyzed: attachedFiles.length,
                        fileMetadata: null,
                        generatedAt: new Date().toISOString(),
                        templateId: selectedTemplate || null,
                    });
                }

                setViewState("complete");
                toast.success("Report generated successfully!");
                onGenerated?.();
            } else {
                // Handle JSON response
                const data = await response.json();
                if (data.succeeded && data.data) {
                    setGeneratedReport(data.data);
                    if (data.data.content) {
                        setStreamingContent(data.data.content);
                    }
                    setViewState("complete");
                    toast.success("Report generated successfully!");
                    onGenerated?.();
                } else {
                    throw new Error(data.message || "Failed to generate report");
                }
            }
        } catch (error) {
            if ((error as Error).name === "AbortError") {
                toast.info("Report generation cancelled");
                setViewState("form");
            } else {
                console.error("Error generating report:", error);
                toast.error("Failed to generate report. Please try again.");
                setViewState("form");
            }
        }
    };

    const handleCancel = () => {
        if (viewState === "generating" && abortControllerRef.current) {
            abortControllerRef.current.abort();
        }
        if (viewState === "complete") {
            resetForm();
        }
        onOpenChange(false);
    };

    const handleNewReport = () => {
        resetForm();
    };

    const handleDownload = async (format: "pdf" | "markdown") => {
        setIsDownloading(true);

        try {
            // If we have a backend report ID, download from backend
            if (generatedReport?.id) {
                const token = getSessionFromCookie();
                const response = await fetch(`${QOS_BACKEND_URL}/genai/reports/${generatedReport.id}/download?format=${format}`, {
                    headers: {
                        Accept: format === "pdf" ? "application/pdf" : "text/markdown",
                        ...(token ? { Authorization: `Bearer ${token}` } : {}),
                    },
                });

                if (!response.ok) throw new Error(`Download failed: ${response.status}`);

                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                const fileExtension = format === "pdf" ? "pdf" : "md";
                a.download = `${title.replace(/[^a-z0-9]/gi, "_")}.${fileExtension}`;
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                document.body.removeChild(a);
                toast.success(`Report downloaded as ${format.toUpperCase()}!`);
            } else {
                // Client-side PDF generation from markdown
                if (!streamingContent) {
                    toast.error("No content available to download");
                    return;
                }

                await generatePDFFromMarkdown({
                    title,
                    content: streamingContent,
                });

                toast.success("PDF downloaded successfully!");
            }
        } catch (error) {
            console.error("Download error:", error);
            toast.error("Failed to download report");
        } finally {
            setIsDownloading(false);
        }
    };

    const handleCopyContent = async () => {
        try {
            await navigator.clipboard.writeText(streamingContent);
            setCopied(true);
            toast.success("Content copied!");
            setTimeout(() => setCopied(false), 2000);
        } catch {
            toast.error("Failed to copy");
        }
    };

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-6xl w-[95vw] h-[90vh] flex flex-col p-0 gap-0 overflow-hidden">
                <DialogHeader className="px-6 pt-6 pb-4 border-b flex-shrink-0">
                    <DialogTitle className="flex items-center gap-2">
                        <div className="h-8 w-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center">
                            <Sparkles className="h-4 w-4 text-white" />
                        </div>
                        {viewState === "form" && "Generate AI Report"}
                        {viewState === "generating" && "Generating Report..."}
                        {viewState === "complete" && title}
                    </DialogTitle>
                    <DialogDescription>
                        {viewState === "form" && "Create comprehensive reports using AI-powered analysis"}
                        {viewState === "generating" && "AI is analyzing your data and generating the report"}
                        {viewState === "complete" && "Your report has been generated successfully"}
                    </DialogDescription>
                </DialogHeader>

                <div className="flex-1 min-h-0 flex flex-col overflow-hidden">
                    {viewState === "form" ? (
                        /* Form View */
                        <form onSubmit={handleSubmit} className="flex-1 flex flex-col min-h-0 overflow-hidden">
                            <div className="flex-1 overflow-y-auto p-6 space-y-6">
                                {/* Template Selection */}
                                <div>
                                    <Label className="text-sm font-medium mb-3 block">Select Report Template</Label>
                                    {isLoadingTemplates ? (
                                        <div className="flex items-center justify-center py-8">
                                            <Loader2 className="h-6 w-6 animate-spin text-blue-500" />
                                            <span className="ml-2 text-sm text-muted-foreground">Loading templates...</span>
                                        </div>
                                    ) : templates.length === 0 ? (
                                        <div className="text-center py-8 text-muted-foreground">
                                            <FileText className="h-12 w-12 mx-auto mb-2 opacity-50" />
                                            <p>No templates available.</p>
                                            <p className="text-xs">Contact your administrator to create report templates.</p>
                                        </div>
                                    ) : (
                                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                                            {templates.map((template) => (
                                                <button
                                                    key={template.id}
                                                    type="button"
                                                    onClick={() => handleTemplateSelect(template.id)}
                                                    className={cn(
                                                        "p-3 rounded-lg border-2 text-left transition-all",
                                                        selectedTemplate === template.id
                                                            ? "border-blue-500 bg-blue-50 dark:bg-blue-950/20"
                                                            : "border-gray-200 dark:border-gray-700 hover:border-gray-300"
                                                    )}
                                                >
                                                    <div className="flex items-center gap-3">
                                                        <div
                                                            className={cn(
                                                                "h-8 w-8 rounded-lg flex items-center justify-center flex-shrink-0",
                                                                selectedTemplate === template.id
                                                                    ? "bg-blue-500 text-white"
                                                                    : "bg-gray-100 dark:bg-gray-800 text-gray-600"
                                                            )}
                                                        >
                                                            <FileText className="h-4 w-4" />
                                                        </div>
                                                        <div className="flex-1 min-w-0">
                                                            <h4 className="font-medium text-sm truncate">{template.title || "Untitled Template"}</h4>
                                                            {template.description && (
                                                                <p className="text-xs text-muted-foreground truncate mt-0.5">
                                                                    {template.description}
                                                                </p>
                                                            )}
                                                        </div>
                                                    </div>
                                                </button>
                                            ))}
                                        </div>
                                    )}
                                </div>

                                {/* Report Details */}
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div>
                                        <Label htmlFor="title" className="text-sm font-medium">
                                            Report Title *
                                        </Label>
                                        <Input
                                            id="title"
                                            placeholder="e.g., Q4 2024 Network Coverage Analysis"
                                            className="mt-1.5"
                                            value={title}
                                            onChange={(e) => setTitle(e.target.value)}
                                            required
                                        />
                                    </div>
                                    <div>
                                        <Label htmlFor="description" className="text-sm font-medium">
                                            Description (Optional)
                                        </Label>
                                        <Input
                                            id="description"
                                            placeholder="Brief description of the report"
                                            className="mt-1.5"
                                            value={description}
                                            onChange={(e) => setDescription(e.target.value)}
                                        />
                                    </div>
                                </div>

                                {/* Prompt */}
                                <div>
                                    <Label htmlFor="prompt" className="text-sm font-medium">
                                        What should the report include? *
                                    </Label>
                                    <div className="relative">
                                        <Textarea
                                            id="prompt"
                                            placeholder="Describe what you want the AI to analyze..."
                                            className="mt-1.5 min-h-[100px]"
                                            value={prompt}
                                            onChange={(e) => setPrompt(e.target.value)}
                                            required
                                            disabled={isLoadingTemplateDetails}
                                        />
                                        {isLoadingTemplateDetails && (
                                            <div className="absolute inset-0 flex items-center justify-center bg-background/50 mt-1.5 rounded-md">
                                                <Loader2 className="h-5 w-5 animate-spin text-primary" />
                                                <span className="ml-2 text-sm text-muted-foreground">Loading instruction...</span>
                                            </div>
                                        )}
                                    </div>
                                </div>

                                {/* File Attachments */}
                                <div>
                                    <Label className="text-sm font-medium mb-2 block">
                                        Attach Supporting Files (Optional)
                                    </Label>
                                    <FileUploader onFilesChange={setAttachedFiles} maxFiles={10} />
                                </div>
                            </div>

                            {/* Form Actions */}
                            <div className="flex-shrink-0 flex justify-between gap-2 p-4 border-t bg-gray-50 dark:bg-gray-900">
                                <Button variant="outline" type="button" onClick={() => onOpenChange(false)}>
                                    Cancel
                                </Button>
                                <Button
                                    type="submit"
                                    className="bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700"
                                >
                                    <Sparkles className="h-4 w-4 mr-2" />
                                    Generate Report
                                </Button>
                            </div>
                        </form>
                    ) : (
                        /* Preview View (Generating / Complete) */
                        <div className="flex-1 min-h-0 flex flex-col overflow-hidden">
                            {/* Live Preview Content */}
                            <div
                                ref={contentRef}
                                className="flex-1 min-h-0 overflow-y-auto p-6"
                            >
                                {streamingContent ? (
                                    <div
                                        className={cn(
                                            "prose prose-sm sm:prose-base dark:prose-invert max-w-none",
                                            "prose-headings:font-bold prose-headings:text-gray-900 dark:prose-headings:text-gray-100",
                                            "prose-h1:text-2xl prose-h1:border-b prose-h1:border-gray-200 dark:prose-h1:border-gray-700 prose-h1:pb-2",
                                            "prose-h2:text-xl prose-h2:border-b prose-h2:border-gray-100 dark:prose-h2:border-gray-800 prose-h2:pb-1",
                                            "prose-p:text-gray-700 dark:prose-p:text-gray-300",
                                            "prose-li:text-gray-700 dark:prose-li:text-gray-300",
                                            "prose-code:bg-gray-100 dark:prose-code:bg-gray-800 prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded prose-code:text-sm",
                                            "prose-pre:bg-gray-100 dark:prose-pre:bg-gray-800 prose-pre:border prose-pre:border-gray-200 dark:prose-pre:border-gray-700",
                                            "prose-blockquote:border-l-blue-500 prose-blockquote:bg-blue-50/50 dark:prose-blockquote:bg-blue-950/20",
                                            "prose-table:border-collapse prose-th:bg-gray-100 dark:prose-th:bg-gray-800",
                                            "prose-td:border prose-td:border-gray-200 dark:prose-td:border-gray-700 prose-td:px-3 prose-td:py-2",
                                            "prose-th:border prose-th:border-gray-200 dark:prose-th:border-gray-700 prose-th:px-3 prose-th:py-2"
                                        )}
                                    >
                                        <Markdown remarkPlugins={[remarkGfm]}>{stripMarkdownCodeFence(streamingContent)}</Markdown>
                                    </div>
                                ) : viewState === "generating" ? (
                                    <div className="flex flex-col items-center justify-center h-full text-gray-500">
                                        <Loader2 className="h-12 w-12 animate-spin mb-4 text-blue-500" />
                                        <p className="text-lg font-medium">Generating your report...</p>
                                        <p className="text-sm text-gray-400 mt-1">
                                            AI is analyzing data and creating comprehensive insights
                                        </p>
                                    </div>
                                ) : (
                                    <div className="flex flex-col items-center justify-center h-full text-gray-500">
                                        <FileText className="h-12 w-12 mb-4 text-gray-300" />
                                        <p>No content generated</p>
                                    </div>
                                )}

                                {/* Streaming indicator */}
                                {viewState === "generating" && streamingContent && (
                                    <div className="flex items-center gap-2 mt-4 text-blue-600 dark:text-blue-400">
                                        <Loader2 className="h-4 w-4 animate-spin" />
                                        <span className="text-sm">Generating...</span>
                                    </div>
                                )}
                            </div>

                            {/* Preview Actions */}
                            <div className="flex-shrink-0 flex justify-between items-center gap-2 p-4 border-t bg-gray-50 dark:bg-gray-900">
                                <div className="flex items-center gap-2">
                                    {viewState === "complete" && (
                                        <>
                                            <Button variant="outline" size="sm" onClick={handleCopyContent}>
                                                {copied ? (
                                                    <Check className="h-4 w-4 mr-2 text-green-500" />
                                                ) : (
                                                    <Copy className="h-4 w-4 mr-2" />
                                                )}
                                                {copied ? "Copied!" : "Copy"}
                                            </Button>
                                            <DropdownMenu>
                                                <DropdownMenuTrigger asChild>
                                                    <Button
                                                        variant="outline"
                                                        size="sm"
                                                        disabled={isDownloading}
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
                                                <DropdownMenuContent align="start">
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
                                        </>
                                    )}
                                    {viewState === "generating" && streamingContent && (
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
                                            {copied ? "Copied!" : "Copy Preview"}
                                        </Button>
                                    )}
                                </div>

                                <div className="flex items-center gap-2">
                                    {viewState === "generating" && (
                                        <Button variant="outline" onClick={handleCancel}>
                                            <X className="h-4 w-4 mr-2" />
                                            Cancel
                                        </Button>
                                    )}
                                    {viewState === "complete" && (
                                        <>
                                            <Button variant="outline" onClick={handleNewReport}>
                                                <ChevronLeft className="h-4 w-4 mr-2" />
                                                New Report
                                            </Button>
                                            <Button onClick={() => onOpenChange(false)}>
                                                Done
                                            </Button>
                                        </>
                                    )}
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
}
