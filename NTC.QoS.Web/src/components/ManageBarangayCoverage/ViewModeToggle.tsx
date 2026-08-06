import { Button } from "@/components/ui/button";
import { type ViewMode } from "./types";

interface ViewModeToggleProps {
    viewMode: ViewMode;
    onViewModeChange: (mode: ViewMode) => void;
    totalBarangays: number;
    loadedBarangays: number;
    hasNextPage?: boolean;
}

export function ViewModeToggle({ viewMode, onViewModeChange, totalBarangays, loadedBarangays, hasNextPage }: ViewModeToggleProps) {
    const percentage = totalBarangays > 0 ? Math.round((loadedBarangays / totalBarangays) * 100) : 0;

    return (
        <div className="mb-3 flex items-center justify-between">
            <div className="text-sm text-gray-600">
                Showing {loadedBarangays.toLocaleString()} of {totalBarangays.toLocaleString()} Barangays ({percentage}%)
            </div>
            <div className="flex gap-2">
                <Button
                    variant={viewMode === "grouped" ? "default" : "outline"}
                    size="sm"
                    onClick={() => onViewModeChange("grouped")}
                    className="h-8">
                    Grouped
                </Button>
                <Button
                    variant={viewMode === "flatten" ? "default" : "outline"}
                    size="sm"
                    onClick={() => onViewModeChange("flatten")}
                    className="h-8">
                    Flatten
                </Button>
            </div>
        </div>
    );
}
