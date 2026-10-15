"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { type Band } from "@/components/features/band/types/Band";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editBandRequest } from "../handlers/editBandRequest";

interface EditBandFormProps {
  onClose?: () => void;
  band?: Band;
}

const useEditBand = createMutationHookFromTemplate(editBandRequest);

const EditBandForm = ({ onClose, band }: EditBandFormProps) => {
  const [bandName, setBandName] = useState(band?.name ?? "");
  const [bandDescription, setBandDescription] = useState(band?.description ?? "");

  const bandId = band?.id;

  const { mutateAsync, isPending } = useEditBand();
  const queryClient = useQueryClient();

  const handleSubmitEditBand = (e: React.FormEvent) => {
    e.preventDefault();

    toast.promise(
      mutateAsync({ pathParams: { id: bandId ?? "" }, payload: { name: bandName, description: bandDescription } }),
      {
        loading: "Updating band...",
        success: () => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-bands" });
          onClose?.();
          return "Band updated successfully!";
        },
        error: "Error updating band.",
      },
    );
  };

  return (
    <CardContent className="">
      <form onSubmit={handleSubmitEditBand} className=" h-full space-y-4 cursor-default">
        <div className="space-y-1"></div>
        <div className="space-y-1">
          <Label htmlFor="band" className="text-sm font-medium">
            Band Name
          </Label>
          <Input
            id="band"
            type="text"
            placeholder={"Band Name"}
            className="mt-1 w-full rounded border border-gray-300 p-2 text-sm font-medium"
            value={bandName}
            onChange={(e) => setBandName(e.target.value)}
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
            value={bandDescription}
            onChange={(e) => setBandDescription(e.target.value)}
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

export default EditBandForm;
