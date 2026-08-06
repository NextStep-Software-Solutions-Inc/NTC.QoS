/**
 * Convert camelCase or PascalCase string to space-separated words
 * @example
 * formatLabel("WithAssets") // "With Assets"
 * formatLabel("coverageStatus") // "coverage Status"
 * formatLabel("totalCells") // "total Cells"
 */
export function formatLabel(str: string): string {
  return str.replace(/([A-Z])/g, " $1").trim();
}
