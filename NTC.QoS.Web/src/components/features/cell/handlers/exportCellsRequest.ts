import { getAuthHeadersAsync, getBackendUrl } from "@/lib/api-utils";

export type CellExportFormat = "Excel" | "Pdf";

export interface ExportCellsParams {
  format: CellExportFormat;
  search?: string;
  status?: string;
  isVerified?: boolean;
  serviceProviderIds?: string[];
  provinceIds?: string[];
  municipalityIds?: string[];
  barangayIds?: string[];
  regionIds?: string[];
}

export async function exportCellsRequest(params: ExportCellsParams): Promise<void> {
  const query = new URLSearchParams();
  query.set("format", params.format);

  if (params.search) {
    query.set("search", params.search);
  }
  if (params.status) {
    query.set("status", params.status);
  }
  if (typeof params.isVerified === "boolean") {
    query.set("isVerified", String(params.isVerified));
  }

  params.serviceProviderIds?.forEach((id) => query.append("serviceProviderIds", id));
  params.provinceIds?.forEach((id) => query.append("provinceIds", id));
  params.municipalityIds?.forEach((id) => query.append("municipalityIds", id));
  params.barangayIds?.forEach((id) => query.append("barangayIds", id));
  params.regionIds?.forEach((id) => query.append("regionIds", id));

  const backendUrl = getBackendUrl();
  const authHeaders = await getAuthHeadersAsync();

  const response = await fetch(`${backendUrl}/cells/export?${query.toString()}`, {
    method: "GET",
    headers: {
      Accept: "application/octet-stream",
      ...authHeaders,
    },
  });

  if (!response.ok) {
    const errorText = await response.text().catch(() => "Unknown error");
    throw new Error(`Export failed: ${response.status} ${response.statusText}. ${errorText}`);
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;

  const contentDisposition = response.headers.get("content-disposition");
  let filename = `CellSites_Export_${new Date().toISOString().slice(0, 10)}.${params.format === "Pdf" ? "pdf" : "xlsx"}`;
  if (contentDisposition) {
    const filenameMatch = contentDisposition.match(/filename\*?=(?:UTF-8'')?["']?([^"';\n]+)["']?/i);
    if (filenameMatch?.[1]) {
      filename = decodeURIComponent(filenameMatch[1]);
    }
  }

  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}
