"use client";

import { useState } from "react";
import {
    CheckCircle2,
    AlertCircle,
    Clock,
    Loader2,
    Sparkles,
    Eye,
    Download,
    ChevronDown,
    Plus,
    FileText,
    Trash2,
} from "lucide-react";

import { useGetReports } from "@/components/features/ai-report/handlers/getReportsRequest";
import { type GeneratedReportDto, type GeneratedReportSummaryDto } from "@/components/features/ai-report/types/Report";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { toast } from "sonner";
import GenerateReportWithPreviewDialog from "@/components/features/ai-report/dialogs/GenerateReportWithPreviewDialog";
import ReportPreviewDialog from "@/components/features/ai-report/dialogs/ReportPreviewDialog";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";

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

interface AIReportsManagerProps {
    showGenerateButton?: boolean;
    showDeleteButton?: boolean;
}

export function AIReportsManager({ showGenerateButton = true, showDeleteButton = false }: AIReportsManagerProps) {
    const [isGenerateDialogOpen, setIsGenerateDialogOpen] = useState(false);
    const [selectedReport, setSelectedReport] = useState<GeneratedReportDto | null>(null);
    const [isPreviewOpen, setIsPreviewOpen] = useState(false);
    const [viewingReportId, setViewingReportId] = useState<string | null>(null);
    const [downloadingReportId, setDownloadingReportId] = useState<string | null>(null);
    const [deleteReportId, setDeleteReportId] = useState<string | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    // Fetch AI reports from API
    const {
        data: reportsResponse,
        isLoading,
        refetch,
    } = useGetReports({
        pathParams: {},
        queryParams: { pageNumber: 1, pageSize: 200 },
    });

    const reports = reportsResponse?.data ?? [];

    // Since API returns only completed reports, set stats accordingly
    const stats = {
        completed: reports.length,
        processing: 0,
        pending: 0,
        failed: 0,
    };

    // Fetch full report details for preview
    const handleViewReport = async (reportSummary: GeneratedReportSummaryDto) => {
        setViewingReportId(reportSummary.id);
        try {
            const token = getSessionFromCookie();
            const response = await fetch(`${QOS_BACKEND_URL}/genai/reports/${reportSummary.id}`, {
                headers: {
                    Accept: "application/json",
                    ...(token ? { Authorization: `Bearer ${token}` } : {}),
                },
            });

            if (!response.ok) {
                throw new Error(`Failed to fetch report: ${response.status}`);
            }

            const data = await response.json();
            if (data.succeeded && data.data) {
                setSelectedReport(data.data as GeneratedReportDto);
                setIsPreviewOpen(true);
            } else {
                toast.error("Failed to load report details");
            }
        } catch (error) {
            console.error("Error fetching report:", error);
            toast.error("Failed to load report. Please try again.");
        } finally {
            setViewingReportId(null);
        }
    };

    // Direct download
    const handleDownloadReport = async (reportId: string, reportTitle: string, format: "pdf" | "markdown") => {
        setDownloadingReportId(reportId);
        try {
            const token = getSessionFromCookie();
            const response = await fetch(`${QOS_BACKEND_URL}/genai/reports/${reportId}/download?format=${format}`, {
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
            a.download = `${reportTitle.replace(/[^a-z0-9]/gi, "_")}.${fileExtension}`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
            toast.success(`Report downloaded as ${format.toUpperCase()} successfully!`);
        } catch (error) {
            console.error("Download error:", error);
            toast.error("Failed to download report. Please try again.");
        } finally {
            setDownloadingReportId(null);
        }
    };

    // Delete report
    const handleDeleteReport = async () => {
        if (!deleteReportId) return;

        setIsDeleting(true);
        try {
            const token = getSessionFromCookie();
            const response = await fetch(`${QOS_BACKEND_URL}/genai/reports/${deleteReportId}`, {
                method: "DELETE",
                headers: {
                    Accept: "application/json",
                    ...(token ? { Authorization: `Bearer ${token}` } : {}),
                },
            });

            if (!response.ok) {
                throw new Error(`Delete failed: ${response.status}`);
            }

            toast.success("Report deleted successfully!");
            void refetch();
        } catch (error) {
            console.error("Delete error:", error);
            toast.error("Failed to delete report. Please try again.");
        } finally {
            setIsDeleting(false);
            setDeleteReportId(null);
        }
    };

    const getStatusIcon = (status?: string) => {
        switch (status) {
            case "completed":
                return <CheckCircle2 className="h-5 w-5 text-green-600" />;
            case "processing":
                return <Loader2 className="h-5 w-5 text-blue-600 animate-spin" />;
            case "failed":
                return <AlertCircle className="h-5 w-5 text-red-600" />;
            default:
                return <Clock className="h-5 w-5 text-gray-600" />;
        }
    };

    const getStatusBadge = (status?: string) => {
        const normalizedStatus = status || "pending";
        const styles = {
            completed: "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400",
            processing: "bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400",
            failed: "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400",
            pending: "bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-400",
        };

        return (
            <span
                className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${styles[normalizedStatus as keyof typeof styles] || styles.pending}`}>
                {getStatusIcon(normalizedStatus)}
                {normalizedStatus.charAt(0).toUpperCase() + normalizedStatus.slice(1)}
            </span>
        );
    };

    return (
        <>
            <div className="space-y-6">
                {/* Header */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div className="space-y-1">
                        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight">AI Reports</h1>
                        <p className="text-sm sm:text-base text-muted-foreground">
                            {showDeleteButton
                                ? "Manage AI-powered analysis and comprehensive reports"
                                : "View AI-powered analysis and comprehensive reports"}
                        </p>
                    </div>
                    {showGenerateButton && (
                        <Button
                            onClick={() => setIsGenerateDialogOpen(true)}
                            className="bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 w-full sm:w-auto">
                            <Sparkles className="h-4 w-4 mr-2" />
                            <span>Generate AI Report</span>
                        </Button>
                    )}
                </div>

                {/* Stats Cards */}
                <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
                    <Card>
                        <CardHeader className="pb-2">
                            <CardTitle className="text-sm font-medium text-green-600">Completed</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-2xl font-bold">{stats.completed}</div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader className="pb-2">
                            <CardTitle className="text-sm font-medium text-blue-600">Processing</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-2xl font-bold">{stats.processing}</div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader className="pb-2">
                            <CardTitle className="text-sm font-medium text-gray-600">Pending</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-2xl font-bold">{stats.pending}</div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader className="pb-2">
                            <CardTitle className="text-sm font-medium text-red-600">Failed</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-2xl font-bold">{stats.failed}</div>
                        </CardContent>
                    </Card>
                </div>

                {/* Reports List */}
                <Card>
                    <CardHeader>
                        <div className="flex items-center justify-between">
                            <div>
                                <CardTitle>Recent AI Reports</CardTitle>
                                <CardDescription>View and manage generated AI reports</CardDescription>
                            </div>
                            {showGenerateButton && (
                                <Button variant="outline" size="sm" onClick={() => setIsGenerateDialogOpen(true)} className="hidden sm:flex">
                                    <Plus className="h-4 w-4 mr-2" />
                                    New Report
                                </Button>
                            )}
                        </div>
                    </CardHeader>
                    <CardContent>
                        <div className="divide-y divide-gray-200 dark:divide-gray-700">
                            {isLoading ? (
                                <div className="p-8 text-center text-gray-500">
                                    <Loader2 className="h-8 w-8 animate-spin mx-auto mb-2" />
                                    <p>Loading reports...</p>
                                </div>
                            ) : reports.length === 0 ? (
                                <div className="p-8 text-center text-gray-500">
                                    <FileText className="h-12 w-12 mx-auto mb-2 text-gray-400" />
                                    <p>No AI reports generated yet</p>
                                    {showGenerateButton && (
                                        <p className="text-sm mt-1">Click &quot;Generate AI Report&quot; to create your first AI-powered report</p>
                                    )}
                                </div>
                            ) : (
                                reports.map((report) => (
                                    <div key={report.id} className="p-4 sm:p-6 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                                        <div className="flex flex-col sm:flex-row sm:items-start gap-4">
                                            <div className="flex items-start gap-3 sm:gap-4 flex-1 min-w-0">
                                                <div className="p-2 sm:p-3 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex-shrink-0">
                                                    <FileText className="h-5 w-5 sm:h-6 sm:w-6 text-white" />
                                                </div>
                                                <div className="flex-1 min-w-0">
                                                    <div className="flex flex-col sm:flex-row sm:items-center gap-2 mb-2">
                                                        <h3 className="text-base sm:text-lg font-semibold truncate">{report.title}</h3>
                                                        {getStatusBadge("completed")}
                                                    </div>
                                                    {report.promptPreview && (
                                                        <p className="text-xs sm:text-sm text-gray-600 dark:text-gray-400 mb-2 line-clamp-2">
                                                            {report.promptPreview}
                                                        </p>
                                                    )}
                                                    {report.contentPreview && (
                                                        <p className="text-xs text-gray-500 dark:text-gray-500 mb-2 line-clamp-1 italic">
                                                            {report.contentPreview}
                                                        </p>
                                                    )}
                                                    <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-4 text-xs text-gray-500 dark:text-gray-400">
                                                        <span>Generated {new Date(report.generatedAt).toLocaleString()}</span>
                                                        {report.filesAnalyzed > 0 && (
                                                            <>
                                                                <span className="hidden sm:inline">•</span>
                                                                <span>{report.filesAnalyzed} files analyzed</span>
                                                            </>
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2 w-full sm:w-auto sm:ml-4">
                                                <Button
                                                    variant="outline"
                                                    size="sm"
                                                    className="w-full sm:w-auto"
                                                    onClick={() => handleViewReport(report)}
                                                    disabled={viewingReportId === report.id}>
                                                    {viewingReportId === report.id ? (
                                                        <Loader2 className="h-4 w-4 animate-spin" />
                                                    ) : (
                                                        <Eye className="h-4 w-4" />
                                                    )}
                                                    View
                                                </Button>
                                                <DropdownMenu>
                                                    <DropdownMenuTrigger asChild>
                                                        <Button variant="outline" size="sm" className="w-full sm:w-auto" disabled={downloadingReportId === report.id}>
                                                            {downloadingReportId === report.id ? (
                                                                <Loader2 className="h-4 w-4 animate-spin" />
                                                            ) : (
                                                                <Download className="h-4 w-4" />
                                                            )}
                                                            Download
                                                            <ChevronDown className="h-4 w-4" />
                                                        </Button>
                                                    </DropdownMenuTrigger>
                                                    <DropdownMenuContent align="end">
                                                        <DropdownMenuItem onClick={() => handleDownloadReport(report.id, report.title, "pdf")}>
                                                            <Download className="h-4 w-4" />
                                                            Download as PDF
                                                        </DropdownMenuItem>
                                                        <DropdownMenuItem onClick={() => handleDownloadReport(report.id, report.title, "markdown")}>
                                                            <Download className="h-4 w-4" />
                                                            Download as Markdown
                                                        </DropdownMenuItem>
                                                    </DropdownMenuContent>
                                                </DropdownMenu>
                                                {showDeleteButton && (
                                                    <Button
                                                        variant="destructive"
                                                        size="sm"
                                                        className="w-full sm:w-auto"
                                                        onClick={() => setDeleteReportId(report.id)}
                                                        disabled={viewingReportId === report.id || downloadingReportId === report.id}>
                                                        <Trash2 className="h-4 w-4" />
                                                        Delete
                                                    </Button>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                ))
                            )}
                        </div>
                    </CardContent>
                </Card>
            </div>

            {/* Generate Report Dialog with Live Preview */}
            <GenerateReportWithPreviewDialog
                open={isGenerateDialogOpen}
                onOpenChange={setIsGenerateDialogOpen}
                onGenerated={() => {
                    void refetch();
                }}
            />

            {/* Report Preview Dialog */}
            <ReportPreviewDialog open={isPreviewOpen} onOpenChange={setIsPreviewOpen} report={selectedReport} />

            {/* Delete Confirmation Dialog */}
            <AlertDialog open={!!deleteReportId} onOpenChange={(open) => !open && setDeleteReportId(null)}>
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Delete Report</AlertDialogTitle>
                        <AlertDialogDescription>
                            Are you sure you want to delete this report? This action cannot be undone.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
                        <AlertDialogAction onClick={handleDeleteReport} disabled={isDeleting} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                            {isDeleting ? (
                                <>
                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                    Deleting...
                                </>
                            ) : (
                                "Delete"
                            )}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </>
    );
}
