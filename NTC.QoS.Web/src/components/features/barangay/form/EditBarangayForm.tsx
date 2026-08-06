"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { type Barangay } from "@/components/features/barangay/types/Barangay";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editBarangayRequest } from "../handlers/postEditBarangayRequest";

interface EditBarangayFormProps {
  onClose?: () => void;
  barangay?: Barangay;
}

const useEditBarangay = createMutationHookFromTemplate(editBarangayRequest);

const EditBarangayForm = ({ onClose, barangay }: EditBarangayFormProps) => {
  const [barangayName, setBarangayName] = useState(barangay?.name ?? "");
  const [barangayDescription, setBarangayDescription] = useState(barangay?.description ?? "");

  const barangayId = barangay?.id;

  const { mutateAsync, isPending } = useEditBarangay();
  const queryClient = useQueryClient();

  const handleSubmitEditBarangay = (e: React.FormEvent) => {
    e.preventDefault();

    toast.promise(
      mutateAsync({
        pathParams: { id: barangayId ?? "" },
        payload: { name: barangayName, description: barangayDescription },
      }),
      {
        loading: "Updating barangay...",
        success: () => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-barangays" });
          onClose?.();
          return "Barangay updated successfully!";
        },
        error: "Error updating barangay.",
      },
    );
  };

  return (
    <CardContent className="">
      <form onSubmit={handleSubmitEditBarangay} className=" h-full space-y-4 cursor-default">
        <div className="space-y-1">{/* <h1 className="text-lg font-semibold"> {municipality?.name}</h1> */}</div>
        <div className="space-y-1">
          <Label htmlFor="barangay">Barangay Name</Label>
          <Input
            id="barangay"
            type="text"
            placeholder={"Barangay Name"}
            value={barangayName}
            onChange={(e) => setBarangayName(e.target.value)}
          />
        </div>

        <div className="space-y-1">
          <Label htmlFor="description">Description</Label>
          <Input
            id="description"
            type="text"
            placeholder={"Description"}
            value={barangayDescription}
            onChange={(e) => setBarangayDescription(e.target.value)}
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

export default EditBarangayForm;
