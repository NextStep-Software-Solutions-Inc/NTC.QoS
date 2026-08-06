"use client";

import React, { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { toast } from "sonner";

import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { DialogTrigger } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createMunicipalityRequest } from "../handlers/createMunicipalityRequest";

interface AddMunicipalDialogProps {
  provinceId?: string;
  open?: boolean;
  onChangeOpen?: (open: boolean) => void;
}

const useCreateMunicipal = createMutationHookFromTemplate(createMunicipalityRequest);

const AddMunicipalDialog = ({ provinceId }: AddMunicipalDialogProps) => {
  const [municipality, setMunicipality] = useState("");
  const [description, setDescription] = useState("");
  const [isCity, setIsCity] = useState(false);

  const { mutateAsync, isPending } = useCreateMunicipal();
  const queryClient = useQueryClient();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const payload = { provinceId: provinceId ?? "", name: municipality, description, isCity };

    toast.promise(mutateAsync({ pathParams: {}, payload }), {
      loading: "Creating municipality...",
      success: (response) => {
        setMunicipality("");
        setDescription("");
        setIsCity(false);
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "municipalities" });
        return response.message;
      },
      error: (error: unknown) => {
        console.error(error);
        return "Failed to create municipality";
      },
    });
  };

  return (
    <ResponsiveDialog
      title="Add Municipality"
      description="Add a new municipality"
      trigger={
        <Button variant="ghost" size="sm">
          <Plus />
        </Button>
      }>
      <form onSubmit={handleSubmit} className="h-full space-y-4 cursor-default">
        <div className="space-y-1">
          <Label htmlFor="municipality">Municipal Name</Label>
          <Input
            id="municipality"
            type="text"
            placeholder="Municipal Name"
            value={municipality}
            onChange={(e) => setMunicipality(e.target.value)}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="description">Description</Label>
          <Input
            id="description"
            type="text"
            placeholder="Add description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        <div className="space-y-1">
          <section className=" flex items-center space-x-3">
            <div className="flex items-center space-x-2">
              <Checkbox id="isCity-true" checked={isCity} onCheckedChange={(checked) => setIsCity(!!checked)} />
              <Label htmlFor="isCity-true">City?</Label>
            </div>
          </section>
        </div>
        <div className="flex justify-between pt-4">
          <DialogTrigger>
            <Button type="button" variant="ghost">
              Cancel
            </Button>
          </DialogTrigger>
          <SubmitButton loading={isPending} type="submit">
            {isPending ? "Creating..." : "Create"}
          </SubmitButton>
        </div>
      </form>
    </ResponsiveDialog>
  );
};

export default AddMunicipalDialog;
