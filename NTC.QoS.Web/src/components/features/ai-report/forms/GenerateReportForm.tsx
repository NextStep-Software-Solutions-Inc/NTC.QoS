"use client";

import { useState, useEffect } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FileUploader } from "../../ai-assistant/components/FileUploader";
import SubmitButton from "@/components/SubmitButton";
import { Sparkles, FileText, TrendingUp, Map, Activity, Radio, CheckCircle } from "lucide-react";
import { ReportTemplate } from "../types/Report";
import { useGenerateReport } from "../handlers/generateReportRequest";

interface GenerateReportFormProps {
  onGenerated?: () => void;
  onClose?: () => void;
}

export default function GenerateReportForm({
  onGenerated,
  onClose,
}: GenerateReportFormProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [prompt, setPrompt] = useState("");
  const [selectedTemplate, setSelectedTemplate] = useState<string>("");
  const [templates, setTemplates] = useState<ReportTemplate[]>([]);
  const [attachedFiles, setAttachedFiles] = useState<File[]>([]);
  
  // Use the actual API mutation hook
  const generateReportMutation = useGenerateReport();
  const isPending = generateReportMutation.isPending;

  // Load templates on mount
  useEffect(() => {
    fetch("/report-templates/templates.json")
      .then((res) => res.json())
      .then((data) => {
        setTemplates(data.templates || []);
      })
      .catch((err) => {
        console.error("Failed to load report templates:", err);
        toast.error("Failed to load report templates");
      });
  }, []);

  // Get icon component based on icon name
  const getIconComponent = (iconName: string) => {
    const icons: Record<string, React.ElementType> = {
      Map,
      Activity,
      TrendingUp,
      Radio,
      CheckCircle,
      FileText,
    };
    return icons[iconName] || FileText;
  };

  const handleTemplateSelect = (templateId: string) => {
    setSelectedTemplate(templateId);
    const template = templates.find((t) => t.id === templateId);
    if (template && template.suggestedPrompts && template.suggestedPrompts.length > 0) {
      // Pre-fill prompt with first suggested prompt
      const firstPrompt = template.suggestedPrompts[0];
      if (firstPrompt) {
        setPrompt(firstPrompt);
      }
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

    try {
      const response = await generateReportMutation.mutateAsync({
        pathParams: {},
        payload: {
          reportTitle: title,
          reportDescription: description || undefined,
          templateId: selectedTemplate || undefined,
          prompt,
          files: attachedFiles.length > 0 ? attachedFiles : undefined,
        },
      });
      
      if (response?.data) {
        const templateName = templates.find((t) => t.id === selectedTemplate)?.name || "custom";
        toast.success(`Report generation started using ${templateName} template! You'll be notified when it's ready.`);
        onGenerated?.();
        onClose?.();

        // Reset form
        setTitle("");
        setDescription("");
        setPrompt("");
        setSelectedTemplate("");
        setAttachedFiles([]);
      }
    } catch (error) {
      toast.error("Failed to generate report. Please try again.");
      console.error("Error generating report:", error);
    }
  };

  const selectedTemplateData = templates.find((t) => t.id === selectedTemplate);

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6 w-full px-1">
      {/* Header with Icon */}
      <div className="flex items-center gap-3 pb-4 border-b border-gray-200 dark:border-gray-700">
        <div className="h-12 w-12 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center flex-shrink-0">
          <Sparkles className="h-6 w-6 text-white" />
        </div>
        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-semibold">AI Report Generation</h3>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Select a template and let AI create comprehensive reports
          </p>
        </div>
      </div>

      {/* Template Selection */}
      <div>
        <Label className="text-sm font-medium mb-3 block">
          Select Report Template
        </Label>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {templates.map((template) => {
            const IconComponent = getIconComponent(template.icon);
            return (
              <button
                key={template.id}
                type="button"
                onClick={() => handleTemplateSelect(template.id)}
                className={`p-4 rounded-lg border-2 text-left transition-all ${selectedTemplate === template.id
                    ? "border-blue-500 bg-blue-50 dark:bg-blue-950/20"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                  }`}
              >
                <div className="flex items-start gap-3">
                  <div className={`h-10 w-10 rounded-lg flex items-center justify-center flex-shrink-0 ${selectedTemplate === template.id
                      ? "bg-blue-500 text-white"
                      : "bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400"
                    }`}>
                    <IconComponent className="h-5 w-5" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h4 className="font-medium text-sm mb-1">{template.name}</h4>
                    <p className="text-xs text-gray-600 dark:text-gray-400 line-clamp-2">
                      {template.description}
                    </p>
                  </div>
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* Template Details (when selected) */}
      {selectedTemplateData && (
        <div className="p-4 bg-gradient-to-br from-blue-50 to-purple-50 dark:from-blue-950/20 dark:to-purple-950/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <div className="flex items-start gap-3 mb-3">
            <div className="h-8 w-8 rounded-lg bg-blue-500 text-white flex items-center justify-center flex-shrink-0">
              {(() => {
                const IconComponent = getIconComponent(selectedTemplateData.icon);
                return <IconComponent className="h-4 w-4" />;
              })()}
            </div>
            <div className="flex-1">
              <h4 className="font-semibold text-sm text-blue-900 dark:text-blue-100">
                {selectedTemplateData.name}
              </h4>
              <p className="text-xs text-blue-700 dark:text-blue-300 mt-1">
                {selectedTemplateData.description}
              </p>
            </div>
          </div>

          {selectedTemplateData.suggestedPrompts && selectedTemplateData.suggestedPrompts.length > 0 && (
            <div className="mt-3">
              <p className="text-xs font-medium text-blue-800 dark:text-blue-200 mb-2">
                Suggested prompts:
              </p>
              <div className="space-y-1">
                {selectedTemplateData.suggestedPrompts.map((suggestedPrompt, idx) => (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => setPrompt(suggestedPrompt)}
                    className="w-full text-left text-xs px-3 py-2 rounded bg-white/50 dark:bg-gray-900/50 hover:bg-white dark:hover:bg-gray-900 text-blue-800 dark:text-blue-200 transition-colors flex items-center gap-2"
                  >
                    <span className="text-blue-400">•</span>
                    <span>{suggestedPrompt}</span>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Report Title */}
      <div>
        <Label htmlFor="title" className="text-sm font-medium flex items-center gap-2">
          <FileText className="h-4 w-4" />
          Report Title
        </Label>
        <Input
          id="title"
          placeholder="e.g., Q4 2024 Network Coverage Analysis"
          className="mt-2"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
        />
      </div>

      {/* Description (Optional) */}
      <div>
        <Label htmlFor="description" className="text-sm font-medium">
          Description (Optional)
        </Label>
        <Input
          id="description"
          placeholder="Brief description of the report"
          className="mt-2"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>

      {/* Prompt - What to include */}
      <div>
        <Label htmlFor="prompt" className="text-sm font-medium flex items-center gap-2">
          <TrendingUp className="h-4 w-4" />
          What should the report include?
        </Label>
        <Textarea
          id="prompt"
          placeholder="Describe what you want the AI to analyze and include in the report. For example:&#10;- Analyze network coverage trends in Region X&#10;- Compare provider performance metrics&#10;- Identify underserved areas&#10;- Include speed test data analysis"
          className="mt-2 min-h-[120px]"
          value={prompt}
          onChange={(e) => setPrompt(e.target.value)}
          required
        />
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
          Be specific about the metrics, regions, or analyses you want included
        </p>
      </div>

      {/* File Attachments */}
      <div>
        <Label className="text-sm font-medium mb-2 block">
          Attach Supporting Files (Optional)
        </Label>
        <p className="text-xs text-gray-500 dark:text-gray-400 mb-3">
          Upload data files, images, or documents to provide context for the AI
        </p>
        <FileUploader
          onFilesChange={setAttachedFiles}
          maxFiles={10}
          disabled={isPending}
        />
      </div>

      {/* Info Box */}
      <div className="p-4 bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800 rounded-lg">
        <p className="text-sm text-blue-800 dark:text-blue-300">
          <strong>💡 Tip:</strong> Templates provide a structured format for your report.
          The AI will analyze all attached files along with system data, following the selected
          template structure to generate comprehensive insights.
        </p>
      </div>

      {/* Action Buttons */}
      <div className="flex justify-between gap-2 pt-4 border-t border-gray-200 dark:border-gray-700">
        <Button variant="outline" type="button" onClick={onClose} disabled={isPending}>
          Cancel
        </Button>
        <SubmitButton
          loading={isPending}
          className="bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700"
        >
          {isPending ? "Generating..." : "Generate Report"}
        </SubmitButton>
      </div>
    </form>
  );
}
