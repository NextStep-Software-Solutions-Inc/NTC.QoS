import { useState } from "react";
import Image from "next/image";
import { AlertCircle } from "lucide-react";
import { toast } from "sonner";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { createProviderLogoRequest } from "../handlers/createProviderLogoRequest";
import { createProviderRequest } from "../handlers/createProviderRequest";
import { restoreProviderRequest } from "../handlers/restoreProviderRequest";
import { type Provider } from "../types/Provider";

interface CreateProviderFormProps {
  onCreated?: (provider: Provider) => void;
  onClose?: () => void;
}

/** Extracts a deleted provider's ID from the backend conflict message, if present. */
function extractDeletedProviderId(message: string): string | null {
  const match = message.match(/\/providers\/([0-9a-f-]{36})\/restore/i);
  return match?.[1] ?? null;
}

const useCreateProvider = createMutationHookFromTemplate(createProviderRequest);
const useCreateProviderLogo = createMutationHookFromTemplate(createProviderLogoRequest);
const useRestoreProvider = createMutationHookFromTemplate(restoreProviderRequest);

export default function CreateProviderForm({ onCreated, onClose }: CreateProviderFormProps) {
  const [providerName, setProviderName] = useState("");
  const [licenseNumber, setLicenseNumber] = useState("");
  const [providerColor, setProviderColor] = useState("#EF4444");
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoPreview, setLogoPreview] = useState<string | undefined>(undefined);
  const [deletedProviderId, setDeletedProviderId] = useState<string | null>(null);

  const { mutateAsync: createProvider, isPending } = useCreateProvider();
  const { mutateAsync: createProviderLogo } = useCreateProviderLogo();
  const { mutateAsync: restoreProvider, isPending: isRestoring } = useRestoreProvider();

  const handleLogoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setLogoFile(file);
      setLogoPreview(URL.createObjectURL(file));
    }
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setDeletedProviderId(null);

    toast.promise(
      createProvider({ pathParams: {}, payload: { name: providerName, licenseNumber, color: providerColor } }),
      {
        loading: "Creating provider...",

        success: async (res) => {
          onCreated?.(res.data);
          if (logoFile) {
            const formData = new FormData();
            formData.append("LogoFile", logoFile);
            await createProviderLogo({ pathParams: { id: res.data.id }, payload: formData });
          }
          return res.message;
        },
        error: (error: Error) => {
          const message = extractErrorMessage(error);
          const deletedId = extractDeletedProviderId(message);
          if (deletedId) {
            setDeletedProviderId(deletedId);
            return null; // suppress toast — inline alert handles this case
          }
          return message;
        },
      },
    );
  };

  const handleRestore = () => {
    if (!deletedProviderId) return;
    toast.promise(restoreProvider({ pathParams: { id: deletedProviderId } }), {
      loading: "Restoring provider...",
      success: (res) => {
        onCreated?.(res.data);
        onClose?.();
        return res.message;
      },
      error: (error: Error) => extractErrorMessage(error),
    });
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6 w-full max-w-md">
      {/* Provider Logo */}
      <div>
        <Label className="text-sm font-medium">Provider Logo</Label>
        <div className="flex items-center gap-4 mt-2">
          <div className="w-15 h-12 rounded-full bg-gray-200 flex items-center justify-center overflow-hidden">
            {logoPreview ? (
              <Image
                src={logoPreview}
                alt="Logo preview"
                width={60}
                height={48}
                className="object-cover rounded-full"
              />
            ) : (
              <span className="text-gray-500">No Logo</span>
            )}
          </div>
          <Input type="file" accept=".jpg,.jpeg,.png" onChange={handleLogoChange} />
        </div>
        <p className="text-xs text-gray-500 mt-1">
          Upload a logo or image for this provider. Recommended size: 200x200px
        </p>
      </div>

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

      {/* License */}
      <div>
        <Label htmlFor="licenseNumber" className="text-sm font-medium">
          License
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
      <div>
        <Label htmlFor="providerColor" className="text-sm font-medium">
          Provider Color
        </Label>
        <div className="flex items-center gap-2 mt-1">
          <input
            type="color"
            value={providerColor}
            onChange={(e) => setProviderColor(e.target.value)}
            className="w-10 h-9 rounded-md border cursor-pointer"
          />
          <Input value={providerColor} onChange={(e) => setProviderColor(e.target.value)} className="w-full" />
        </div>
      </div>

      {/* Restore deleted provider banner */}
      {deletedProviderId && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 p-4 text-destructive">
          <div className="flex items-start gap-3">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
            <div className="flex-1">
              <p className="text-sm font-semibold">Deleted provider already exists</p>
              <p className="mt-1 text-sm">
                A deleted provider with this name already exists. You can restore it instead of creating a new one.
              </p>
              <div className="mt-3 flex gap-2">
                <Button
                  type="button"
                  size="sm"
                  variant="destructive"
                  onClick={handleRestore}
                  disabled={isRestoring}>
                  {isRestoring ? "Restoring..." : "Restore Provider"}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  className="border-destructive/50 text-destructive hover:bg-destructive/10"
                  onClick={() => setDeletedProviderId(null)}>
                  Dismiss
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-end gap-2 mt-4">
        <Button variant="outline" type="button" onClick={onClose}>
          Cancel
        </Button>
        <SubmitButton loading={isPending}>{isPending ? "Creating..." : "Create"}</SubmitButton>
      </div>
    </form>
  );
}
