import React, { forwardRef, useImperativeHandle, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { editProviderRequest } from "./handlers/editProviderRequest";
import { type Provider } from "./types/Provider";

export interface EditProviderFormHandles {
  submitForm: () => void;
}
interface EditProviderProps {
  provider: Provider;
  handleSuccess?: () => void;
}

export const EditProviderForm = forwardRef<EditProviderFormHandles, EditProviderProps>(
  ({ provider, handleSuccess }, ref) => {
    const [name, setName] = useState(provider.name);
    const [licenseNumber, setLicenseNumber] = useState(provider.licenseNumber);
    const [color, setColor] = useState(provider.color);
    const [logo, setLogo] = useState<string | undefined>(undefined);
    const formRef = useRef<HTMLFormElement | null>(null);

    const queryClient = useQueryClient();
    const useEditProvider = createMutationHookFromTemplate(editProviderRequest);
    const { mutateAsync } = useEditProvider();

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

    const handleSubmit = async (e?: React.FormEvent<HTMLFormElement>) => {
      if (e) e.preventDefault();
      toast.promise(mutateAsync({ pathParams: { id: provider.id }, payload: { name, licenseNumber, color, logo } }), {
        loading: "Editing provider...",
        success: (response) => {
          void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-providers" });
          handleSuccess?.();
          return `${response.data.name} Provider successfully edit`;
        },
        error: (error: unknown) => {
          console.error(error);
          return "Failed to edit provider";
        },
      });
    };

    useImperativeHandle(ref, () => ({
      submitForm: async () => {
        if (formRef.current) {
          await handleSubmit();
        }
      },
    }));

    return (
      <form ref={formRef} onSubmit={handleSubmit} className="grid sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        {/* Provider Name */}
        <div>
          <Label htmlFor="providerName" className="text-sm font-medium">
            Name
          </Label>
          <Input
            id="providerName"
            placeholder="Enter provider name"
            className="mt-1"
            value={name}
            onChange={(e) => setName(e.target.value)}
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
            Color
          </Label>
          {/* Provider Color */}
          <div className="flex items-center gap-2">
            <input
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              className="w-10 h-9 rounded-md border-none outline-none"
            />
            <Input value={color} onChange={(e) => setColor(e.target.value)} className="w-full" />
          </div>
        </div>

        {/* Provider Logo */}
        <div className="">
          <Label htmlFor="licenseNumber" className="text-sm font-medium">
            Logo
          </Label>
          <section className="flex items-center gap-2">
            <div className="w-15 aspect-square rounded-full  bg-slate-500"></div>
            <label className="flex flex-col">
              {/* <span className="sr-only">Upload a photo</span> */}
              <Input type="file" accept="image/*" onChange={handleLogoChange} />
            </label>
          </section>
        </div>
      </form>
    );
  },
);

EditProviderForm.displayName = "EditProviderForm";
