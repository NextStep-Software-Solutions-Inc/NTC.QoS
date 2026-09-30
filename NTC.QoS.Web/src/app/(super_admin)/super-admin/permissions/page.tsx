"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Check,
  Copy,
  Globe,
  Key,
  Layers,
  Lock,
  MapPin,
  Search,
  ShieldCheck,
} from "lucide-react";
import { toast } from "sonner";

import { getPermissionsRequest } from "@/components/features/permission/handlers/getPermissionsRequest";
import type { Permission } from "@/components/features/permission/types/Permission";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";

const useGetPermissions = createQueryHookFromTemplate(getPermissionsRequest);

const PLATFORM_ONLY_PERMISSIONS = new Set([
  "system:admin",
  "system:logs",
  "system:settings",
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

export default function PermissionsManagementPage() {
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string>("all");
  const [copiedKey, setCopiedKey] = useState<string | null>(null);

  const { page, search, setSearch } = useUrlPagination();

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchQuery);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery, setSearch]);

  const { data: permissionsData, isLoading } = useGetPermissions({
    pathParams: {},
    queryParams: {
      pageNumber: page,
      pageSize: 100, // Load all permissions for instant categorized client browsing
      search: search || undefined,
    },
  });

  const allPermissions = useMemo(() => permissionsData?.data ?? [], [permissionsData?.data]);
  const totalCount = permissionsData?.totalRows || 0;

  // Extract unique categories
  const categories = useMemo(() => {
    const set = new Set<string>();
    allPermissions.forEach((p) => {
      if (p.category) set.add(p.category);
    });
    return Array.from(set).sort();
  }, [allPermissions]);

  // Filter permissions by active category
  const displayedPermissions = useMemo(() => {
    if (selectedCategory === "all") return allPermissions;
    if (selectedCategory === "platform-only") {
      return allPermissions.filter((p) => PLATFORM_ONLY_PERMISSIONS.has(p.key));
    }
    if (selectedCategory === "regional") {
      return allPermissions.filter((p) => !PLATFORM_ONLY_PERMISSIONS.has(p.key));
    }
    return allPermissions.filter((p) => p.category === selectedCategory);
  }, [allPermissions, selectedCategory]);

  const stats = useMemo(() => {
    const platformOnlyCount = allPermissions.filter((p) => PLATFORM_ONLY_PERMISSIONS.has(p.key)).length;
    const regionalCapableCount = allPermissions.length - platformOnlyCount;
    return { platformOnlyCount, regionalCapableCount, categoriesCount: categories.length };
  }, [allPermissions, categories]);

  const copyToClipboard = (key: string) => {
    navigator.clipboard.writeText(key);
    setCopiedKey(key);
    toast.success(`Copied "${key}" to clipboard`);
    setTimeout(() => setCopiedKey(null), 2000);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2.5">
            <ShieldCheck className="h-8 w-8 text-primary" />
            Security Permissions Catalog
          </h1>
          <p className="text-muted-foreground mt-1">
            Definitive system authorization registry governing all platform and regional RBAC policies.
          </p>
        </div>
      </div>

      {/* KPI Stats Banner */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Registered Permissions</CardDescription>
            <CardTitle className="text-2xl font-bold">{totalCount}</CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">In-memory static catalog</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Functional Categories</CardDescription>
            <CardTitle className="text-2xl font-bold text-primary flex items-center justify-between">
              <span>{stats.categoriesCount}</span>
              <Layers className="h-5 w-5 text-primary/50" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Domain boundaries</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Regional Capable</CardDescription>
            <CardTitle className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 flex items-center justify-between">
              <span>{stats.regionalCapableCount}</span>
              <MapPin className="h-5 w-5 text-emerald-500/50" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">Regional role assignable</CardContent>
        </Card>

        <Card className="shadow-xs">
          <CardHeader className="p-4 pb-2">
            <CardDescription className="text-xs">Platform Protected</CardDescription>
            <CardTitle className="text-2xl font-bold text-amber-600 dark:text-amber-400 flex items-center justify-between">
              <span>{stats.platformOnlyCount}</span>
              <Lock className="h-5 w-5 text-amber-500/50" />
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 pt-0 text-xs text-muted-foreground">National officers only</CardContent>
        </Card>
      </div>

      {/* Main Table Card */}
      <Card className="shadow-xs">
        <CardHeader className="pb-3">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            {/* Horizontal Category Scroll */}
            <div className="overflow-x-auto pb-1 max-w-2xl">
              <Tabs value={selectedCategory} onValueChange={setSelectedCategory} className="w-full">
                <TabsList className="bg-muted/70 flex w-max h-9 p-1">
                  <TabsTrigger value="all" className="text-xs px-3">
                    All ({allPermissions.length})
                  </TabsTrigger>
                  <TabsTrigger value="regional" className="text-xs px-3">
                    Regional Scope ({stats.regionalCapableCount})
                  </TabsTrigger>
                  <TabsTrigger value="platform-only" className="text-xs px-3">
                    Platform Only ({stats.platformOnlyCount})
                  </TabsTrigger>
                  {categories.map((cat) => (
                    <TabsTrigger key={cat} value={cat} className="text-xs px-3">
                      {cat}
                    </TabsTrigger>
                  ))}
                </TabsList>
              </Tabs>
            </div>

            {/* Search Box */}
            <div className="relative w-full md:w-72">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search key, name, description..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 h-9 text-sm"
              />
            </div>
          </div>
        </CardHeader>

        <CardContent>
          {isLoading ? (
            <div className="text-center py-16 text-muted-foreground">Loading security catalog...</div>
          ) : displayedPermissions.length === 0 ? (
            <div className="text-center py-16 text-muted-foreground border rounded-lg border-dashed">
              <Key className="h-10 w-10 mx-auto text-muted-foreground/40 mb-2" />
              <p className="font-semibold text-foreground">No permissions found</p>
              <p className="text-xs">Adjust category or search filter.</p>
            </div>
          ) : (
            <div className="rounded-lg border overflow-hidden">
              <Table>
                <TableHeader className="bg-muted/50">
                  <TableRow>
                    <TableHead className="w-[280px]">Permission Key</TableHead>
                    <TableHead className="w-[200px]">Display Name</TableHead>
                    <TableHead className="w-[160px]">Domain Category</TableHead>
                    <TableHead className="w-[160px]">Scope Compatibility</TableHead>
                    <TableHead>Functional Description</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {displayedPermissions.map((permission: Permission) => {
                    const isPlatformOnly = PLATFORM_ONLY_PERMISSIONS.has(permission.key);

                    return (
                      <TableRow key={permission.key} className="hover:bg-muted/40 transition-colors">
                        {/* Key with Copy */}
                        <TableCell>
                          <div className="flex items-center gap-1.5">
                            <code className="text-xs bg-muted px-2 py-1 rounded font-mono font-medium text-foreground">
                              {permission.key}
                            </code>
                            <button
                              type="button"
                              onClick={() => copyToClipboard(permission.key)}
                              className="text-muted-foreground hover:text-foreground p-1 rounded transition-colors"
                              title="Copy permission key">
                              {copiedKey === permission.key ? (
                                <Check className="h-3.5 w-3.5 text-emerald-500" />
                              ) : (
                                <Copy className="h-3.5 w-3.5" />
                              )}
                            </button>
                          </div>
                        </TableCell>

                        {/* Name */}
                        <TableCell className="font-semibold text-sm text-foreground">
                          {permission.name}
                        </TableCell>

                        {/* Category */}
                        <TableCell>
                          <Badge variant="outline" className="text-xs font-normal">
                            {permission.category}
                          </Badge>
                        </TableCell>

                        {/* Scope Compatibility */}
                        <TableCell>
                          {isPlatformOnly ? (
                            <Badge
                              variant="outline"
                              className="gap-1 border-amber-500/30 text-amber-600 dark:text-amber-400 bg-amber-500/5 text-xs">
                              <Lock className="h-3 w-3" />
                              Platform Only
                            </Badge>
                          ) : (
                            <Badge
                              variant="outline"
                              className="gap-1 border-emerald-500/30 text-emerald-600 dark:text-emerald-400 bg-emerald-500/5 text-xs">
                              <Globe className="h-3 w-3" />
                              Regional Capable
                            </Badge>
                          )}
                        </TableCell>

                        {/* Description */}
                        <TableCell className="text-xs text-muted-foreground max-w-md">
                          {permission.description || "Grants operational access to this capability."}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
