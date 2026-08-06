import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createPermissionRequest } from "../handlers/createPermissionRequest";
import { type Permission } from "../types/Permission";

interface CreatePermissionFormProps {
  onCreated?: (permission: Permission) => void;
  onClose?: () => void;
}

const useCreatePermission = createMutationHookFromTemplate(createPermissionRequest);

export default function CreatePermissionForm({ onCreated, onClose }: CreatePermissionFormProps) {
  const [name, setName] = useState("");
  const [key, setKey] = useState("");
  const [description, setDescription] = useState("");

  const { mutateAsync, isPending } = useCreatePermission();
  const queryClient = useQueryClient();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    toast.promise(mutateAsync({ pathParams: {}, payload: { name, key, description } }), {
      loading: "Creating permission...",
      success: (response) => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-permissions" });
        onCreated?.(response.data);
        onClose?.();
        return response.message;
      },
      error: (error: unknown) => {
        console.error(error);
        return "Failed to create permission.";
      },
    });
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-xl">
      <div>
        <Label htmlFor="name" className="text-sm font-medium">
          Permission Name
        </Label>
        <Input id="name" className="mt-1" value={name} onChange={(e) => setName(e.target.value)} required />
      </div>

      <div>
        <Label htmlFor="key" className="text-sm font-medium">
          Permission Key
        </Label>
        <Input
          id="key"
          className="mt-1"
          value={key}
          onChange={(e) => setKey(e.target.value)}
          placeholder="e.g., create_user, edit_role"
          required
        />
      </div>

      <div>
        <Label htmlFor="description" className="text-sm font-medium">
          Description
        </Label>
        <textarea
          id="description"
          className="mt-1 w-full rounded border border-gray-300 p-2 text-sm"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={3}
        />
      </div>

      <div className="flex justify-between gap-2 mt-4">
        <Button variant="outline" type="button" onClick={onClose}>
          Cancel
        </Button>
        <SubmitButton loading={isPending}>{isPending ? "Creating..." : "Create"}</SubmitButton>
      </div>
    </form>
  );
}
