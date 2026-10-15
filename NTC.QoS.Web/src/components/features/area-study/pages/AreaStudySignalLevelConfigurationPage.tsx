"use client";

import React, { useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Check, Loader2, Save, Trash2 } from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";

import SubmitButton from "@/components/SubmitButton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { cn } from "@/lib/utils";
import { createSignalLevelEntryRequest } from "../handlers/createSignalLevelEntryRequest";
import { createSignalLevelRequest } from "../handlers/createSignalLevelRequest";
import { deleteSignalLevelEntryRequest } from "../handlers/deleteSignalLevelEntryRequest";
import { editSignalLevelEntryRequest } from "../handlers/editSignalLevelEntryRequest";
import { getAreaStudyDetailsRequest } from "../handlers/getAreaStudyDetailsRequest";
import { getSignalLevelEntriesRequest } from "../handlers/getSignalLevelEntriesRequest";
import { getSignalLevelsRequest } from "../handlers/getSignalLevelsRequest";
import { type SignalLevelEntry } from "../types/SignalLevelEntry";

const useGetAreaStudyDetails = createQueryHookFromTemplate(getAreaStudyDetailsRequest);
const useGetSignalLevels = createQueryHookFromTemplate(getSignalLevelsRequest);
const useGetSignalLevelEntries = createQueryHookFromTemplate(getSignalLevelEntriesRequest);
const useCreateSignalLevel = createMutationHookFromTemplate(createSignalLevelRequest);
const useCreateSignalLevelEntry = createMutationHookFromTemplate(createSignalLevelEntryRequest);
const useEditSignalLevelEntry = createMutationHookFromTemplate(editSignalLevelEntryRequest);
const useDeleteSignalLevelEntry = createMutationHookFromTemplate(deleteSignalLevelEntryRequest);

type EditableSignalLevelRow = {
  rowKey: string;
  id?: string;
  color: string;
  thresholdFrom: string;
  thresholdTo: string;
  description: string;
};

const levelCountSchema = z.object({
  noOfLevels: z
    .coerce
    .number({ message: "Number of levels is required." })
    .int("Number of levels must be a whole number.")
    .min(3, "Minimum is 3 levels.")
    .max(8, "Maximum is 8 levels."),
});

const defaultColors = ["#FF0000", "#FF9900", "#FFF200", "#7EC850", "#1A7A1A", "#0099FF", "#A020F0", "#777777"];

const parseNumber = (value: string): number | null => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
};

function buildRows(entries: SignalLevelEntry[], noOfLevels: number): EditableSignalLevelRow[] {
  const sortedEntries = [...entries].sort((a, b) => a.sortOrder - b.sortOrder);

  const existingRows = sortedEntries.map((entry, index) => ({
    rowKey: entry.id,
    id: entry.id,
    color: entry.color || defaultColors[index % defaultColors.length] || "#FF0000",
    thresholdFrom: entry.thresholdFrom?.toString() ?? "",
    thresholdTo: entry.thresholdTo?.toString() ?? "",
    description: entry.description ?? "",
  }));

  const targetCount = Math.max(noOfLevels, existingRows.length);

  const paddedRows = Array.from({ length: targetCount - existingRows.length }).map((_, index) => {
    const absoluteIndex = existingRows.length + index;
    return {
      rowKey: `draft-${absoluteIndex}`,
      color: defaultColors[absoluteIndex % defaultColors.length] || "#FF0000",
      thresholdFrom: "",
      thresholdTo: "",
      description: `Level ${absoluteIndex + 1}`,
    };
  });

  return [...existingRows, ...paddedRows];
}

export default function AreaStudySignalLevelConfigurationPage() {
  const router = useRouter();
  const params = useParams();
  const queryClient = useQueryClient();

  const areaStudyId = String(params.areaStudyId ?? "");

  const { data: areaStudyDetails } = useGetAreaStudyDetails({
    pathParams: { id: areaStudyId },
    queryParams: {},
    enabled: Boolean(areaStudyId),
  });

  const { data: signalLevelsResponse, isLoading: isLoadingSignalLevels } = useGetSignalLevels({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
    enabled: Boolean(areaStudyId),
  });

  const signalLevel = useMemo(
    () => signalLevelsResponse?.data.find((item) => item.areaStudyId === areaStudyId),
    [signalLevelsResponse?.data, areaStudyId],
  );

  const signalLevelId = signalLevel?.id;
  const configuredNoOfLevels = signalLevel?.noOfLevels ?? 0;

  const { data: entriesResponse, isLoading: isLoadingEntries } = useGetSignalLevelEntries({
    pathParams: { signalLevelId: signalLevelId ?? "" },
    queryParams: { pageSize: 100, pageNumber: 1 },
    enabled: Boolean(signalLevelId),
  });

  const entries = entriesResponse?.data ?? [];

  const createSignalLevelMutation = useCreateSignalLevel();
  const createEntryMutation = useCreateSignalLevelEntry();
  const editEntryMutation = useEditSignalLevelEntry();
  const deleteEntryMutation = useDeleteSignalLevelEntry();

  const [rows, setRows] = useState<EditableSignalLevelRow[]>([]);
  const [savingRowKey, setSavingRowKey] = useState<string | null>(null);
  const [savedRowKey, setSavedRowKey] = useState<string | null>(null);

  const createLevelForm = useForm<z.infer<typeof levelCountSchema>>({
    resolver: zodResolver(levelCountSchema),
    defaultValues: { noOfLevels: 5 },
  });

  useEffect(() => {
    if (!signalLevel) {
      return;
    }

    setRows(buildRows(entries, signalLevel.noOfLevels));
  }, [signalLevel, entries]);

  const invalidateSignalQueries = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === "get-signal-levels" }),
      queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === "get-signal-level-entries" }),
      queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === "get-area-studies" }),
    ]);
  };

  const setCellValue = (rowKey: string, key: keyof EditableSignalLevelRow, value: string) => {
    setRows((prev) => prev.map((row) => (row.rowKey === rowKey ? { ...row, [key]: value } : row)));
  };

  const getRowWarning = (index: number) => {
    const current = rows[index];
    if (!current) {
      return null;
    }
    const currentFrom = parseNumber(current.thresholdFrom);
    const currentTo = parseNumber(current.thresholdTo);

    if (currentFrom === null || currentTo === null) {
      return null;
    }

    if (currentFrom > currentTo) {
      return "From value must be less than or equal to To value.";
    }

    if (index === 0) {
      return null;
    }

    const prevRow = rows[index - 1];
    if (!prevRow) {
      return null;
    }
    const previousTo = parseNumber(prevRow.thresholdTo);
    const previousFrom = parseNumber(prevRow.thresholdFrom);
    if (previousTo === null) {
      return null;
    }

    if (previousFrom === null) {
      return null;
    }

    // Accept contiguous ranges for either direction in row order.
    // Ascending example: prev -110 to -99, current -99 to -89
    // Descending example: prev 90 to 100, current 60 to 90
    if (currentFrom === previousTo || currentTo === previousFrom) {
      return null;
    }

    const overlap = Math.max(currentFrom, previousFrom) < Math.min(currentTo, previousTo);
    if (overlap) {
      return `Overlap detected between Level ${index} and Level ${index + 1}.`;
    }

    return `Gap detected between Level ${index} and Level ${index + 1}.`;
  };

  const validateRow = (row: EditableSignalLevelRow, index: number, silent = false) => {
    const thresholdFrom = parseNumber(row.thresholdFrom);
    const thresholdTo = parseNumber(row.thresholdTo);

    if (!row.description.trim()) {
      if (!silent) {
        toast.error(`Row ${index + 1}: Description is required.`);
      }
      return null;
    }

    if (!/^#(?:[0-9a-fA-F]{3}){1,2}$/.test(row.color)) {
      if (!silent) {
        toast.error(`Row ${index + 1}: Color must be a valid hex value.`);
      }
      return null;
    }

    if (thresholdFrom === null || thresholdTo === null) {
      if (!silent) {
        toast.error(`Row ${index + 1}: Threshold From and To must be valid numbers.`);
      }
      return null;
    }

    if (thresholdFrom > thresholdTo) {
      if (!silent) {
        toast.error(`Row ${index + 1}: From value must be less than or equal to To value.`);
      }
      return null;
    }

    return {
      description: row.description.trim(),
      color: row.color,
      thresholdFrom,
      thresholdTo,
      sortOrder: index + 1,
    };
  };

  const saveRow = async (index: number, silent = false) => {
    const row = rows[index];

    if (!signalLevelId || !row) {
      return;
    }

    const payload = validateRow(row, index, silent);
    if (!payload) {
      return;
    }

    setSavingRowKey(row.rowKey);

    try {
      if (row.id) {
        const response = await editEntryMutation.mutateAsync({
          pathParams: { signalLevelId, id: row.id },
          payload,
        });

        if (!silent) {
          toast.success(response.message ?? "Entry updated.");
        }
      } else {
        const response = await createEntryMutation.mutateAsync({
          pathParams: { signalLevelId },
          payload,
        });

        setRows((prev) =>
          prev.map((item, itemIndex) =>
            itemIndex === index ? { ...item, id: response.data.id, rowKey: response.data.id } : item,
          ),
        );

        if (!silent) {
          toast.success(response.message ?? "Entry created.");
        }
      }

      setSavedRowKey(row.rowKey);
      setTimeout(() => {
        setSavedRowKey((current) => (current === row.rowKey ? null : current));
      }, 1400);

      await invalidateSignalQueries();
    } catch {
      if (!silent) {
        toast.error("Unable to save row.");
      }
    } finally {
      setSavingRowKey((current) => (current === row.rowKey ? null : current));
    }
  };

  const onCreateLevels = async (values: z.infer<typeof levelCountSchema>) => {
    if (!areaStudyId) {
      toast.error("Area Study context is missing.");
      return;
    }

    toast.promise(
      createSignalLevelMutation.mutateAsync({
        pathParams: {},
        payload: {
          areaStudyId,
          noOfLevels: values.noOfLevels,
        },
      }),
      {
        loading: "Creating signal levels...",
        success: async (response) => {
          await invalidateSignalQueries();
          return response.message ?? "Signal levels configured.";
        },
        error: "Failed to create signal levels.",
      },
    );
  };

  const onDeleteExistingRow = async (index: number) => {
    const row = rows[index];

    if (!signalLevelId || !row?.id) {
      return;
    }

    const confirmed = window.confirm(`Delete ${row.description || `Level ${index + 1}`}?`);
    if (!confirmed) {
      return;
    }

    toast.promise(
      deleteEntryMutation.mutateAsync({ pathParams: { signalLevelId, id: row.id } }),
      {
        loading: "Deleting row...",
        success: async (response) => {
          await invalidateSignalQueries();
          return response.message ?? "Row deleted.";
        },
        error: "Failed to delete row.",
      },
    );
  };

  const [isSavingAll, setIsSavingAll] = useState(false);

  const applyStandardColors = () => {
    setRows((prev) =>
      prev.map((row, index) => ({
        ...row,
        color: defaultColors[index % defaultColors.length] || "#FF0000",
      }))
    );
    toast.info("Standard colors applied (Weakest = Red, Strongest = Green). Click 'Save All' or 'Save' on each row to persist.");
  };

  const saveAllRows = async () => {
    if (!signalLevelId) return;
    setIsSavingAll(true);
    try {
      let savedCount = 0;
      for (let i = 0; i < rows.length; i++) {
        const row = rows[i];
        if (!row) continue;
        const payload = validateRow(row, i, false);
        if (!payload) {
          setIsSavingAll(false);
          return;
        }
        if (row.id) {
          await editEntryMutation.mutateAsync({
            pathParams: { signalLevelId, id: row.id },
            payload,
          });
        } else {
          const res = await createEntryMutation.mutateAsync({
            pathParams: { signalLevelId },
            payload,
          });
          setRows((prev) =>
            prev.map((item, idx) => (idx === i ? { ...item, id: res.data.id, rowKey: res.data.id } : item))
          );
        }
        savedCount++;
      }
      toast.success(`Successfully saved ${savedCount} signal levels.`);
      await invalidateSignalQueries();
    } catch {
      toast.error("Failed to save all signal levels.");
    } finally {
      setIsSavingAll(false);
    }
  };

  const areaStudyName = areaStudyDetails?.data?.name ?? "Area Study";

  const isStateALoading = isLoadingSignalLevels && !signalLevel;
  const isStateBCreating = createSignalLevelMutation.isPending;
  const isStateCConfigured = Boolean(signalLevel);

  return (
    <div className="w-full p-4 md:p-6 space-y-6">
      <div className="flex items-center justify-between gap-2 flex-wrap">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold">Configure Signal Levels for: {areaStudyName}</h1>
          <p className="text-sm text-muted-foreground">
            Define the number of levels and maintain color-coded dBm ranges in a single table.
          </p>
        </div>
        <Button variant="outline" onClick={() => router.back()}>
          <ArrowLeft className="h-4 w-4" />
          Back
        </Button>
      </div>

      {!isStateCConfigured && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">How many signal levels do you want?</CardTitle>
          </CardHeader>
          <CardContent>
            {isStateALoading ? (
              <div className="h-20 rounded-md border bg-muted/20 animate-pulse" />
            ) : (
              <Form {...createLevelForm}>
                <form onSubmit={createLevelForm.handleSubmit(onCreateLevels)} className="flex items-end gap-3 flex-wrap">
                  <FormField
                    control={createLevelForm.control}
                    name="noOfLevels"
                    render={({ field }) => (
                      <FormItem className="w-[220px]">
                        <FormLabel>Number of Levels (3 to 8)</FormLabel>
                        <FormControl>
                          <Input type="number" min={3} max={8} step={1} {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <SubmitButton loading={isStateBCreating}>Create Levels</SubmitButton>
                </form>
              </Form>
            )}
          </CardContent>
        </Card>
      )}

      {isStateBCreating && (
        <Card>
          <CardContent className="py-10">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Creating signal level configuration...
            </div>
          </CardContent>
        </Card>
      )}

      {isStateCConfigured && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Signal Level Ranges</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between flex-wrap gap-2">
              <div className="flex items-center gap-2">
                <Badge variant="secondary">{rows.length} levels</Badge>
                <Badge variant="outline">Configured count: {configuredNoOfLevels}</Badge>
              </div>
              <div className="flex items-center gap-2 flex-wrap">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={applyStandardColors}>
                  Standard Colors (Red → Green)
                </Button>
                <Button
                  variant="default"
                  size="sm"
                  onClick={() => void saveAllRows()}
                  disabled={isSavingAll || createEntryMutation.isPending || editEntryMutation.isPending}
                  className="gap-1">
                  {isSavingAll ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Save className="h-3.5 w-3.5" />}
                  Save All
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setRows(buildRows(entries, configuredNoOfLevels));
                  }}>
                  Reset Drafts
                </Button>
              </div>
            </div>

            <div className="rounded-md border overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[80px]">Level</TableHead>
                    <TableHead className="w-[160px]">Color</TableHead>
                    <TableHead className="min-w-[340px]">Range (dBm)</TableHead>
                    <TableHead className="min-w-[240px]">Description</TableHead>
                    <TableHead className="w-[160px] text-right">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoadingEntries ? (
                    <TableRow>
                      <TableCell colSpan={5} className="text-center text-muted-foreground">
                        Loading ranges...
                      </TableCell>
                    </TableRow>
                  ) : (
                    rows.map((row, index) => {
                      const rowWarning = getRowWarning(index);
                      const isSaving = savingRowKey === row.rowKey;
                      const isSaved = savedRowKey === row.rowKey;

                      return (
                        <TableRow key={row.rowKey}>
                          <TableCell className="font-medium">{index + 1}</TableCell>

                          <TableCell>
                            <div className="flex items-center gap-2">
                              <input
                                type="color"
                                value={row.color}
                                onChange={(event) => setCellValue(row.rowKey, "color", event.target.value)}
                                onBlur={() => {
                                  void saveRow(index, true);
                                }}
                                className="h-9 w-10 cursor-pointer rounded border bg-background p-1"
                              />
                              <span className="text-xs text-muted-foreground font-mono">{row.color}</span>
                            </div>
                          </TableCell>

                          <TableCell>
                            <div className="space-y-2">
                              <div className="flex items-center gap-2">
                                <Input
                                  type="number"
                                  value={row.thresholdFrom}
                                  onChange={(event) => setCellValue(row.rowKey, "thresholdFrom", event.target.value)}
                                  onBlur={() => {
                                    void saveRow(index, true);
                                  }}
                                  placeholder="From"
                                  className="w-[130px]"
                                />
                                <span className="text-muted-foreground">to</span>
                                <Input
                                  type="number"
                                  value={row.thresholdTo}
                                  onChange={(event) => setCellValue(row.rowKey, "thresholdTo", event.target.value)}
                                  onBlur={() => {
                                    void saveRow(index, true);
                                  }}
                                  placeholder="To"
                                  className="w-[130px]"
                                />
                                <span className="text-xs text-muted-foreground">dBm</span>
                              </div>
                              {rowWarning && (
                                <p className="text-xs text-amber-600 dark:text-amber-400">
                                  {rowWarning}
                                </p>
                              )}
                            </div>
                          </TableCell>

                          <TableCell>
                            <Input
                              value={row.description}
                              onChange={(event) => setCellValue(row.rowKey, "description", event.target.value)}
                              onBlur={() => {
                                void saveRow(index, true);
                              }}
                              placeholder={`Level ${index + 1}`}
                            />
                          </TableCell>

                          <TableCell>
                            <div className="flex items-center justify-end gap-2">
                              <Button
                                type="button"
                                size="sm"
                                variant="outline"
                                onClick={() => {
                                  void saveRow(index, false);
                                }}
                                disabled={isSaving || createEntryMutation.isPending || editEntryMutation.isPending}
                                className={cn("gap-1", isSaved && "text-green-700 border-green-300")}
                              >
                                {isSaving ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Save className="h-3.5 w-3.5" />}
                                {isSaved ? "Saved" : "Save"}
                              </Button>

                              {row.id && (
                                <Button
                                  type="button"
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => {
                                    void onDeleteExistingRow(index);
                                  }}
                                  disabled={deleteEntryMutation.isPending}
                                >
                                  <Trash2 className="h-4 w-4 text-destructive" />
                                </Button>
                              )}

                              {!row.id && isSaved && <Check className="h-4 w-4 text-green-600" />}
                            </div>
                          </TableCell>
                        </TableRow>
                      );
                    })
                  )}
                </TableBody>
              </Table>
            </div>

            <details className="rounded-md border p-3">
              <summary className="cursor-pointer text-sm font-medium">Developer details</summary>
              <div className="mt-2 space-y-1 text-xs text-muted-foreground">
                <p>areaStudyId: {areaStudyId || "—"}</p>
                <p>signalLevelId: {signalLevelId || "—"}</p>
              </div>
            </details>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
