"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editMunicipalityRequest } from "../handlers/editMunicipalityRequest";
import { type Municipality } from "../types/Municipality";

interface EditMunicipalFormProps {
  onClose?: () => void;
  municipal?: Municipality;
}

const useEditMunicipal = createMutationHookFromTemplate(editMunicipalityRequest);

const EditMunicipalForm = ({ onClose, municipal }: EditMunicipalFormProps) => {
  const [municipalName, setMunicipalName] = useState(municipal?.name ?? "");
  const [municipalDescription, setMunicipalDescription] = useState(municipal?.description ?? "");

  const municipalId = municipal?.id;

  const { mutateAsync, isPending } = useEditMunicipal();
  const queryClient = useQueryClient();

  const handleSubmitEditMunicipal = (e: React.FormEvent) => {
    e.preventDefault();

    const payload = { name: municipalName, description: municipalDescription };
    toast.promise(mutateAsync({ pathParams: { id: municipalId ?? "" }, payload }), {
      loading: "Updating municipal...",
      success: () => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-municipals" });
        onClose?.();
        return "Municipality updated successfully!";
      },
      error: "Error updating municipality.",
    });
  };

  return (
    <CardContent className="">
      <form onSubmit={handleSubmitEditMunicipal} className=" h-full space-y-4 cursor-default">
        <div className="space-y-1">{/* <h1 className="text-lg font-semibold"> {municipality?.name}</h1> */}</div>
        <div className="space-y-1">
          <Label htmlFor="municipal">Municipal Name</Label>
          <Input
            id="municipal"
            type="text"
            placeholder={"Municipal Name"}
            value={municipalName}
            onChange={(e) => setMunicipalName(e.target.value)}
          />
        </div>

        <div className="space-y-1">
          <Label htmlFor="description">Description</Label>
          <Input
            id="description"
            type="text"
            placeholder={"Description"}
            value={municipalDescription}
            onChange={(e) => setMunicipalDescription(e.target.value)}
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

export default EditMunicipalForm;
