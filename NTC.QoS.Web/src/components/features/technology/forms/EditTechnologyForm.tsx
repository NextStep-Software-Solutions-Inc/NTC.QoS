"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { type Technology } from "@/components/features/technology/types/Technology";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editTechnologyRequest } from "../handlers/editTechnologyRequest";

interface EditTechnologyFormProps {
  onClose?: () => void;
  technology?: Technology;
}

const useEditTechnology = createMutationHookFromTemplate(editTechnologyRequest);

const EditTechnologyForm = ({ onClose, technology }: EditTechnologyFormProps) => {
  const [technologyStandard, setTechnologyStandard] = useState(technology?.standard ?? "");
  const [technologyDescription, setTechnologyDescription] = useState(technology?.description ?? "");

  const technologyId = technology?.id;

  const { mutateAsync, isPending } = useEditTechnology();
  const queryClient = useQueryClient();

  const handleSubmitEditTechnology = (e: React.FormEvent) => {
    e.preventDefault();

    toast.promise(
      mutateAsync({
        pathParams: { id: technologyId ?? "" },
        payload: { standard: technologyStandard, description: technologyDescription },
      }),
      {
        loading: "Updating technology...",
        success: () => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-technologies" });
          onClose?.();
          return "Technology updated successfully!";
        },
        error: "Error updating technology.",
      },
    );
  };

  return (
    <CardContent className="">
      <form onSubmit={handleSubmitEditTechnology} className=" h-full space-y-4 cursor-default">
        <div className="space-y-1"></div>
        <div className="space-y-1">
          <Label htmlFor="technology" className="text-sm font-medium">
            Technology Name
          </Label>
          <Input
            id="technology"
            type="text"
            placeholder={"Technology Name"}
            className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
            value={technologyStandard}
            onChange={(e) => setTechnologyStandard(e.target.value)}
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
            value={technologyDescription}
            onChange={(e) => setTechnologyDescription(e.target.value)}
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

export default EditTechnologyForm;
