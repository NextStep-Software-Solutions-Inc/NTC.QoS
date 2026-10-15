"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createBandRequest } from "../handlers/createBandRequest";
import { type Band } from "../types/Band";

interface CreateBandFormProps {
  onCreated?: (band: Band) => void;
  onClose?: () => void;
}

const useCreateBand = createMutationHookFromTemplate(createBandRequest);

export default function CreateBandForm({ onCreated, onClose }: CreateBandFormProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  const { mutateAsync, isPending } = useCreateBand();
  const queryClient = useQueryClient();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    toast.promise(mutateAsync({ pathParams: {}, payload: { name, description } }), {
      loading: "Creating band...",
      success: (response) => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-bands" });
        onCreated?.(response.data);
        onClose?.();
        return response.message;
      },
      error: (error: unknown) => {
        console.error(error);
        return "Band already exists.";
      },
    });
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-xl">
      <div>
        <Label htmlFor="name" className="text-sm font-medium">
          Band Name
        </Label>
        <Input
          id="name"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </div>

      <div>
        <Label htmlFor="description" className="text-sm font-medium">
          Description
        </Label>
        <textarea
          id="description"
          className="flex min-h-[80px] w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>

      <div className="flex justify-between gap-2 mt-4">
        <Button variant="outline" type="button" onClick={onClose}>
          Cancel
        </Button>
        <SubmitButton loading={isPending} className="bg-blue-900 text-white hover:bg-blue-800" disabled={isPending}>
          {isPending ? "Creating..." : "Create"}
        </SubmitButton>
      </div>
    </form>
  );
}
