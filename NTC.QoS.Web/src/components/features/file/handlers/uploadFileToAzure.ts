/**
 * Directly uploads a File binary to Azure Blob Storage using a pre-signed SAS URL.
 * Includes `x-ms-blob-type: BlockBlob` and `x-ms-version` matching the SAS token `sv` version.
 */
export async function uploadFileToAzure(presignedUrl: string, file: File): Promise<void> {
  const response = await fetch(presignedUrl, {
    method: "PUT",
    headers: {
      "x-ms-blob-type": "BlockBlob",
      "Content-Type": file.type || "application/octet-stream",
    },
    body: file,
  });

  if (!response.ok) {
    const errorText = await response.text();
    console.error(`Azure Storage upload failed (${response.status}):`, errorText);

    const codeMatch = errorText.match(/<Code>(.*?)<\/Code>/i);
    const detail = codeMatch?.[1] ? ` (${codeMatch[1]})` : "";

    if (response.status === 403) {
      throw new Error(`Upload failed: Access permission expired or denied${detail}. Please refresh the page and try again.`);
    }
    if (response.status === 400) {
      throw new Error(`Upload failed: Invalid upload request or file parameters${detail}.`);
    }
    if (response.status === 413) {
      throw new Error("Upload failed: The file size exceeds the allowed limit.");
    }
    if (response.status >= 500) {
      throw new Error("Upload failed: Cloud storage server issue. Please try again later.");
    }

    throw new Error(`Upload failed (HTTP ${response.status})${detail}. Please try again or contact support.`);
  }
}
