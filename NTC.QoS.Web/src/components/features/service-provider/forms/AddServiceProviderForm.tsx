import { useState } from "react";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createProviderRequest } from "../../provider/handlers/createProviderRequest";
import { type Provider } from "../../provider/types/Provider";

interface CreateProviderFormProps {
  onCreated?: (provider: Provider) => void;
}

const useCreateProvider = createMutationHookFromTemplate(createProviderRequest);

export default function AddServiceProviderForm({ onCreated }: CreateProviderFormProps) {
  const [providerName, setProviderName] = useState("");
  const [licenseNumber, setLicenseNumber] = useState("");
  const [providerColor, setProviderColor] = useState("#EF4444");
  const [logo, setLogo] = useState<string | undefined>(undefined);

  const { mutateAsync, isPending } = useCreateProvider();

  const handleLogoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = () => {
        setLogo(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    toast.promise(
      mutateAsync({ pathParams: {}, payload: { name: providerName, licenseNumber, color: providerColor, logo: logo } }),
      {
        loading: "Creating provider...",
        success: (response) => {
          onCreated?.(response.data);
          return response.message;
        },
        error: (error: unknown) => {
          console.error(error);
          return "Failed to create provider";
        },
      },
    );
  };

  return (
    <form onSubmit={handleSubmit} className="grid grid-cols-2 gap-6 max-w-xl">
      {/* Provider Name */}
      <div>
        <Label htmlFor="providerName" className="text-sm font-medium">
          Provider Name
        </Label>
        <Input
          id="providerName"
          placeholder="Enter provider name"
          className="mt-1"
          value={providerName}
          onChange={(e) => setProviderName(e.target.value)}
        />
      </div>

      {/* License Number */}
      <div>
        <Label htmlFor="licenseNumber" className="text-sm font-medium">
          License Number
        </Label>
        <Input
          id="licenseNumber"
          placeholder="Enter license number"
          className="mt-1"
          value={licenseNumber}
          onChange={(e) => setLicenseNumber(e.target.value)}
        />
      </div>

      {/* Provider Color */}
      <div className="">
        <Label htmlFor="providerColor" className="text-sm font-medium">
          Provider Color
        </Label>
        {/* Provider Color */}
        <div className="flex items-center gap-2">
          <input
            type="color"
            value={providerColor}
            onChange={(e) => setProviderColor(e.target.value)}
            className="w-10 h-9 rounded-md border-none outline-none"
          />
          <Input value={providerColor} onChange={(e) => setProviderColor(e.target.value)} className="w-full" />
        </div>
      </div>

      {/* Provider Logo */}
      <div className="">
        <Label htmlFor="licenseNumber" className="text-sm font-medium">
          License Number
        </Label>
        <section className="flex items-center gap-2">
          <div className="w-15 aspect-square rounded-full  bg-slate-500"></div>
          <label className="flex flex-col">
            <span className="sr-only">Upload a photo</span>
            <Input type="file" accept="image/*" onChange={handleLogoChange} />
          </label>
        </section>
      </div>

      {/* Buttons (Optional) */}
      <div className="col-span-2 flex justify-between gap-2 mt-4">
        <Button variant="outline" type="button">
          Cancel
        </Button>
        <SubmitButton loading={isPending}>{isPending ? "Creating..." : "Create Provider"}</SubmitButton>
      </div>
    </form>
  );
}
