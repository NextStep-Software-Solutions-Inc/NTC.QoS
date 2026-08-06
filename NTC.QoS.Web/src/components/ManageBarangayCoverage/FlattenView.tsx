import { memo } from "react";
import { BarangayCard } from "./BarangayCard";
import { type BarangayWithCoverage } from "./types";

interface FlattenViewProps {
    flattenedBarangays: BarangayWithCoverage[];
    onSave: (barangay: BarangayWithCoverage, values: { wiredPercentage: number; wirelessPercentage: number }) => void;
    onDelete: (barangay: BarangayWithCoverage) => void;
    savingBarangays: Set<string>;
}

export const FlattenView = memo(function FlattenView({ flattenedBarangays, onSave, onDelete, savingBarangays }: FlattenViewProps) {
    return (
        <div className="space-y-2">
            {flattenedBarangays?.map((barangay) => (
                <BarangayCard
                    key={barangay.id}
                    barangay={barangay}
                    onSave={onSave}
                    onDelete={onDelete}
                    isSaving={savingBarangays.has(barangay.id)}
                />
            ))}
        </div>
    );
});
