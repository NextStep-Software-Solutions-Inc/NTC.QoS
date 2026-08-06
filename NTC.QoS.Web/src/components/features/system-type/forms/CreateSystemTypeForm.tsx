import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { getTechnologiesRequest } from "@/components/features/technology/handlers/getTechnologiesRequest";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { getGenerationsRequest } from "../../generation/handlers/getGenerationsRequest";
import { createSystemTypeRequest } from "../handlers/createSystemTypeRequest";
import { type SystemType } from "../types/SystemType";

const useGetTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);
const useGetGenerations = createQueryHookFromTemplate(getGenerationsRequest);

interface CreateSystemTypeFormProps {
  onCreated?: (systemType: SystemType) => void;
  onClose?: () => void;
}

const useCreateSystemType = createMutationHookFromTemplate(createSystemTypeRequest);

export default function CreateSystemTypeForm({ onCreated, onClose }: CreateSystemTypeFormProps) {
  const [name, setName] = useState("");
  const [technologyId, setTechnologyId] = useState("");
  const [generationId, setGenerationId] = useState("");

  const { mutateAsync, isPending } = useCreateSystemType();
  const { data: technologiesResponse } = useGetTechnologies({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const technologies = technologiesResponse?.data ?? [];
  const { data: generationsResponse } = useGetGenerations({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const generations = generationsResponse?.data ?? [];

  const queryClient = useQueryClient();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!name || !technologyId || !generationId) {
      toast.error("Please fill in all required fields.");
      return;
    }

    toast.promise(mutateAsync({ pathParams: {}, payload: { name, technologyId, generationId } }), {
      loading: "Creating system type...",
      success: (response) => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-system-types" });
        onCreated?.(response.data);
        onClose?.();
        return response.message;
      },
      error: (error: unknown) => {
        console.error(error);
        return "Failed to create system type. Please try again.";
      },
    });
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-xl">
      {/* Name */}
      <div>
        <Label htmlFor="name" className="text-sm font-medium">
          Name
        </Label>
        <Input
          id="name"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </div>

      {/* Technology Dropdown */}
      <div>
        <Label htmlFor="technology">Technology</Label>
        <Select value={technologyId} onValueChange={setTechnologyId}>
          <SelectTrigger id="technology">
            <SelectValue placeholder="Select a technology" />
          </SelectTrigger>
          <SelectContent>
            {technologies.map((tech) => (
              <SelectItem key={tech.id} value={tech.id}>
                {tech.standard} ({tech.generation.name})
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Generation Dropdown */}
      <div>
        <Label htmlFor="generation">Generation</Label>
        <Select value={generationId} onValueChange={setGenerationId}>
          <SelectTrigger id="generation">
            <SelectValue placeholder="Select a generation" />
          </SelectTrigger>
          <SelectContent>
            {generations.map((gen) => (
              <SelectItem key={gen.id} value={gen.id}>
                {gen.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex justify-between gap-2 mt-4">
        <Button variant="outline" type="button" onClick={onClose}>
          Cancel
        </Button>
        <SubmitButton
          loading={isPending}
          disabled={!name || !technologyId || !generationId}
          className="bg-blue-900 text-white hover:bg-blue-800">
          {isPending ? "Creating..." : "Create"}
        </SubmitButton>
      </div>
    </form>
  );
}
