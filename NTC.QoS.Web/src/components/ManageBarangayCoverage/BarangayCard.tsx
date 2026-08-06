import { memo, useEffect, useRef, useMemo, useCallback } from "react";
import { useForm } from "react-hook-form";
import { Save, Loader2, Trash2 } from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { type BarangayWithCoverage } from "./types";

interface BarangayCardProps {
    barangay: BarangayWithCoverage;
    onSave: (barangay: BarangayWithCoverage, values: { wiredPercentage: number; wirelessPercentage: number }) => void;
    onDelete: (barangay: BarangayWithCoverage) => void;
    isSaving: boolean;
}

// Custom equality check for memo to prevent unnecessary re-renders
const arePropsEqual = (prevProps: BarangayCardProps, nextProps: BarangayCardProps) => {
    return (
        prevProps.barangay.id === nextProps.barangay.id &&
        prevProps.barangay.wiredPercentage === nextProps.barangay.wiredPercentage &&
        prevProps.barangay.wirelessPercentage === nextProps.barangay.wirelessPercentage &&
        prevProps.barangay.coverage?.id === nextProps.barangay.coverage?.id &&
        prevProps.barangay.coverage?.updatedAt === nextProps.barangay.coverage?.updatedAt &&
        prevProps.barangay.localCoverageId === nextProps.barangay.localCoverageId &&
        prevProps.isSaving === nextProps.isSaving
    );
};

export const BarangayCard = memo(({ barangay, onSave, onDelete, isSaving }: BarangayCardProps) => {
    const form = useForm<{ wiredPercentage: number; wirelessPercentage: number }>({
        defaultValues: {
            wiredPercentage: barangay.wiredPercentage ?? 0,
            wirelessPercentage: barangay.wirelessPercentage ?? 0,
        },
    });

    // Track if we're waiting for delete confirmation
    const pendingDeleteRef = useRef(false);

    const wiredPercentage = form.watch("wiredPercentage");
    const wirelessPercentage = form.watch("wirelessPercentage");

    // Memoize expensive calculations
    const { isChecked, combinedPercentage } = useMemo(() => {
        const getCombinedPercentage = (wired: number, wireless: number) => Math.min(wired + wireless, 100);
        const isCovered = (wired: number, wireless: number) => wired === 100 || wireless === 100;
        return {
            isChecked: isCovered(wiredPercentage, wirelessPercentage),
            combinedPercentage: getCombinedPercentage(wiredPercentage, wirelessPercentage)
        };
    }, [wiredPercentage, wirelessPercentage]);

    // Check if barangay has a coverage record (from server or locally created)
    const hasCoverage = !!(barangay.coverage?.id ?? barangay.localCoverageId);

    // Determine if values are zeroed out (for delete detection)
    const isZeroed = wiredPercentage === 0 && wirelessPercentage === 0;
    const formIsDirty = form.formState.isDirty;

    // Show delete pending state when:
    // 1. Form has unsaved changes (isDirty)
    // 2. Coverage is not checked (values are not at 100)
    // 3. Existing coverage exists (has coverageId)
    // 4. Both values are zeroed out (user unchecked the coverage)
    const isDeletePending = formIsDirty && !isChecked && hasCoverage && isZeroed;

    useEffect(() => {
        form.reset({
            wiredPercentage: barangay.wiredPercentage ?? 0,
            wirelessPercentage: barangay.wirelessPercentage ?? 0,
        });
        pendingDeleteRef.current = false;
    }, [barangay.id, barangay.wiredPercentage, barangay.wirelessPercentage, barangay.coverage?.id, barangay.localCoverageId, form]);

    // Memoize callbacks to prevent re-renders
    const handleCheckboxToggle = useCallback((checked: boolean) => {
        const value = checked ? 100 : 0;
        form.setValue("wiredPercentage", value, { shouldDirty: true, shouldTouch: true });
        form.setValue("wirelessPercentage", value, { shouldDirty: true, shouldTouch: true });
    }, [form]);

    const submit = form.handleSubmit((values) => {
        onSave(barangay, {
            wiredPercentage: Math.min(Math.max(values.wiredPercentage ?? 0, 0), 100),
            wirelessPercentage: Math.min(Math.max(values.wirelessPercentage ?? 0, 0), 100),
        });
    });

    const handleDelete = useCallback(() => {
        onDelete(barangay);
    }, [onDelete, barangay]);

    // Handle form submission - either save or delete based on state
    const handleFormSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (!formIsDirty || isSaving) return;

        if (isDeletePending) {
            handleDelete();
        } else {
            submit();
        }
    };

    return (
        <form
            onSubmit={handleFormSubmit}
            className={`
          relative rounded-lg border-2 p-3 transition-all duration-200 mb-2
          ${isDeletePending
                    ? "border-red-300 bg-gradient-to-r from-red-50 to-orange-50"
                    : formIsDirty
                        ? "border-amber-300 bg-gradient-to-r from-amber-50 to-orange-50"
                        : isChecked
                            ? "border-green-300 bg-gradient-to-r from-green-50 to-emerald-50"
                            : "border-gray-200 bg-white hover:border-purple-200"
                }
        `}
            style={{ contain: 'layout style paint' }}
        >
            <div className="flex flex-col gap-3">
                {/* Top row: Checkbox, Barangay Name/Info, and Action Buttons */}
                <div className="flex items-center gap-3">
                    {/* Checkbox */}
                    <Checkbox
                        checked={isChecked}
                        onCheckedChange={(checked) => handleCheckboxToggle(checked as boolean)}
                        className="h-4 w-4 border-2 data-[state=checked]:bg-green-500 data-[state=checked]:border-green-500 flex-shrink-0"
                        disabled={isSaving}
                    />

                    {/* Barangay Name and Badges */}
                    <div className="flex items-center gap-2 flex-wrap flex-1 min-w-0">
                        <h4 className="font-semibold text-gray-900 text-sm">{barangay.name}</h4>
                        {barangay.code && (
                            <Badge variant="outline" className="text-xs bg-blue-100 text-blue-700 border-blue-300">
                                {barangay.code}
                            </Badge>
                        )}
                        {isDeletePending && (
                            <Badge variant="outline" className="text-xs bg-red-100 text-red-700 border-red-300">
                                Pending Delete
                            </Badge>
                        )}
                        {!isDeletePending && formIsDirty && (
                            <Badge variant="outline" className="text-xs bg-amber-100 text-amber-700 border-amber-300">
                                Unsaved
                            </Badge>
                        )}
                        {barangay.coverage?.isWiredCovered && (
                            <Badge variant="outline" className="text-xs bg-green-100 text-green-700 border-green-300">
                                Wired 100%
                            </Badge>
                        )}
                        {barangay.coverage?.isWirelessCovered && (
                            <Badge variant="outline" className="text-xs bg-blue-100 text-blue-700 border-blue-300">
                                Wireless 100%
                            </Badge>
                        )}
                    </div>

                    {/* Action Buttons - inline with name */}
                    {formIsDirty && (
                        <div className="flex items-center gap-2 flex-shrink-0">
                            {isDeletePending ? (
                                <Button
                                    type="submit"
                                    disabled={isSaving}
                                    size="sm"
                                    variant="destructive"
                                    className="h-8">
                                    {isSaving ? (
                                        <>
                                            <Loader2 className="h-3 w-3 animate-spin mr-1" />
                                            Deleting
                                        </>
                                    ) : (
                                        <>
                                            <Trash2 className="h-3 w-3 mr-1" />
                                            Delete
                                        </>
                                    )}
                                </Button>
                            ) : (
                                <Button
                                    type="submit"
                                    disabled={isSaving}
                                    size="sm"
                                    className="h-8 bg-gradient-to-r from-green-500 to-emerald-600 hover:from-green-600 hover:to-emerald-700 text-white">
                                    {isSaving ? (
                                        <>
                                            <Loader2 className="h-3 w-3 animate-spin mr-1" />
                                            Saving
                                        </>
                                    ) : (
                                        <>
                                            <Save className="h-3 w-3 mr-1" />
                                            Save
                                        </>
                                    )}
                                </Button>
                            )}
                        </div>
                    )}
                </div>

                {/* Coverage Inputs */}
                <div className="grid grid-cols-1 xs:grid-cols-2 sm:grid-cols-3 gap-2 text-xs pl-7">
                    {/* Wired FTTH */}
                    <div className="space-y-1">
                        <Label htmlFor={`wired-${barangay.id}`} className="text-xs font-medium text-gray-600">
                            Wired Validation (%)
                        </Label>
                        <Input
                            id={`wired-${barangay.id}`}
                            type="number"
                            min="0"
                            max="100"
                            step="0.1"
                            {...form.register("wiredPercentage", {
                                setValueAs: (val) => {
                                    const parsed = parseFloat(val);
                                    if (Number.isNaN(parsed)) return 0;
                                    return Math.min(Math.max(parsed, 0), 100);
                                },
                            })}
                            onFocus={(e) => e.target.select()}
                            className="h-8 w-full text-xs border-purple-200 focus:border-purple-400"
                            disabled={isSaving}
                        />
                        <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                            <div
                                className="h-full bg-gradient-to-r from-purple-400 to-purple-600 transition-all duration-300"
                                style={{ width: `${wiredPercentage}%` }}
                            />
                        </div>
                    </div>

                    {/* Wireless */}
                    <div className="space-y-1">
                        <Label htmlFor={`wireless-${barangay.id}`} className="text-xs font-medium text-gray-600">
                            Wireless Validation (%)
                        </Label>
                        <Input
                            id={`wireless-${barangay.id}`}
                            type="number"
                            min="0"
                            max="100"
                            step="0.1"
                            {...form.register("wirelessPercentage", {
                                setValueAs: (val) => {
                                    const parsed = parseFloat(val);
                                    if (Number.isNaN(parsed)) return 0;
                                    return Math.min(Math.max(parsed, 0), 100);
                                },
                            })}
                            onFocus={(e) => e.target.select()}
                            className="h-8 w-full text-xs border-blue-200 focus:border-blue-400"
                            disabled={isSaving}
                        />
                        <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                            <div
                                className="h-full bg-gradient-to-r from-blue-400 to-blue-600 transition-all duration-300"
                                style={{ width: `${wirelessPercentage}%` }}
                            />
                        </div>
                    </div>

                    {/* Combined */}
                    <div className="space-y-1">
                        <Label className="text-xs font-medium text-gray-600">Combined</Label>
                        <div className="h-8 w-full bg-gray-100 rounded-lg flex items-center justify-center border border-gray-200">
                            <span className="text-sm font-bold text-gray-700">{combinedPercentage.toFixed(1)}%</span>
                        </div>
                        <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                            <div
                                className="h-full bg-gradient-to-r from-green-400 to-emerald-600 transition-all duration-300"
                                style={{ width: `${combinedPercentage}%` }}
                            />
                        </div>
                    </div>
                </div>

                {/* Metadata */}
                {barangay.coverage && (
                    <div className="text-xs text-gray-500 pl-7">
                        Updated: {new Date(barangay.coverage.updatedAt).toLocaleDateString()} by {barangay.coverage.updatedBy}
                    </div>
                )}
            </div>
        </form>
    );
}, arePropsEqual);

BarangayCard.displayName = "BarangayCard";
