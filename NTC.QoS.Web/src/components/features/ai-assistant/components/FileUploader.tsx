"use client";

import { useState, useRef } from "react";
import { Upload, X, File, Image as ImageIcon, FileText } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface FileUploaderProps {
  onFilesChange: (files: File[]) => void;
  maxFiles?: number;
  acceptedTypes?: string;
  disabled?: boolean;
  className?: string;
}

export function FileUploader({
  onFilesChange,
  maxFiles = 5,
  acceptedTypes = "image/*,.pdf,.xlsx,.xls,.csv,.txt,.doc,.docx",
  disabled = false,
  className,
}: FileUploaderProps) {
  const [files, setFiles] = useState<File[]>([]);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFiles = (newFiles: FileList | null) => {
    if (!newFiles || disabled) return;

    const fileArray = Array.from(newFiles);
    const remainingSlots = maxFiles - files.length;
    const filesToAdd = fileArray.slice(0, remainingSlots);

    const updatedFiles = [...files, ...filesToAdd];
    setFiles(updatedFiles);
    onFilesChange(updatedFiles);
  };

  const removeFile = (index: number) => {
    const updatedFiles = files.filter((_, i) => i !== index);
    setFiles(updatedFiles);
    onFilesChange(updatedFiles);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    if (!disabled) setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    handleFiles(e.dataTransfer.files);
  };

  const getFileIcon = (file: File) => {
    if (file.type.startsWith("image/")) return <ImageIcon className="h-5 w-5" />;
    if (file.type === "application/pdf") return <FileText className="h-5 w-5" />;
    return <File className="h-5 w-5" />;
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return bytes + " B";
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB";
    return (bytes / (1024 * 1024)).toFixed(1) + " MB";
  };

  return (
    <div className={cn("space-y-4", className)}>
      {/* Drop Zone */}
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        className={cn(
          "relative border-2 border-dashed rounded-lg p-8 transition-all duration-300",
          isDragging
            ? "border-blue-500 bg-blue-50/50 dark:bg-blue-950/20"
            : "border-gray-300 dark:border-gray-700 hover:border-blue-400 dark:hover:border-blue-600",
          disabled && "opacity-50 cursor-not-allowed",
          files.length >= maxFiles && "opacity-50"
        )}
      >
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept={acceptedTypes}
          onChange={(e) => handleFiles(e.target.files)}
          className="hidden"
          disabled={disabled || files.length >= maxFiles}
        />

        <div className="flex flex-col items-center justify-center text-center">
          <div className="p-4 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 mb-4">
            <Upload className="h-8 w-8 text-white" />
          </div>
          
          <h3 className="text-lg font-semibold mb-2">
            {isDragging ? "Drop files here" : "Upload files"}
          </h3>
          
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
            Drag & drop or click to browse
          </p>
          
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => fileInputRef.current?.click()}
            disabled={disabled || files.length >= maxFiles}
          >
            Choose Files
          </Button>
          
          <p className="text-xs text-gray-500 dark:text-gray-500 mt-3">
            Max {maxFiles} files • Images, PDF, Excel, Text
          </p>
        </div>
      </div>

      {/* File List */}
      {files.length > 0 && (
        <div className="space-y-2">
          <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Attached Files ({files.length}/{maxFiles})
          </h4>
          {files.map((file, index) => (
            <div
              key={index}
              className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700 group hover:border-blue-300 dark:hover:border-blue-600 transition-colors"
            >
              <div className="flex items-center gap-3 flex-1 min-w-0">
                <div className="text-blue-600 dark:text-blue-400">
                  {getFileIcon(file)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">{file.name}</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    {formatFileSize(file.size)}
                  </p>
                </div>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => removeFile(index)}
                disabled={disabled}
                className="h-8 w-8 p-0 hover:bg-red-50 dark:hover:bg-red-950/30 hover:text-red-600"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
