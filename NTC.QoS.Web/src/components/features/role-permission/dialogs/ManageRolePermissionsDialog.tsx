"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  BarChart3,
  CheckSquare,
  FileSpreadsheet,
  FileText,
  Globe,
  HardDrive,
  Layers,
  Loader2,
  Lock,
  Map as MapIcon,
  MapPin,
  Radio,
  Search,
  Server,
  Settings,
  Shield,
  Sparkles,
  Square,
  UserCheck,
  Users,
} from "lucide-react";
import { toast } from "sonner";

import { useGetPermissions } from "@/components/features/permission/hooks/useGetPermissions";
import type { Permission } from "@/components/features/permission/types/Permission";
import { updateRolePermissionsRequest } from "@/components/features/role/handlers/updateRolePermissionsRequest";
import type { Role } from "@/components/features/role/types/Role";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";

// Platform-only permission keys that regional roles cannot hold
const PLATFORM_ONLY_PERMISSIONS = new Set([
  "system:admin",
  "system:logs",
  "system:settings",
  "organizations:create",
  "organizations:update",
  "organizations:delete",
  "regions:create",
  "regions:update",
  "regions:delete",
  "roles:create",
  "roles:update",
  "roles:delete",
  "permissions:create",
  "permissions:update",
  "permissions:delete",
  "role_permissions:create",
  "role_permissions:delete",
]);

interface ManageRolePermissionsDialogProps {
  role: Role | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}

const useUpdateRolePermissions = createMutationHookFromTemplate(updateRolePermissionsRequest);

export function ManageRolePermissionsDialog({ role, open, onOpenChange, onSuccess }: ManageRolePermissionsDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title={
        <div className="flex items-center gap-2">
          <div className="h-9 w-9 rounded-lg bg-primary/10 flex items-center justify-center text-primary">
            <Shield className="h-5 w-5" />
          </div>
          <div>
            <div className="text-lg font-bold flex items-center gap-2">
              <span>Manage Role Permissions</span>
              {role && (
                <Badge variant={role.scope === "Platform" ? "default" : "secondary"} className="gap-1 text-xs">
                  {role.scope === "Platform" ? <Globe className="h-3 w-3" /> : <MapPin className="h-3 w-3" />}
                  {role.scope || "Platform"}
                </Badge>
              )}
            </div>
            {role && <p className="text-xs text-muted-foreground font-normal">Role: {role.name} ({role.key})</p>}
          </div>
        </div>
      }
      description={role?.description || "Configure functional and jurisdictional capabilities for this role."}
      size="wide">
      {open && role && (
        <ManageRolePermissionsContent
          key={role.id}
          role={role}
          onOpenChange={onOpenChange}
          onSuccess={onSuccess}
        />
      )}
    </ResponsiveDialog>
  );
}

function getCategoryIcon(category: string) {
  const cat = category.toLowerCase();
  if (cat.includes("user management") || cat.includes("user")) return <Users className="h-4 w-4" />;
  if (cat.includes("role") || cat.includes("permission")) return <Shield className="h-4 w-4" />;
  if (cat.includes("region") || cat.includes("province") || cat.includes("municipality") || cat.includes("barangay"))
    return <MapIcon className="h-4 w-4" />;
  if (cat.includes("organization")) return <Layers className="h-4 w-4" />;
  if (cat.includes("speedtest")) return <Activity className="h-4 w-4" />;
  if (cat.includes("broadband") || cat.includes("nap") || cat.includes("cell")) return <Radio className="h-4 w-4" />;
  if (cat.includes("device") || cat.includes("hardware")) return <Server className="h-4 w-4" />;
  if (cat.includes("analytics") || cat.includes("dashboard")) return <BarChart3 className="h-4 w-4" />;
  if (cat.includes("report")) return <FileText className="h-4 w-4" />;
  if (cat.includes("file")) return <FileSpreadsheet className="h-4 w-4" />;
  if (cat.includes("ai")) return <Sparkles className="h-4 w-4" />;
  if (cat.includes("system")) return <Settings className="h-4 w-4" />;
  return <HardDrive className="h-4 w-4" />;
}

interface CategoryGroup {
  category: string;
  items: Permission[];
  totalCount: number;
  selectedCount: number;
}

function ManageRolePermissionsContent({
  role,
  onOpenChange,
  onSuccess,
}: {
  role: Role;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}) {
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [activeCategory, setActiveCategory] = useState<string>("all");
  const [showOnlySelected, setShowOnlySelected] = useState<boolean>(false);
  const [selectedPermissions, setSelectedPermissions] = useState<Set<string>>(
    () => new Set(role.permissions ?? [])
  );
  const initialPermissions = useMemo(() => new Set(role.permissions ?? []), [role.permissions]);

  const isRegionalRole = role.scope === "Regional";

  // Debounced search (250ms)
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchQuery.trim().toLowerCase());
    }, 250);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Fetch catalog permissions
  const { data: permissionsData, isLoading: isLoadingPermissions } = useGetPermissions({
    pathParams: {},
    queryParams: { pageNumber: 1, pageSize: 1000 },
  });

  const { mutateAsync: updateRolePermissions, isPending: isSaving } = useUpdateRolePermissions();

  const allPermissions: Permission[] = useMemo(() => permissionsData?.data ?? [], [permissionsData?.data]);

  // Group permissions by category
  const categories: [string, Permission[]][] = useMemo(() => {
    const map = new globalThis.Map<string, Permission[]>();
    for (const p of allPermissions) {
      const cat = p.category || "General";
      if (!map.has(cat)) map.set(cat, []);
      map.get(cat)!.push(p);
    }
    return Array.from(map.entries()).sort(([a], [b]) => a.localeCompare(b));
  }, [allPermissions]);

  // Filter permissions based on search, category tab, and showOnlySelected
  const filteredCategories: CategoryGroup[] = useMemo(() => {
    const result: CategoryGroup[] = [];

    for (const [category, items] of categories) {
      if (activeCategory !== "all" && activeCategory !== category) {
        continue;
      }

      const filteredItems = items.filter((p: Permission) => {
        if (showOnlySelected && !selectedPermissions.has(p.key)) return false;
        if (!debouncedSearch) return true;
        return (
          p.name.toLowerCase().includes(debouncedSearch) ||
          p.key.toLowerCase().includes(debouncedSearch) ||
          Boolean(p.description && p.description.toLowerCase().includes(debouncedSearch))
        );
      });

      if (filteredItems.length > 0) {
        result.push({
          category,
          items: filteredItems,
          totalCount: items.length,
          selectedCount: items.filter((p: Permission) => selectedPermissions.has(p.key)).length,
        });
      }
    }

    return result;
  }, [categories, activeCategory, showOnlySelected, debouncedSearch, selectedPermissions]);

  const totalFilteredPermissions = useMemo(() => {
    return filteredCategories.reduce((acc, cat) => acc + cat.items.length, 0);
  }, [filteredCategories]);

  const handleTogglePermission = (permissionKey: string) => {
    // If regional role and platform-only, forbid toggle
    if (isRegionalRole && PLATFORM_ONLY_PERMISSIONS.has(permissionKey)) {
      toast.error(`Permission "${permissionKey}" requires Platform scope and cannot be assigned to Regional roles.`);
      return;
    }

    setSelectedPermissions((prev) => {
      const next = new Set(prev);
      if (next.has(permissionKey)) {
        next.delete(permissionKey);
      } else {
        next.add(permissionKey);
      }
      return next;
    });
  };

  const handleToggleCategory = (categoryItems: Permission[]) => {
    const selectableKeys = categoryItems
      .filter((p) => !(isRegionalRole && PLATFORM_ONLY_PERMISSIONS.has(p.key)))
      .map((p) => p.key);

    const allSelected = selectableKeys.every((k) => selectedPermissions.has(k));

    setSelectedPermissions((prev) => {
      const next = new Set(prev);
      if (allSelected) {
        selectableKeys.forEach((k) => next.delete(k));
      } else {
        selectableKeys.forEach((k) => next.add(k));
      }
      return next;
    });
  };

  const handleSelectAllFiltered = () => {
    const allFilteredKeys = filteredCategories.flatMap((cat) =>
      cat.items
        .filter((p) => !(isRegionalRole && PLATFORM_ONLY_PERMISSIONS.has(p.key)))
        .map((p) => p.key)
    );

    const allSelected = allFilteredKeys.every((k) => selectedPermissions.has(k));

    setSelectedPermissions((prev) => {
      const next = new Set(prev);
      if (allSelected) {
        allFilteredKeys.forEach((k) => next.delete(k));
      } else {
        allFilteredKeys.forEach((k) => next.add(k));
      }
      return next;
    });
  };

  const handleSave = async () => {
    try {
      await updateRolePermissions({
        pathParams: { id: role.id },
        payload: { permissions: Array.from(selectedPermissions) },
      });

      toast.success(`Updated ${selectedPermissions.size} permissions for role "${role.name}"`);
      onOpenChange(false);
      onSuccess?.();
    } catch (error) {
      const err = error as Error;
      toast.error(`Failed to update permissions: ${err.message || "Unknown error"}`);
    }
  };

  const hasChanges = !arePermissionSetsEqual(selectedPermissions, initialPermissions);

  return (
    <div className="space-y-4 pt-1">
      {/* Scope Alert for Regional Roles */}
      {isRegionalRole && (
        <div className="flex items-center gap-2 p-3 bg-amber-500/10 border border-amber-500/20 rounded-lg text-xs text-amber-800 dark:text-amber-300">
          <Lock className="h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400" />
          <span>
            <strong>Regional Role Scope:</strong> Platform-level permissions (such as system administration, role creation, and national region management) are locked to maintain regional security boundaries.
          </span>
        </div>
      )}

      {/* Controls Bar: Search, Category Tabs, Stats */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 pb-1">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search permissions or categories..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9 h-9 text-sm"
          />
        </div>

        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant={showOnlySelected ? "default" : "outline"}
            size="sm"
            onClick={() => setShowOnlySelected(!showOnlySelected)}
            className="h-9 gap-1.5 text-xs font-medium">
            <UserCheck className="h-3.5 w-3.5" />
            {showOnlySelected ? "Showing Selected Only" : "Show Selected Only"}
          </Button>

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleSelectAllFiltered}
            disabled={totalFilteredPermissions === 0 || isLoadingPermissions}
            className="h-9 gap-1.5 text-xs font-medium">
            <CheckSquare className="h-3.5 w-3.5" />
            Select / Deselect Visible
          </Button>
        </div>
      </div>

      {/* Category Filter Chips */}
      <div className="overflow-x-auto pb-1">
        <Tabs value={activeCategory} onValueChange={setActiveCategory} className="w-full">
          <TabsList className="h-9 bg-muted/70 p-1 flex w-max">
            <TabsTrigger value="all" className="text-xs px-3 py-1">
              All Domains ({allPermissions.length})
            </TabsTrigger>
            {categories.map(([category, items]: [string, Permission[]]) => {
              const countSelected = items.filter((p: Permission) => selectedPermissions.has(p.key)).length;
              return (
                <TabsTrigger key={category} value={category} className="text-xs px-3 py-1 gap-1.5">
                  {getCategoryIcon(category)}
                  <span>{category}</span>
                  <Badge variant={countSelected > 0 ? "default" : "outline"} className="text-[10px] px-1 py-0 h-4">
                    {countSelected}/{items.length}
                  </Badge>
                </TabsTrigger>
              );
            })}
          </TabsList>
        </Tabs>
      </div>

      {/* Main Permissions Content */}
      {isLoadingPermissions ? (
        <div className="flex flex-col items-center justify-center py-16 gap-3 text-muted-foreground">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
          <p className="text-sm">Loading permissions registry...</p>
        </div>
      ) : filteredCategories.length === 0 ? (
        <div className="text-center py-16 rounded-xl border border-dashed text-muted-foreground space-y-2">
          <Shield className="h-10 w-10 mx-auto text-muted-foreground/50" />
          <p className="font-medium text-foreground">No permissions match your filters</p>
          <p className="text-xs">Try clearing the search query or changing category view.</p>
        </div>
      ) : (
        <ScrollArea className="h-[430px] rounded-lg border bg-muted/20 p-4">
          <div className="space-y-6">
            {filteredCategories.map(({ category, items, totalCount, selectedCount }) => {
              const isCategoryFull = selectedCount === totalCount && totalCount > 0;
              return (
                <div key={category} className="space-y-3 rounded-lg border bg-card p-4 shadow-sm">
                  {/* Category Header */}
                  <div className="flex items-center justify-between pb-2 border-b">
                    <div className="flex items-center gap-2.5">
                      <div className="p-1.5 rounded-md bg-primary/10 text-primary">
                        {getCategoryIcon(category)}
                      </div>
                      <div>
                        <h3 className="font-semibold text-sm flex items-center gap-2">
                          <span>{category}</span>
                          <Badge variant="outline" className="text-xs font-normal">
                            {selectedCount} of {totalCount} selected
                          </Badge>
                        </h3>
                      </div>
                    </div>

                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleCategory(items)}
                      className="h-8 text-xs gap-1">
                      {isCategoryFull ? (
                        <>
                          <Square className="h-3.5 w-3.5" />
                          Deselect All
                        </>
                      ) : (
                        <>
                          <CheckSquare className="h-3.5 w-3.5" />
                          Select All
                        </>
                      )}
                    </Button>
                  </div>

                  {/* Grid of Permission Items */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-2.5">
                    {items.map((permission) => {
                      const isSelected = selectedPermissions.has(permission.key);
                      const isPlatformOnly = PLATFORM_ONLY_PERMISSIONS.has(permission.key);
                      const isLocked = isRegionalRole && isPlatformOnly;

                      return (
                        <div
                          key={permission.key}
                          onClick={() => {
                            if (!isLocked) handleTogglePermission(permission.key);
                          }}
                          className={`flex items-start gap-3 p-3 rounded-lg border transition-all cursor-pointer select-none ${
                            isLocked
                              ? "opacity-50 bg-muted/50 cursor-not-allowed border-dashed"
                              : isSelected
                              ? "bg-primary/5 border-primary/40 shadow-xs"
                              : "hover:bg-accent/40 bg-card"
                          }`}>
                          <Checkbox
                            id={`permission-${permission.key}`}
                            checked={isSelected}
                            disabled={isLocked}
                            onCheckedChange={() => handleTogglePermission(permission.key)}
                            className="mt-0.5"
                          />
                          <div className="flex-1 min-w-0 space-y-1">
                            <div className="flex items-center justify-between gap-1">
                              <Label
                                htmlFor={`permission-${permission.key}`}
                                className={`text-sm font-semibold truncate ${
                                  isLocked ? "cursor-not-allowed text-muted-foreground" : "cursor-pointer"
                                }`}>
                                {permission.name}
                              </Label>
                              {isPlatformOnly ? (
                                <Badge
                                  variant="outline"
                                  className="text-[10px] shrink-0 border-amber-500/30 text-amber-600 dark:text-amber-400 bg-amber-500/5">
                                  {isLocked ? "Locked" : "Platform Only"}
                                </Badge>
                              ) : (
                                <Badge
                                  variant="outline"
                                  className="text-[10px] shrink-0 border-emerald-500/30 text-emerald-600 dark:text-emerald-400 bg-emerald-500/5">
                                  Regional Capable
                                </Badge>
                              )}
                            </div>

                            <code className="text-[11px] font-mono text-muted-foreground block truncate">
                              {permission.key}
                            </code>

                            {permission.description && (
                              <p className="text-xs text-muted-foreground line-clamp-2">
                                {permission.description}
                              </p>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        </ScrollArea>
      )}

      {/* Sticky Bottom Actions Bar */}
      <Separator />
      <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-1">
        <div className="flex items-center gap-3 text-xs text-muted-foreground">
          <div>
            Total Selected: <strong className="text-foreground text-sm font-bold">{selectedPermissions.size}</strong> of{" "}
            {allPermissions.length}
          </div>
          {hasChanges ? (
            <Badge variant="outline" className="border-amber-500/40 text-amber-600 dark:text-amber-400 bg-amber-500/10 font-medium">
              ● Unsaved changes
            </Badge>
          ) : (
            <Badge variant="outline" className="text-muted-foreground">
              ✓ In sync
            </Badge>
          )}
        </div>

        <div className="flex items-center gap-2 w-full sm:w-auto justify-end">
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isSaving}
            className="h-9">
            Cancel
          </Button>
          <Button
            type="button"
            onClick={handleSave}
            disabled={isSaving || !hasChanges || isLoadingPermissions}
            className="h-9 gap-1.5 min-w-[120px]">
            {isSaving ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin mr-1" />
                Saving...
              </>
            ) : (
              "Save Changes"
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}

function arePermissionSetsEqual(set1: Set<string>, set2: Set<string>): boolean {
  if (set1.size !== set2.size) return false;
  for (const item of set1) {
    if (!set2.has(item)) return false;
  }
  return true;
}
