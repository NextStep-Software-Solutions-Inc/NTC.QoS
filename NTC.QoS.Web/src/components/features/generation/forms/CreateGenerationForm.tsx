import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createGenerationRequest } from "../handlers/createGenerationRequest";
import { type Generation } from "../types/Generation";

interface CreateGenerationFormProps {
  onCreated?: (generation: Generation) => void;
  onClose?: () => void;
}

const useCreateGeneration = createMutationHookFromTemplate(createGenerationRequest);

export default function CreateGenerationForm({ onCreated, onClose }: CreateGenerationFormProps) {
  const [generationName, setGenerationName] = useState("");
  const [description, setDescription] = useState("");

  const { mutateAsync, isPending } = useCreateGeneration();

  const queryClient = useQueryClient();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    toast.promise(mutateAsync({ pathParams: {}, payload: { name: generationName, description } }), {
      loading: "Creating generation...",
      success: (response) => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-generations" });
        onCreated?.(response.data);
        onClose?.();
        return response.message;
      },
      error: (error: unknown) => {
        console.error(error);
        return "Generation already exists.";
      },
    });
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-xl">
      {/* Generation Name */}
      <div>
        <Label htmlFor="generationName" className="text-sm font-medium">
          Generation Name
        </Label>
        <Input
          id="generationName"
          //placeholder="Enter generation name"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={generationName}
          onChange={(e) => setGenerationName(e.target.value)}
        />
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
