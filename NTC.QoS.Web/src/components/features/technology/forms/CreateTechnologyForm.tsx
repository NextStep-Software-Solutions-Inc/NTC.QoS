"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { getGenerationsRequest } from "@/components/features/generation/handlers/getGenerationsRequest";
import { type Generation } from "@/components/features/generation/types/Generation";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { createTechnologyRequest } from "../handlers/createTechnologyRequest";
import { type Technology } from "../types/Technology";

const useGetGenerations = createQueryHookFromTemplate(getGenerationsRequest);

interface CreateTechnologyFormProps {
  onCreated?: (technology: Technology) => void;
  onClose?: () => void;
}

const useCreateTechnology = createMutationHookFromTemplate(createTechnologyRequest);

export default function CreateTechnologyForm({ onCreated, onClose }: CreateTechnologyFormProps) {
  const [standard, setStandard] = useState("");
  const [generationId, setGenerationId] = useState("");
  const [description, setDescription] = useState("");

  const { mutateAsync, isPending } = useCreateTechnology();
  const { data: generationsData } = useGetGenerations({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const generations = generationsData?.data ?? [];

  const queryClient = useQueryClient();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    toast.promise(mutateAsync({ pathParams: {}, payload: { standard, description, generationId } }), {
      loading: "Creating technology...",
      success: (response) => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-technologies" });
        onCreated?.(response.data);
        onClose?.();
        return response.message;
      },
      error: (error: unknown) => {
        console.error(error);
        return "Technology already exists.";
      },
    });
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-xl">
      {/* Standard */}
      <div>
        <Label htmlFor="standard" className="text-sm font-medium">
          Standard
        </Label>
        <Input
          id="standard"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={standard}
          onChange={(e) => setStandard(e.target.value)}
        />
      </div>

      {/* Generation Dropdown */}
      <div>
        <Label htmlFor="generation" className="text-sm font-medium">
          Generation
        </Label>
        <select
          id="generation"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={generationId}
          onChange={(e) => setGenerationId(e.target.value)}
          required>
          <option value="" disabled>
            Select Generation
          </option>
          {generations.map((gen: Generation) => (
            <option key={gen.id} value={gen.id}>
              {gen.name}
            </option>
          ))}
        </select>
      </div>

      {/* Description */}
      <div>
        <Label htmlFor="description" className="text-sm font-medium">
          Description
        </Label>
        <textarea
          id="description"
          //placeholder="Enter description"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>

      {/* Buttons (Optional) */}
      <div className="flex justify-between gap-2 mt-4">
        <Button variant="outline" type="button" onClick={onClose}>
          Cancel
        </Button>
        <SubmitButton loading={isPending}>{isPending ? "Creating..." : "Create"}</SubmitButton>
      </div>
    </form>
  );
}
