import React from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { type Municipality } from "../../municipality/types/Municipality";
import { createBarangayRequest } from "../handlers/createBarangayRequest";

const useCreateBarangay = createMutationHookFromTemplate(createBarangayRequest);

interface AddBarangayFormProps {
  municipality?: Municipality;
  municipalityId?: string;
  onOpenChange?: (open: boolean) => void;
}

const AddBarangayForm = ({ municipality, municipalityId, onOpenChange }: AddBarangayFormProps) => {
  const [barangayName, setBarangayName] = React.useState("");
  const [barangayDescription, setBarangayDescription] = React.useState("");
  const [code, setCode] = React.useState("");
  const [area, setArea] = React.useState("");
  const [boundariesFile, setBoundariesFile] = React.useState<File | string>("");

  const queryClient = useQueryClient();
  const { mutateAsync } = useCreateBarangay();

  const handleSubmitAddBarangay = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      name: barangayName,
      description: barangayDescription,
      municipalityId: municipalityId ?? "",
      code: code,
      area: area ? parseFloat(area) : undefined,
      // Note: boundariesFile is not in the typed payload - may need backend API update
    };

    toast.promise(mutateAsync({ pathParams: {}, payload }), {
      loading: "Adding barangay...",
      success: () => {
        void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-barangays" });
        onOpenChange?.(false);
        return "Barangay added successfully!";
      },
      error: "Error adding barangay.",
    });
  };

  return (
    <form onSubmit={handleSubmitAddBarangay} className="space-y-4">
      <p className="text-sm font-semibold">
        Municipality: <Badge>{municipality?.name}</Badge>
      </p>
      <div className="space-y-1">
        <Label htmlFor="barangay">Name</Label>
        <Input
          id="barangay"
          type="text"
          placeholder="Barangay Name"
          value={barangayName}
          onChange={(e) => setBarangayName(e.target.value)}
        />
      </div>

      <div className="space-y-1">
        <Label htmlFor="description">Description</Label>
        <Input
          id="description"
          type="text"
          placeholder="Description"
          value={barangayDescription}
          onChange={(e) => setBarangayDescription(e.target.value)}
        />
      </div>

      <div className="space-y-1">
        <Label htmlFor="code">Code</Label>
        <Input id="code" type="text" placeholder="Code" value={code} onChange={(e) => setCode(e.target.value)} />
      </div>

      <div className="space-y-1">
        <Label htmlFor="area">Area (sq. meters)</Label>
        <Input
          id="area"
          type="number"
          step="any"
          placeholder="Area"
          value={area}
          onChange={(e) => setArea(e.target.value)}
        />
      </div>

      <div className="space-y-1">
        <Label htmlFor="boundariesFile">Boundaries</Label>
        <Input
          id="boundariesFile"
          type="file"
          accept=".geojson,application/geo+json"
          onChange={(e) => setBoundariesFile(e.target.files?.[0] ?? "")}
        />
      </div>

      <div className="flex justify-between pt-4">
        <Button type="button" variant="ghost" onClick={() => onOpenChange?.(false)}>
          Cancel
        </Button>
        <SubmitButton disabled={!barangayName || !barangayDescription || !municipalityId}>Add Barangay</SubmitButton>
      </div>
    </form>
  );
};

export default AddBarangayForm;
