import { useState } from "react";
import { useParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { ArrowRight, ChevronDown, Download, Layers, Loader2 } from "lucide-react";

import SubmitButton from "@/components/SubmitButton";
import { Button } from "@/components/ui/button";
import { DialogClose, DialogFooter } from "@/components/ui/dialog";
import { Form } from "@/components/ui/form";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { getAuthHeadersAsync, getBackendUrl } from "@/lib/api-utils";
import { getMunicipalitiesRequest } from "../../municipality/handlers/getMunicipalitiesRequest";
import { getProvincesRequest } from "../../province/handlers/getProvincesRequest";
import { type PreSignedUrl } from "@/components/features/file/types/PreSignedUrl";
import { request } from "@/services/fetch";
import { type APIResponse } from "@/types/reusable/api";
import { uploadFileToAzure } from "../handlers/uploadFileToAzure";
import { uploadFileRequest } from "../handlers/uploadFileRequest";
import { type InitTilesetIngestResult } from "../../cellular-coverage/types/CellularCoverageTileset";
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
  const [isDownloadingTool, setIsDownloadingTool] = useState(false);
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
      const isMbtiles = file.name.toLowerCase().endsWith(".mbtiles");

      if (isMbtiles) {
        // Direct MapTiler Ingestion Flow for MBTiles
        toast.promise(
          (async () => {
            // 1. Request MapTiler Ingest Session
            const initRes = await request.post<APIResponse<InitTilesetIngestResult>>(
              "/cellular-coverages/tilesets/init-upload",
              {
                fileName: file.name,
                fileSize: file.size,
                displayName: file.name.replace(/\.[^/.]+$/, ""),
                regionId: values.regionId || undefined,
              }
            );

            if (!initRes.succeeded || !initRes.data?.uploadUrl) {
              throw new Error(initRes.message ?? "Failed to initialize MapTiler upload session");
            }

            const { tilesetId, uploadUrl } = initRes.data;

            // 2. Direct binary upload to MapTiler storage
            const uploadRes = await fetch(uploadUrl, {
              method: "PUT",
              body: file,
              headers: {
                "Content-Type": "application/octet-stream",
              },
            });

            if (!uploadRes.ok) {
              throw new Error(`Failed to upload to MapTiler storage (Status ${uploadRes.status})`);
            }

            // 3. Notify backend to trigger server-side processing & start polling
            const processRes = await request.post<APIResponse<boolean>>(
              `/cellular-coverages/tilesets/${tilesetId}/process`,
              {}
            );

            if (!processRes.succeeded) {
              throw new Error(processRes.message ?? "Failed to start tileset generation");
            }

            onSuccessHandler();
            return "MBTiles uploaded to MapTiler. Tileset generation has been queued.";
          })(),
          {
            loading: `Uploading ${file.name} directly to MapTiler Cloud...`,
            success: (msg) => (typeof msg === "string" ? msg : "MBTiles uploaded successfully."),
            error: (error) => extractErrorMessage(error) ?? "Failed to upload MBTiles file.",
          }
        );
        continue;
      }

      // Legacy / Excel upload flow
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

  const isCellularCoverage = type === "CellularCoverage";
  const maxFileSizeBytes = isCellularCoverage ? 1024 * 1024 * 1024 : 25 * 1024 * 1024; // 1 GB vs 25 MB
  const maxFileSizeDisplay = isCellularCoverage ? "1 GB" : "25 MB";

  const handleFilesAdded = (incomingFiles: File[]) => {
    const validFiles = incomingFiles.filter((file) => file.size <= maxFileSizeBytes);
    const oversizedFiles = incomingFiles.filter((file) => file.size > maxFileSizeBytes);

    if (oversizedFiles.length > 0) {
      toast.error(`Some files exceed ${maxFileSizeDisplay} and were not added.`);
    }
    setFiles((prev) => [...prev, ...validFiles]);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      handleFilesAdded(Array.from(e.target.files));
      e.target.value = "";
    }
  };

  const handleDownloadConversionTool = async () => {
    setIsDownloadingTool(true);
    try {
      const backendUrl = getBackendUrl();
      const endpoint = `${backendUrl}/files/download/converter-tool`;
      const authHeaders = await getAuthHeadersAsync();
      const response = await fetch(endpoint, {
        method: "GET",
        headers: {
          ...authHeaders,
        },
      });

      if (!response.ok) {
        toast.info("Conversion Tool executable will be downloadable here once finalized by the tools team.", {
          description: "Supported formats right now: .mbtiles, .geojson, .gpkg",
          duration: 5000,
        });
        return;
      }

      const blob = await response.blob();
      const blobUrl = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = blobUrl;
      link.download = "ntc-rp3-converter.zip";
      document.body.appendChild(link);
      link.click();
      if (link.parentNode) {
        link.parentNode.removeChild(link);
      }
      window.URL.revokeObjectURL(blobUrl);
      toast.success("Converter tool downloaded successfully");
    } catch {
      toast.info("Conversion Tool will be available once the converter build is completed by the tools team.", {
        description: "You can upload existing .mbtiles or .geojson files directly.",
        duration: 5000,
      });
    } finally {
      setIsDownloadingTool(false);
    }
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

      const authHeaders = await getAuthHeadersAsync();
      const response = await fetch(endpoint, {
        method: "GET",
        headers: {
          Accept: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          ...authHeaders,
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

      {/* RP3 CSV to MBTiles Conversion Tool Card (Cellular Coverage only) */}
      {isCellularCoverage && (
        <div className="mb-4 rounded-xl border border-violet-200/80 dark:border-violet-800/60 bg-gradient-to-br from-violet-50/70 via-purple-50/40 to-indigo-50/30 dark:from-violet-950/30 dark:via-purple-950/20 dark:to-indigo-950/10 p-4 shadow-sm transition-all">
          <div className="flex flex-col gap-3">
            {/* Header row with icon, title, badge, and download action */}
            <div className="flex items-center justify-between gap-3 flex-wrap">
              <div className="flex items-center gap-2.5">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-violet-600/10 dark:bg-violet-400/15 text-violet-600 dark:text-violet-400 shrink-0">
                  <Layers className="h-5 w-5" />
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100">
                      RP3 CSV to MBTiles Converter
                    </h4>
                    <span className="rounded-full bg-violet-100 dark:bg-violet-900/60 px-2 py-0.5 text-[10px] font-semibold text-violet-700 dark:text-violet-300">
                      Recommended
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Convert raw signal drive-tests locally into high-performance vector tiles
                  </p>
                </div>
              </div>

              <Button
                type="button"
                size="sm"
                onClick={handleDownloadConversionTool}
                disabled={isDownloadingTool}
                className="bg-violet-600 hover:bg-violet-700 text-white shadow-sm font-medium h-8 px-3 text-xs gap-1.5 transition-all">
                {isDownloadingTool ? (
                  <>
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    Downloading...
                  </>
                ) : (
                  <>
                    <Download className="h-3.5 w-3.5" />
                    Download Converter Tool
                  </>
                )}
              </Button>
            </div>

            {/* Stepper workflow visualization */}
            <div className="flex items-center justify-between gap-2 pt-2 border-t border-violet-100 dark:border-violet-900/40 text-[11px]">
              <div className="flex items-center gap-1.5 text-violet-900 dark:text-violet-200 font-medium">
                <span className="flex h-4 w-4 items-center justify-center rounded-full bg-violet-200 dark:bg-violet-800 text-[10px] font-bold">1</span>
                <span>Convert CSV locally</span>
              </div>
              <ArrowRight className="h-3 w-3 text-violet-400 shrink-0" />
              <div className="flex items-center gap-1.5 text-violet-900 dark:text-violet-200 font-medium">
                <span className="flex h-4 w-4 items-center justify-center rounded-full bg-violet-200 dark:bg-violet-800 text-[10px] font-bold">2</span>
                <span>Upload .mbtiles here</span>
              </div>
              <ArrowRight className="h-3 w-3 text-violet-400 shrink-0" />
              <div className="flex items-center gap-1.5 text-violet-900 dark:text-violet-200 font-medium">
                <span className="flex h-4 w-4 items-center justify-center rounded-full bg-violet-200 dark:bg-violet-800 text-[10px] font-bold">3</span>
                <span>Render on Map</span>
              </div>
            </div>

            {/* Secondary template link for legacy CSV */}
            <div className="flex items-center justify-between text-[11px] pt-1 text-muted-foreground border-t border-violet-100/60 dark:border-violet-900/20">
              <span>Looking for raw data template?</span>
              <button
                type="button"
                onClick={handleDownloadTemplate}
                disabled={isDownloading}
                className="text-violet-600 hover:text-violet-700 dark:text-violet-400 hover:underline font-medium inline-flex items-center gap-1">
                {isDownloading ? <Loader2 className="h-3 w-3 animate-spin" /> : <Download className="h-3 w-3" />}
                Download CSV Template
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Download Template Section for Non-CellularCoverage */}
      {!isCellularCoverage && (
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
      )}

      <form onSubmit={handleSubmit}>
        <UploadedFilesContent
          type={type}
          files={files}
          handleRemoveFile={handleRemoveFile}
          handleFileChange={handleFileChange}
          onFilesAdded={handleFilesAdded}
        />
        <DialogFooter className="flex items-center justify-between sm:justify-between pt-2">
          <DialogClose asChild>
            <Button variant="ghost" onClick={handleCancel}>
              Cancel
            </Button>
          </DialogClose>
          <SubmitButton
            loading={isPending}
            disabled={!form.formState.isValid || files.length === 0}
            type="submit">
            {files.length > 0 ? `Upload ${files.length} File${files.length > 1 ? "s" : ""}` : "Upload"}
          </SubmitButton>
        </DialogFooter>
      </form>
    </Form>
  );
}
