"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { type SystemType } from "@/components/features/system-type/types/SystemType";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editSystemTypeRequest } from "../handlers/editSystemTypeRequest";

interface EditSystemTypeFormProps {
  onClose?: () => void;
  systemType?: SystemType;
}

const useEditSystemType = createMutationHookFromTemplate(editSystemTypeRequest);

const EditSystemTypeForm = ({ onClose, systemType }: EditSystemTypeFormProps) => {
  const [systemTypeName, setSystemTypeName] = useState(systemType?.name ?? "");

  const systemTypeId = systemType?.id;

  const { mutateAsync, isPending } = useEditSystemType();
  const queryClient = useQueryClient();

  const handleSubmitEditSystemType = (e: React.FormEvent) => {
    e.preventDefault();

    toast.promise(mutateAsync({ pathParams: { id: systemTypeId ?? "" }, payload: { name: systemTypeName } }), {
      loading: "Updating system type...",
      success: (response) => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-system-types" });
        onClose?.();
        return response.message;
      },
      error: (error: unknown) => {
        const errorMessage =
          error && typeof error === "object" && "response" in error
            ? (error.response as { data?: { message?: string } })?.data?.message
            : undefined;
        return errorMessage ?? "Error updating system type.";
      },
    });
  };

  return (
    <CardContent className="">
      <form onSubmit={handleSubmitEditSystemType} className=" h-full space-y-4 cursor-default">
        <div className="space-y-1"></div>
        <div className="space-y-1">
          <Label htmlFor="systemTypeName" className="text-sm font-medium">
            System Type Name
          </Label>
          <Input
            id="systemTypeName"
            type="text"
            placeholder={"System Type Name"}
            className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
            value={systemTypeName}
            onChange={(e) => setSystemTypeName(e.target.value)}
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

export default EditSystemTypeForm;
