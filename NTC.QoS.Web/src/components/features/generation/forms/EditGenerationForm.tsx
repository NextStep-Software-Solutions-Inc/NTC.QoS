"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { type Generation } from "@/components/features/generation/types/Generation";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editGenerationRequest } from "../handlers/editGenerationRequest";

interface EditGenerationFormProps {
  onClose?: () => void;
  generation?: Generation;
}

const useEditGeneration = createMutationHookFromTemplate(editGenerationRequest);

const EditGenerationForm = ({ onClose, generation }: EditGenerationFormProps) => {
  const [generationName, setGenerationName] = useState(generation?.name ?? "");
  const [generationDescription, setGenerationDescription] = useState(generation?.description ?? "");

  const generationId = generation?.id;

  const { mutateAsync, isPending } = useEditGeneration();
  const queryClient = useQueryClient();

  const handleSubmitEditGeneration = (e: React.FormEvent) => {
    e.preventDefault();

    toast.promise(
      mutateAsync({
        pathParams: { id: generationId ?? "" },
        payload: { name: generationName, description: generationDescription },
      }),
      {
        loading: "Updating generation...",
        success: () => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-generations" });
          onClose?.();
          return "Generation updated successfully!";
        },
        error: "Error updating generation.",
      },
    );
  };

  return (
    <CardContent className="">
      <form onSubmit={handleSubmitEditGeneration} className=" h-full space-y-4 cursor-default">
        <div className="space-y-1"></div>
        <div className="space-y-1">
          <Label htmlFor="generation" className="text-sm font-medium">
            Generation Name
          </Label>
          <Input
            id="generation"
            type="text"
            placeholder={"Generation Name"}
            className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
            value={generationName}
            onChange={(e) => setGenerationName(e.target.value)}
          />
        </div>

        <div className="space-y-1">
          <Label htmlFor="description" className="text-sm font-medium">
            Description
          </Label>
          <textarea
            id="description"
            placeholder="Description"
            className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
            value={generationDescription}
            onChange={(e) => setGenerationDescription(e.target.value)}
            rows={3}
          />
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

export default EditGenerationForm;
