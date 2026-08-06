"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { updateReportTemplateRequest } from "../handlers/updateReportTemplateRequest";
import { type ReportTemplate } from "../types/ReportTemplate";

interface EditReportTemplateFormProps {
  onClose?: () => void;
  reportTemplate?: ReportTemplate;
}

const useEditReportTemplate = createMutationHookFromTemplate(updateReportTemplateRequest);

const EditReportTemplateForm = ({ onClose, reportTemplate }: EditReportTemplateFormProps) => {
  const [title, setTitle] = useState(reportTemplate?.title ?? "");
  const [description, setDescription] = useState(reportTemplate?.description ?? "");
  const [instruction, setInstruction] = useState(reportTemplate?.instruction ?? "");
  const [isActive, setIsActive] = useState(reportTemplate?.isActive ?? true);

  const reportTemplateId = reportTemplate?.id;

  const { mutateAsync, isPending } = useEditReportTemplate();
  const queryClient = useQueryClient();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    toast.promise(
      mutateAsync({
        pathParams: { id: reportTemplateId ?? "" },
        payload: {
          title: title || null,
          description: description || null,
          instruction: instruction || null,
          isActive,
        },
      }),
      {
        loading: "Updating report template...",
        success: () => {
          void queryClient.invalidateQueries({
            predicate: (query) => query.queryKey[0] === "get-report-templates",
          });
          onClose?.();
          return "Report template updated successfully!";
        },
        error: "Error updating report template.",
      },
    );
  };

  return (
    <CardContent className="">
      <form onSubmit={handleSubmit} className="h-full space-y-4 cursor-default">
        {/* Title */}
        <div className="space-y-1">
          <Label htmlFor="title" className="text-sm font-medium">
            Title
          </Label>
          <Input
            id="title"
            type="text"
            placeholder="Template Title"
            className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            maxLength={200}
          />
        </div>

        {/* Description */}
        <div className="space-y-1">
          <Label htmlFor="description" className="text-sm font-medium">
            Description
          </Label>
          <textarea
            id="description"
            placeholder="Description"
            className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            maxLength={500}
          />
        </div>

        {/* Instruction */}
        <div className="space-y-1">
          <Label htmlFor="instruction" className="text-sm font-medium">
            Instruction
          </Label>
          <textarea
            id="instruction"
            placeholder="AI instruction for report generation"
            className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
            value={instruction}
            onChange={(e) => setInstruction(e.target.value)}
            rows={5}
          />
          <p className="text-xs text-muted-foreground">
            This instruction will guide AI when generating reports using this template.
          </p>
        </div>

        {/* Is Active Toggle */}
        <div className="flex items-center space-x-3">
          <Switch id="isActive" checked={isActive} onCheckedChange={setIsActive} />
          <Label htmlFor="isActive" className="text-sm font-medium">
            Active
          </Label>
        </div>

        <div className="flex justify-between pt-4">
          <Button type="button" variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <SubmitButton loading={isPending} type="submit" className="bg-gray-700 hover:bg-gray-800 text-white">
            {isPending ? "Updating..." : "Save"}
          </SubmitButton>
        </div>
      </form>
    </CardContent>
  );
};

export default EditReportTemplateForm;
