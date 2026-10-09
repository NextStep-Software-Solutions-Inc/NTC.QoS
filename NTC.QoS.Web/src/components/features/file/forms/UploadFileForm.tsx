import { useState } from "react";
import { useParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { ChevronDown, Loader2 } from "lucide-react";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { DialogClose, DialogFooter } from "@/components/ui/dialog";
import { Form } from "@/components/ui/form";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { getAuthHeaders, getBackendUrl } from "@/lib/api-utils";
import { getMunicipalitiesRequest } from "../../municipality/handlers/getMunicipalitiesRequest";
import { getProvincesRequest } from "../../province/handlers/getProvincesRequest";
import { type PreSignedUrl } from "@/components/features/file/types/PreSignedUrl";
import { request } from "@/services/fetch";
import { type APIResponse } from "@/types/reusable/api";
import { uploadFileToAzure } from "../handlers/uploadFileToAzure";
import { uploadFileRequest } from "../handlers/uploadFileRequest";
import UploadedFilesContent from "./UploadedFilesContent";

const useUploadFile = createMutationHookFromTemplate(uploadFileRequest);
const useGetProvinces = createQueryHookFromTemplate(getProvincesRequest);
const useGetMunicipalities = createQueryHookFromTemplate(getMunicipalitiesRequest);

const uploadFileSchema = z.object({
  fileName: z.string().trim().optional(),
  type: z.string().trim().min(3, { message: "File type must be at least 3 characters long" }),
  regionId: z.string({ required_error: "Region ID is required", invalid_type_error: "Region ID must be a string" }),
});

interface UploadFileFormProps {
  onSuccess?: () => void;
  onClose: () => void;
  type: "Speedtest" | "NAP" | "Cell" | "Cellular" | "CellularCoverage" | "WirelessValidation";
}

export default function UploadFileForm({ onClose, onSuccess, type }: UploadFileFormProps) {
  const { region } = useParams<{ region: string }>();
  const [files, setFiles] = useState<File[]>([]);
  const [isDownloading, setIsDownloading] = useState(false);
  const [selectedProvinceId, setSelectedProvinceId] = useState<string>("");
  const [selectedMunicipalityId, setSelectedMunicipalityId] = useState<string>("");

  // Format type for display with proper spacing
  const displayType = type === "CellularCoverage" 
    ? "Cellular Coverage" 
    : type === "WirelessValidation" 
    ? "Wireless Validation" 
    : type;

  const { mutateAsync, isPending } = useUploadFile();
  const queryClient = useQueryClient();

  // Fetch provinces for the current region (for WirelessValidation)
  const { data: provincesData, isLoading: isLoadingProvinces, error: provincesError } = useGetProvinces({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, regionIds: region ? [region] : [] },
    enabled: type === "WirelessValidation" && !!region,
  });

  // Fetch municipalities for the selected province (for WirelessValidation)
  const { data: municipalitiesData, isLoading: isLoadingMunicipalities, error: municipalitiesError } = useGetMunicipalities({
    pathParams: {},
    queryParams: { pageSize: 1000, pageNumber: 1, provinceIds: selectedProvinceId ? [selectedProvinceId] : [] },
    enabled: type === "WirelessValidation" && !!selectedProvinceId,
  });

  const form = useForm<z.infer<typeof uploadFileSchema>>({
    resolver: zodResolver(uploadFileSchema),
    progressive: true,
    mode: "all",
    values: { fileName: "", type, regionId: region },
  });

  const onSuccessHandler = () => {
    onSuccess?.();
    onClose?.();
  };

  const handleCancel = () => {
    void queryClient.cancelQueries({ queryKey: ["upload-file"] });
    onClose?.();
  };

  const handleSubmit = form.handleSubmit(async (values) => {
    for (const file of files) {
      toast.promise(
        (async () => {
          // Step 1 & 2: Attempt direct Pre-Signed Upload SAS URL to Azure Storage
          try {
            const presignedRes = await request.get<APIResponse<PreSignedUrl>>(
              `/files/presigned-upload-url?fileName=${encodeURIComponent(file.name)}&fileType=${encodeURIComponent(values.type)}&regionId=${encodeURIComponent(values.regionId)}`
            );

            if (presignedRes.succeeded && presignedRes.data?.url) {
              await uploadFileToAzure(presignedRes.data.url, file);
            }
          } catch (azureErr) {
            // Safely extract error message without exposing SAS tokens or query parameters
            const rawMessage = azureErr instanceof Error ? azureErr.message : "Direct upload failed";
            const sanitizedMessage = rawMessage.replace(/([?&][\w-]+)=([^&\s"']+)/g, "$1=[REDACTED]");

            if (process.env.NODE_ENV === "development") {
              console.warn("Direct blob upload fallback triggered:", sanitizedMessage);
            }
          }

          // Step 3: Send file to backend to ensure persistence and queue processing job
          const formData = new FormData();
          formData.append("type", values.type);
          formData.append("file", file);
          formData.append("regionId", values.regionId);
          const res = await mutateAsync({ pathParams: {}, payload: formData });

          onSuccessHandler();
          return res.message;
        })(),
        {
          loading: "Uploading file securely to cloud storage...",
          success: (msg) => (typeof msg === "string" ? msg : "File uploaded successfully."),
          error: (error) => extractErrorMessage(error) ?? "Failed to upload file.",
        }
      );
    }
  });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newFiles = e.target.files ? Array.from(e.target.files) : [];
    const validFiles = newFiles.filter((file) => file.size <= 25 * 1024 * 1024); // 25MB
    const oversizedFiles = newFiles.filter((file) => file.size > 25 * 1024 * 1024);

    if (oversizedFiles.length > 0) {
      alert("Some files exceed 25MB and were not added.");
    }
    setFiles((prev) => [...prev, ...validFiles]);
  };

  const handleRemoveFile = (index: number) => {
    setFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const provinces = provincesData?.data ?? [];
  const municipalities = municipalitiesData?.data ?? [];

  if (process.env.NODE_ENV === "development") {
    console.log("Province & Municipality data:", {
      provinces: provinces.length,
      municipalities: municipalities.length,
      isLoadingProvinces,
      isLoadingMunicipalities,
      hasProvinceError: !!provincesError,
      hasMunicipalityError: !!municipalitiesError,
      regionId: region,
      selectedProvinceId,
      type,
    });
  }

  // Check if download is enabled
  const isDownloadEnabled = type !== "WirelessValidation" || selectedMunicipalityId !== "";

  const handleDownloadTemplate = async () => {
    if (!isDownloadEnabled || isDownloading) return;

    setIsDownloading(true);
    try {
      const backendUrl = getBackendUrl();
      const endpoint =
        type === "WirelessValidation" && selectedMunicipalityId
          ? `${backendUrl}/files/download/wireless-validation/${selectedMunicipalityId}`
          : `${backendUrl}/files/download/${type}`;

      const response = await fetch(endpoint, {
        method: "GET",
        headers: {
          Accept: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          ...getAuthHeaders(),
        },
      });

      if (!response.ok) {
        let errorMessage = `Download failed with status ${response.status}`;
        try {
          const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
          if (contentType.includes("json")) {
            const jsonError = await response.json();
            errorMessage = extractErrorMessage(jsonError, errorMessage);
          } else {
            const textError = await response.text();
            if (textError && textError.length < 200) {
              errorMessage = textError;
            }
          }
        } catch {
          // ignore error parsing failure
        }
        throw new Error(errorMessage);
      }

      // Extract filename from Content-Disposition header if available
      let filename = `${type}_template-file.xlsx`;
      const disposition = response.headers.get("content-disposition");
      if (disposition && disposition.includes("filename=")) {
        const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(disposition);
        if (matches?.[1]) {
          filename = matches[1].replace(/['"]/g, "").trim();
        }
      } else if (type === "WirelessValidation" && selectedMunicipalityId) {
        const selectedMuni = municipalities.find((m) => m.id === selectedMunicipalityId);
        const muniName = selectedMuni?.name ? selectedMuni.name.replace(/\s+/g, "_") : "unknown";
        filename = `wireless_validation_template-${muniName}.xlsx`;
      }

      // Ensure .xlsx extension
      if (!filename.toLowerCase().endsWith(".xlsx")) {
        filename += ".xlsx";
      }

      const blob = await response.blob();
      const blobUrl = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = blobUrl;
      link.download = filename;

      try {
        document.body.appendChild(link);
        link.click();
      } finally {
        if (link.parentNode) {
          link.parentNode.removeChild(link);
        }
        window.URL.revokeObjectURL(blobUrl);
      }

      toast.success(`${displayType} template downloaded successfully`);
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Failed to download template file");
    } finally {
      setIsDownloading(false);
    }
  };

  return (
    <Form {...form}>
      {/* Province and Municipality Selectors for Wireless Validation */}
      {type === "WirelessValidation" && (
        <div className="mb-4 space-y-4">
          {/* Province Selector */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Select Province <span className="text-red-500">*</span>
            </label>
            <div className="relative">
              <select
                value={selectedProvinceId}
                onChange={(e) => {
                  setSelectedProvinceId(e.target.value);
                  setSelectedMunicipalityId(""); // Reset municipality when province changes
                }}
                disabled={isLoadingProvinces || provinces.length === 0}
                className="w-full h-10 px-3 py-2 text-sm border border-input rounded-md bg-background appearance-none pr-8 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50">
                <option value="">
                  {isLoadingProvinces
                    ? "Loading provinces..."
                    : provinces.length === 0
                      ? "No provinces available"
                      : "Select a province..."}
                </option>
                {provinces.map((province) => (
                  <option key={province.id} value={province.id}>
                    {province.name}
                  </option>
                ))}
              </select>
              <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
            </div>
            {!!provincesError && (
              <p className="text-xs text-red-500">
                Error loading provinces. Please try again.
              </p>
            )}
            {!isLoadingProvinces && provinces.length === 0 && !provincesError && (
              <p className="text-xs text-amber-600">
                No provinces found for this region.
              </p>
            )}
          </div>

          {/* Municipality Selector - Only show when province is selected */}
          {selectedProvinceId && (
            <div className="space-y-2">
              <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                Select Municipality <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <select
                  value={selectedMunicipalityId}
                  onChange={(e) => setSelectedMunicipalityId(e.target.value)}
                  disabled={isLoadingMunicipalities || municipalities.length === 0}
                  className="w-full h-10 px-3 py-2 text-sm border border-input rounded-md bg-background appearance-none pr-8 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50">
                  <option value="">
                    {isLoadingMunicipalities
                      ? "Loading municipalities..."
                      : municipalities.length === 0
                        ? "No municipalities available"
                        : "Select a municipality..."}
                  </option>
                  {municipalities.map((municipality) => (
                    <option key={municipality.id} value={municipality.id}>
                      {municipality.name}
                    </option>
                  ))}
                </select>
                <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
              </div>
              {!!municipalitiesError && (
                <p className="text-xs text-red-500">
                  Error loading municipalities. Please try again.
                </p>
              )}
              {!isLoadingMunicipalities && municipalities.length === 0 && !municipalitiesError && (
                <p className="text-xs text-amber-600">
                  No municipalities found for this province.
                </p>
              )}
              {selectedMunicipalityId === "" && municipalities.length > 0 && (
                <p className="text-xs text-muted-foreground">
                  Please select a municipality to download the template file.
                </p>
              )}
            </div>
          )}
        </div>
      )}

      {/* Download Template Section */}
      <div className="mb-4 p-3 rounded-lg bg-gradient-to-r from-blue-50 to-cyan-50 dark:from-blue-950/20 dark:to-cyan-950/20 border border-blue-200 dark:border-blue-800">
        {isDownloadEnabled ? (
          <button
            type="button"
            disabled={isDownloading}
            onClick={handleDownloadTemplate}
            className="text-sm font-semibold text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300 hover:underline flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed">
            {isDownloading ? (
              <Loader2 className="h-4 w-4 animate-spin text-blue-600 dark:text-blue-400" />
            ) : (
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
            )}
            {isDownloading ? `Downloading ${displayType} template...` : `Download ${displayType} template file`}
          </button>
        ) : (
          <div className="text-sm font-semibold text-gray-400 dark:text-gray-500 flex items-center gap-2 cursor-not-allowed">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
            Download {displayType} template file (select municipality first)
          </div>
        )}
      </div>
      <form onSubmit={handleSubmit}>
        <UploadedFilesContent files={files} handleRemoveFile={handleRemoveFile} handleFileChange={handleFileChange} />
        <DialogFooter className="flex items-center justify-between sm:justify-between">
          <DialogClose asChild>
            <Button variant="ghost" onClick={handleCancel}>
              Cancel
            </Button>
          </DialogClose>
          <SubmitButton loading={isPending} disabled={!form.formState.isValid} type="submit">
            Upload
          </SubmitButton>
        </DialogFooter>
      </form>
    </Form>
  );
}
