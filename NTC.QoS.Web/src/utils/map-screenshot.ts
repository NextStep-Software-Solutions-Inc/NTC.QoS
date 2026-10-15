/**
 * Map Screenshot Utility
 *
 * Provides shared functions to capture the current map view as an image:
 *   - downloadMapScreenshot        → PNG download
 *   - exportMapScreenshotAsPDF     → PDF download (PNG embedded via jsPDF, Letter size)
 *   - copyMapScreenshotToClipboard → Copy PNG to clipboard
 *
 * Usage:
 *   import { downloadMapScreenshot, exportMapScreenshotAsPDF, copyMapScreenshotToClipboard } from "@/utils/map-screenshot";
 *   downloadMapScreenshot(map.getCanvas(), "public");
 *   exportMapScreenshotAsPDF(map.getCanvas(), "public");
 *   await copyMapScreenshotToClipboard(map.getCanvas());
 */

import { jsPDF } from "jspdf";

/**
 * Formats a Date as "YYYYMMDD_HHMM".
 */
function formatTimestamp(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hours = pad(date.getHours());
  const minutes = pad(date.getMinutes());
  return `${year}${month}${day}_${hours}${minutes}`;
}

/**
 * Renders the source canvas onto a new canvas with a timestamp watermark applied.
 * Used internally by all three export functions to ensure consistent output.
 */
function renderAnnotatedCanvas(sourceCanvas: HTMLCanvasElement, now: Date): HTMLCanvasElement {
  const outputCanvas = document.createElement("canvas");
  outputCanvas.width = sourceCanvas.width;
  outputCanvas.height = sourceCanvas.height;

  const ctx = outputCanvas.getContext("2d");
  if (!ctx) return outputCanvas;

  // Draw the map canvas
  ctx.drawImage(sourceCanvas, 0, 0);

  // Add timestamp watermark in the bottom-right corner
  const label = `${now.toLocaleDateString()} ${now.toLocaleTimeString()}`;
  const padding = 8;
  const fontSize = Math.max(12, Math.round(outputCanvas.width / 80));
  ctx.font = `${fontSize}px sans-serif`;

  const textWidth = ctx.measureText(label).width;
  const textHeight = fontSize;
  const boxX = outputCanvas.width - textWidth - padding * 2 - padding;
  const boxY = outputCanvas.height - textHeight - padding * 2 - padding;
  const boxWidth = textWidth + padding * 2;
  const boxHeight = textHeight + padding * 2;

  // Semi-transparent background for readability (rounded rectangle)
  const radius = 4;
  ctx.fillStyle = "rgba(0, 0, 0, 0.45)";
  ctx.beginPath();
  ctx.moveTo(boxX + radius, boxY);
  ctx.lineTo(boxX + boxWidth - radius, boxY);
  ctx.arcTo(boxX + boxWidth, boxY, boxX + boxWidth, boxY + radius, radius);
  ctx.lineTo(boxX + boxWidth, boxY + boxHeight - radius);
  ctx.arcTo(boxX + boxWidth, boxY + boxHeight, boxX + boxWidth - radius, boxY + boxHeight, radius);
  ctx.lineTo(boxX + radius, boxY + boxHeight);
  ctx.arcTo(boxX, boxY + boxHeight, boxX, boxY + boxHeight - radius, radius);
  ctx.lineTo(boxX, boxY + radius);
  ctx.arcTo(boxX, boxY, boxX + radius, boxY, radius);
  ctx.closePath();
  ctx.fill();

  // White text
  ctx.fillStyle = "#ffffff";
  ctx.fillText(label, boxX + padding, boxY + textHeight + padding * 0.5);

  return outputCanvas;
}

/**
 * Captures the map canvas, adds a timestamp watermark, and downloads as a PNG file.
 *   map_screenshot_<mapType>_<YYYYMMDD_HHMM>.png
 */
export function downloadMapScreenshot(sourceCanvas: HTMLCanvasElement, mapType: string): void {
  const now = new Date();
  const timestamp = formatTimestamp(now);
  const filename = `map_screenshot_${mapType}_${timestamp}.png`;

  const outputCanvas = renderAnnotatedCanvas(sourceCanvas, now);
  const dataUrl = outputCanvas.toDataURL("image/png");

  const link = document.createElement("a");
  link.href = dataUrl;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

/**
 * Captures the map canvas, adds a timestamp watermark, and downloads as a PDF (US Letter).
 * The annotated PNG is scale-fitted to the page with 10 mm margins.
 *   map_export_<mapType>_<YYYYMMDD_HHMM>.pdf
 */
export function exportMapScreenshotAsPDF(sourceCanvas: HTMLCanvasElement, mapType: string): void {
  const now = new Date();
  const timestamp = formatTimestamp(now);
  const filename = `map_export_${mapType}_${timestamp}.pdf`;

  const outputCanvas = renderAnnotatedCanvas(sourceCanvas, now);
  const dataUrl = outputCanvas.toDataURL("image/png");

  // Auto-orient based on canvas aspect ratio
  const isLandscape = outputCanvas.width > outputCanvas.height;
  const pdf = new jsPDF({
    orientation: isLandscape ? "landscape" : "portrait",
    unit: "mm",
    format: "letter",
  });

  const pageWidth = pdf.internal.pageSize.getWidth();
  const pageHeight = pdf.internal.pageSize.getHeight();
  const margin = 10;
  const availableWidth = pageWidth - margin * 2;
  const availableHeight = pageHeight - margin * 2;

  // Scale-fit while preserving aspect ratio
  const canvasAspect = outputCanvas.width / outputCanvas.height;
  let imgWidth = availableWidth;
  let imgHeight = imgWidth / canvasAspect;
  if (imgHeight > availableHeight) {
    imgHeight = availableHeight;
    imgWidth = imgHeight * canvasAspect;
  }

  // Center on page
  const x = margin + (availableWidth - imgWidth) / 2;
  const y = margin + (availableHeight - imgHeight) / 2;

  pdf.addImage(dataUrl, "PNG", x, y, imgWidth, imgHeight);
  pdf.save(filename);
}

/**
 * Captures the map canvas, adds a timestamp watermark, and writes the PNG to the clipboard.
 * Requires a secure context (HTTPS or localhost) and clipboard-write permission.
 */
export async function copyMapScreenshotToClipboard(sourceCanvas: HTMLCanvasElement): Promise<void> {
  const outputCanvas = renderAnnotatedCanvas(sourceCanvas, new Date());

  return new Promise<void>((resolve, reject) => {
    outputCanvas.toBlob((blob) => {
      if (!blob) {
        reject(new Error("Failed to create PNG blob"));
        return;
      }
      navigator.clipboard
        .write([new ClipboardItem({ "image/png": blob })])
        .then(resolve)
        .catch(reject);
    }, "image/png");
  });
}
