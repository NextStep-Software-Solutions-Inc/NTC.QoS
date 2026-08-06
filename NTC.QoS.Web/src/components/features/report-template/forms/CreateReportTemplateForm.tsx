import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createReportTemplateRequest } from "../handlers/createReportTemplateRequest";
import { type ReportTemplate } from "../types/ReportTemplate";

interface CreateReportTemplateFormProps {
  onCreated?: (reportTemplate: ReportTemplate) => void;
  onClose?: () => void;
}

const useCreateReportTemplate = createMutationHookFromTemplate(createReportTemplateRequest);

export default function CreateReportTemplateForm({ onCreated, onClose }: CreateReportTemplateFormProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [instruction, setInstruction] = useState("");

  const { mutateAsync, isPending } = useCreateReportTemplate();

  const queryClient = useQueryClient();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    toast.promise(
      mutateAsync({
        pathParams: {},
        payload: { title, description: description || null, instruction },
      }),
      {
        loading: "Creating report template...",
        success: (response) => {
          void queryClient.invalidateQueries({
            predicate: (query) => query.queryKey[0] === "get-report-templates",
          });
          onCreated?.(response.data);
          onClose?.();
          return response.message ?? "Report template created successfully!";
        },
        error: (error: unknown) => {
          console.error(error);
          return "Failed to create report template.";
        },
      },
    );
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-xl">
      {/* Title */}
      <div>
        <Label htmlFor="title" className="text-sm font-medium">
          Title <span className="text-red-500">*</span>
        </Label>
        <Input
          id="title"
          placeholder="Enter template title"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          maxLength={200}
          required
        />
      </div>

      {/* Description */}
      <div>
        <Label htmlFor="description" className="text-sm font-medium">
          Description
        </Label>
        <textarea
          id="description"
          placeholder="Enter template description"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium min-h-[80px]"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={500}
        />
      </div>

      {/* Instruction */}
      <div>
        <Label htmlFor="instruction" className="text-sm font-medium">
          Instruction <span className="text-red-500">*</span>
        </Label>
        <textarea
          id="instruction"
          placeholder="Enter AI instruction for report generation"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium min-h-[150px]"
          value={instruction}
          onChange={(e) => setInstruction(e.target.value)}
          required
        />
        <p className="text-xs text-muted-foreground mt-1">
          This instruction will guide AI when generating reports using this template.
        </p>
      </div>

      {/* Buttons */}
      <div className="flex justify-between gap-2 mt-4">
        <Button variant="outline" type="button" onClick={onClose}>
          Cancel
        </Button>
        <SubmitButton loading={isPending}>{isPending ? "Creating..." : "Create"}</SubmitButton>
      </div>
    </form>
  );
}
