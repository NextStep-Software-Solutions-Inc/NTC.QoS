"use client";

import React, { useCallback, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { motion } from "framer-motion";
import {
  AlertTriangle,
  Building2,
  Check,
  CheckCircle2,
  ChevronLeft,
  Copy,
  Edit,
  ExternalLink,
  Globe,
  MapPin,
  Navigation,
  RadioTower,
  Ruler,
  Satellite,
  Save,
  Shield,
  Signal,
  Trash,
  Wrench,
  X,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";

import { deleteCellsRequest } from "@/components/features/cell/handlers/deleteCellsRequest";
import { editCellRequest } from "@/components/features/cell/handlers/editCellRequest";
import { editCellStatusRequest } from "@/components/features/cell/handlers/editCellStatusRequest";
import { getCellDetailsRequest } from "@/components/features/cell/handlers/getCellDetailsRequest";
import { getCellComplianceRequest } from "@/components/features/cell/handlers/getCellComplianceRequest";
import { getCellTechnologiesRequest } from "@/components/features/cell/handlers/getCellTechnologiesRequest";
import { getServiceProvidersRequest } from "@/components/features/service-provider/handlers/getServiceProviders";
import { getTechnologiesRequest } from "@/components/features/technology/handlers/getTechnologiesRequest";
import CellSectorsTab from "../tabs/CellSectorsTab";
import { type ServiceProviderDetails } from "@/components/features/service-provider/types/serviceProvider";
import { type Technology } from "@/components/features/technology/types/Technology";
import { type CellTechnology } from "@/components/features/cell/types/Cell";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useGetCellDetails = createQueryHookFromTemplate(getCellDetailsRequest);
const useGetCellTechnologies = createQueryHookFromTemplate(getCellTechnologiesRequest);
const useGetCellCompliance = createQueryHookFromTemplate(getCellComplianceRequest);
const useUpdateCell = createMutationHookFromTemplate(editCellRequest);
const useUpdateCellStatus = createMutationHookFromTemplate(editCellStatusRequest);
const useDeleteCells = createMutationHookFromTemplate(deleteCellsRequest);
const useServiceProviders = createQueryHookFromTemplate(getServiceProvidersRequest);
const useGetAllTechnologies = createQueryHookFromTemplate(getTechnologiesRequest);

interface CellsiteDetailPageProps {
  regionId: string;
  cellId: string;
}

// Status badge styling
const statusStyles = {
  unknown: {
    bg: "bg-amber-100",
    text: "text-amber-800",
    border: "border-amber-300",
    icon: AlertTriangle,
    label: "Unknown",
  },
  pending: {
    bg: "bg-orange-100",
    text: "text-orange-800",
    border: "border-orange-300",
    icon: CheckCircle2,
    label: "Pending",
  },
  licensed: {
    bg: "bg-green-100",
    text: "text-green-800",
    border: "border-green-300",
    icon: CheckCircle2,
    label: "Licensed",
  },
};

export default function CellsiteDetailPage({ regionId, cellId }: CellsiteDetailPageProps) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [isEditing, setIsEditing] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [editingStatus, setEditingStatus] = useState(false);
  const [pendingStatus, setPendingStatus] = useState<string>("");

  // Editable field state
  const [editForm, setEditForm] = useState({
    name: "",
    status: "",
    serviceProviderId: "",
    group: "",
    address: "",
    cid: "",
    lac: "",
    tac: "",
    siteId: "",
    towerType: "",
    elevation: 0,
    latitude: 0,
    longitude: 0,
    isVerified: false,
  });

  // Fetch cell details
  const { data: cellResponse, isLoading, refetch } = useGetCellDetails({ pathParams: { id: cellId }, queryParams: {} });

  // Fetch cell technologies
  const {
    data: technologiesResponse,
    isLoading: technologiesLoading,
    refetch: refetchTechnologies,
  } = useGetCellTechnologies({ pathParams: { id: cellId }, queryParams: {} });
  const technologies = technologiesResponse?.data ?? [];

  // Fetch cell compliance
  const {
    data: complianceResponse,
    isLoading: complianceLoading,
  } = useGetCellCompliance({ pathParams: { id: cellId }, queryParams: {} });
  const compliance = complianceResponse?.data;

  // Fetch service providers (wireless only) for editing
  const { data: serviceProvidersResponse } = useServiceProviders({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const wirelessServiceProviders = React.useMemo(
    () => serviceProvidersResponse?.data?.filter((sp: ServiceProviderDetails) => sp.service.type === "Wireless") ?? [],
    [serviceProvidersResponse?.data],
  );

  // Fetch all available technologies for editing
  const { data: allTechnologiesResponse } = useGetAllTechnologies({
    pathParams: {},
    queryParams: { pageSize: 100, pageNumber: 1 },
  });
  const allTechnologies = allTechnologiesResponse?.data ?? [];

  const cell = cellResponse?.data;

  // Mutations
  const updateCellMutation = useUpdateCell();
  const updateStatusMutation = useUpdateCellStatus();
  const deleteMutation = useDeleteCells();

  // Initialize edit form when cell data loads
  React.useEffect(() => {
    if (cell && !isEditing) {
      setEditForm({
        name: cell.name || "",
        status: cell.status || "Unknown",
        serviceProviderId: cell.serviceProviderId || "",
        group: cell.group || cell.serviceProvider?.provider?.name || "",
        address: cell.address || "",
        cid: cell.cid || "",
        lac: cell.lac || "",
        tac: cell.tac || "",
        siteId: cell.siteId || "",
        towerType: cell.towerType || "",
        elevation: cell.elevation || 0,
        latitude: cell.latitude || 0,
        longitude: cell.longitude || 0,
        isVerified: cell.isVerified || false,
      });
    }
  }, [cell, isEditing]);

  // Auto-enter edit mode if edit query parameter is present
  React.useEffect(() => {
    const editParam = searchParams.get("edit");
    const editStatusParam = searchParams.get("editStatus");
    if (editParam === "true" && cell && !isEditing) {
      setIsEditing(true);
    }
    if (editStatusParam === "true" && cell && !editingStatus) {
      setEditingStatus(true);
      setPendingStatus(cell.status || "Unknown");
    }
  }, [searchParams, cell, isEditing, editingStatus]);

  const handleGoBack = useCallback(() => {
    router.push(`/${regionId}/broadband/cellsites`);
  }, [router, regionId]);

  const handleEdit = useCallback(() => {
    setIsEditing(true);
  }, []);

  const handleCancelEdit = useCallback(() => {
    setIsEditing(false);
    // Reset form to original values
    if (cell) {
      setEditForm({
        name: cell.name || "",
        status: cell.status || "Unknown",
        serviceProviderId: cell.serviceProviderId || "",
        group: cell.group || cell.serviceProvider?.provider?.name || "",
        address: cell.address || "",
        cid: cell.cid || "",
        lac: cell.lac || "",
        tac: cell.tac || "",
        siteId: cell.siteId || "",
        towerType: cell.towerType || "",
        elevation: cell.elevation || 0,
        latitude: cell.latitude || 0,
        longitude: cell.longitude || 0,
        isVerified: cell.isVerified || false,
      });
    }
  }, [cell]);

  const handleSaveEdit = useCallback(async () => {
    try {
      // Update cell details
      await updateCellMutation.mutateAsync({
        pathParams: { id: cellId },
        payload: {
          name: editForm.name || undefined,
          serviceProviderId: editForm.serviceProviderId || undefined,
          group: editForm.group || undefined,
          address: editForm.address || undefined,
          cid: editForm.cid || undefined,
          lac: editForm.lac || undefined,
          tac: editForm.tac || undefined,
          siteId: editForm.siteId || undefined,
          towerType: editForm.towerType || undefined,
          elevation: editForm.elevation || undefined,
          latitude: editForm.latitude || undefined,
          longitude: editForm.longitude || undefined,
          isVerified: editForm.isVerified,
        },
      });

      // Update status if changed
      if (editForm.status !== cell?.status) {
        try {
          await updateStatusMutation.mutateAsync({
            pathParams: { id: cellId },
            payload: editForm.status,
          });
        } catch {
          toast.error("Cell site details saved, but failed to update status.");
          void refetch();
          void refetchTechnologies();
          setIsEditing(false);
          return;
        }
      }

      toast.success("Cell site updated successfully");
      setIsEditing(false);

      // Reload browser to display updates
      setTimeout(() => {
        window.location.reload();
      }, 500);
    } catch (error) {
      toast.error("Failed to update cell site");
      console.error("Update error:", error);
    }
  }, [updateCellMutation, updateStatusMutation, cellId, editForm, cell?.status, refetch, refetchTechnologies]);

  const handleDelete = useCallback(async () => {
    try {
      await deleteMutation.mutateAsync({ pathParams: {}, queryParams: { ids: [cellId] } });
      toast.success("Cell site deleted successfully");
      router.push(`/${regionId}/broadband/cellsites`);
    } catch (error) {
      toast.error("Failed to delete cell site");
      console.error("Delete error:", error);
    }
    setDeleteDialogOpen(false);
  }, [deleteMutation, cellId, router, regionId]);

  const handleStatusSave = useCallback(async () => {
    try {
      await updateStatusMutation.mutateAsync({ pathParams: { id: cellId }, payload: pendingStatus });
      toast.success(`Status updated to ${pendingStatus}`);
      setEditingStatus(false);
      setPendingStatus("");
      
      // Navigate back to detail page without edit mode
      router.push(`/${regionId}/broadband/cellsites/${cellId}`);
      
      // Reload browser to display updates
      setTimeout(() => {
        window.location.reload();
      }, 500);
    } catch (error) {
      toast.error("Failed to update status");
      console.error("Status update error:", error);
    }
  }, [updateStatusMutation, cellId, pendingStatus, router, regionId]);

  const handleCancelStatus = useCallback(() => {
    setEditingStatus(false);
    setPendingStatus("");
  }, []);

  const copyToClipboard = useCallback((text: string, field: string) => {
    navigator.clipboard.writeText(text);
    setCopiedField(field);
    toast.success("Copied to clipboard");
    setTimeout(() => setCopiedField(null), 2000);
  }, []);

  const openInMaps = useCallback(() => {
    if (cell?.latitude && cell?.longitude) {
      window.open(`https://www.google.com/maps/search/?api=1&query=${cell.latitude},${cell.longitude}`, "_blank");
    }
  }, [cell]);

  const status = cell?.status?.toLowerCase() || "unknown";
  const statusStyle = statusStyles[status as keyof typeof statusStyles] || statusStyles.unknown;
  const StatusIcon = statusStyle.icon;

  if (isLoading) {
    return <CellsiteDetailSkeleton />;
  }

  if (!cell) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[50vh] gap-4">
        <RadioTower className="h-16 w-16 text-gray-400" />
        <h2 className="text-xl font-semibold text-gray-600">Cell site not found</h2>
        <Button onClick={handleGoBack} variant="outline">
          <ChevronLeft className="h-4 w-4 mr-2" />
          Back to Cell Sites
        </Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50/30 to-indigo-50/20 dark:from-gray-900 dark:via-gray-900 dark:to-gray-900">
      {/* Hero Header */}
      <motion.div
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        className="relative overflow-hidden"
        style={{ background: "linear-gradient(135deg, rgb(59 130 246), rgb(99 102 241), rgb(139 92 246))" }}>
        {/* Decorative elements */}
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-20 -right-20 w-64 h-64 rounded-full bg-white/10 blur-3xl" />
          <div className="absolute -bottom-20 -left-20 w-64 h-64 rounded-full bg-purple-500/20 blur-3xl" />
        </div>

        <div className="relative px-6 py-8">
          {/* Back button and actions */}
          <div className="flex items-center justify-between mb-6">
            <Button variant="ghost" onClick={handleGoBack} className="text-white/90 hover:text-white hover:bg-white/20">
              <ChevronLeft className="h-5 w-5 mr-1" />
              Back to Cell Sites
            </Button>

            <div className="flex items-center gap-2">
              {isEditing ? (
                <>
                  <Button
                    variant="ghost"
                    onClick={handleCancelEdit}
                    className="text-white/90 hover:text-white hover:bg-white/20"
                    disabled={updateCellMutation.isPending}>
                    <X className="h-4 w-4 mr-2" />
                    Cancel
                  </Button>
                  <Button
                    onClick={handleSaveEdit}
                    className="bg-white text-blue-600 hover:bg-white/90"
                    disabled={updateCellMutation.isPending}>
                    <Save className="h-4 w-4 mr-2" />
                    {updateCellMutation.isPending ? "Saving..." : "Save Changes"}
                  </Button>
                </>
              ) : (
                <>
                  <Button
                    variant="ghost"
                    onClick={handleEdit}
                    className="text-white/90 hover:text-white hover:bg-white/20">
                    <Edit className="h-4 w-4 mr-2" />
                    Edit
                  </Button>
                  <Button
                    variant="ghost"
                    onClick={() => setDeleteDialogOpen(true)}
                    className="text-white/90 hover:text-white hover:bg-red-500/20">
                    <Trash className="h-4 w-4 mr-2" />
                    Delete
                  </Button>
                </>
              )}
            </div>
          </div>

          {/* Cell info header */}
          <div className="flex flex-col md:flex-row items-start md:items-center gap-6">
            {/* Icon */}
            <motion.div
              initial={{ scale: 0.8, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              transition={{ delay: 0.1 }}
              className="p-5 rounded-2xl bg-white/20 backdrop-blur-sm border border-white/30">
              <RadioTower className="h-12 w-12 text-white" />
            </motion.div>

            {/* Title and badges */}
            <div className="flex-1 space-y-2">
              {/* Site ID at top */}
              {cell.siteId && (
                <div className="text-white/80">
                  <span className="font-mono text-sm bg-white/10 px-2 py-1 rounded">ID: {cell.siteId}</span>
                </div>
              )}

              {/* Cell name and badges on same line */}
              <div className="flex flex-wrap items-center gap-3">
                {isEditing ? (
                  <Textarea
                    value={editForm.name}
                    onChange={(e) => setEditForm((prev) => ({ ...prev, name: e.target.value }))}
                    className="text-2xl md:text-3xl font-bold bg-white/20 text-white border-white/30 placeholder:text-white/50 resize-none"
                    placeholder="Enter cell site name"
                    rows={2}
                  />
                ) : (
                  <h1 className="text-3xl md:text-4xl font-bold text-white">
                    {cell.name || cell.siteId || "Unnamed Cell Site"}
                  </h1>
                )}

                {/* Verified and Status badges aligned with name */}
                <div className="flex items-center gap-2">
                  {/* Verified badge */}
                  {cell.isVerified && (
                    <div className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-blue-500/30 text-white font-medium text-sm border border-blue-400/30">
                      <Shield className="h-4 w-4" />
                      Verified
                    </div>
                  )}

                  {/* Status badge */}
                  {isEditing ? (
                    <Select 
                      value={editForm.status || status} 
                      onValueChange={(value) => setEditForm((prev) => ({ ...prev, status: value }))}>
                      <SelectTrigger className="w-48 bg-white/20 border-white/30 text-white">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Unknown">Unknown</SelectItem>
                        <SelectItem value="Pending">Pending</SelectItem>
                        <SelectItem value="Licensed">Licensed</SelectItem>
                      </SelectContent>
                    </Select>
                  ) : editingStatus ? (
                    <div className="flex items-center gap-2">
                      <Select value={pendingStatus} onValueChange={setPendingStatus}>
                        <SelectTrigger className="w-40 bg-white/20 border-white/30 text-white h-9">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Unknown">Unknown</SelectItem>
                          <SelectItem value="Pending">Pending</SelectItem>
                          <SelectItem value="Licensed">Licensed</SelectItem>
                        </SelectContent>
                      </Select>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleStatusSave}
                        disabled={updateStatusMutation.isPending}
                        className="h-9 text-green-400 hover:!text-green-300 hover:!bg-green-500/20 transition-all">
                        <Check className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleCancelStatus}
                        disabled={updateStatusMutation.isPending}
                        className="h-9 text-white/70 hover:!text-white hover:!bg-white/10 transition-all">
                        <X className="h-4 w-4" />
                      </Button>
                    </div>
                  ) : (
                    <div
                      className={`flex items-center gap-2 px-3 py-1.5 rounded-full ${statusStyle.bg} ${statusStyle.text} font-medium text-sm border ${statusStyle.border || "border-white/20"}`}>
                      <StatusIcon className="h-4 w-4" />
                      {statusStyle.label}
                    </div>
                  )}
                </div>
              </div>

              {/* Provider in the middle */}
              {isEditing ? (
                <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
                  <div className="flex flex-col gap-1.5">
                    <span className="text-white/70 text-xs font-medium">Service Provider</span>
                    <Select
                      value={editForm.serviceProviderId}
                      onValueChange={(providerId) => {
                        const selectedProvider = wirelessServiceProviders.find(
                          (sp: ServiceProviderDetails) => sp.id === providerId,
                        );
                        setEditForm((prev) => ({
                          ...prev,
                          serviceProviderId: providerId,
                          group: selectedProvider?.provider.name ?? prev.group,
                        }));
                      }}>
                      <SelectTrigger className="w-56 bg-white/20 border-white/30 text-white h-9">
                        <SelectValue placeholder="Select provider" />
                      </SelectTrigger>
                      <SelectContent>
                        {wirelessServiceProviders.map((sp: ServiceProviderDetails) => (
                          <SelectItem key={sp.id} value={sp.id}>
                            {sp.provider.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <span className="text-white/70 text-xs font-medium">Group</span>
                    <input
                      type="text"
                      value={editForm.group}
                      onChange={(e) => setEditForm((prev) => ({ ...prev, group: e.target.value }))}
                      placeholder="Group"
                      className="h-9 w-44 px-3 rounded-md bg-white/20 border border-white/30 text-white placeholder:text-white/50 text-sm focus:outline-none focus:ring-1 focus:ring-white/50"
                    />
                  </div>
                </div>
              ) : (
                cell.serviceProvider?.provider?.name && (
                  <div className="flex items-center gap-2 text-white/80">
                    {cell.serviceProvider.provider.logo ? (
                      <img
                        src={cell.serviceProvider.provider.logo}
                        alt={cell.serviceProvider.provider.name}
                        className="h-6 w-6 rounded object-contain bg-white p-0.5"
                      />
                    ) : (
                      <div
                        className="h-6 w-6 rounded flex items-center justify-center text-xs font-bold text-white"
                        style={{ backgroundColor: cell.serviceProvider.provider.color || "#6b7280" }}>
                        {cell.serviceProvider.provider.name.substring(0, 2).toUpperCase()}
                      </div>
                    )}
                    <span>{cell.serviceProvider.provider.name}</span>
                  </div>
                )
              )}
            </div>
          </div>
        </div>
      </motion.div>

      {/* Main Content */}
      <div className="px-6 py-8 -mt-4">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left Column - Key Information */}
          <motion.div
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.2 }}
            className="space-y-6">
            {/* Location Card */}
            <Card className="overflow-hidden shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <MapPin className="h-5 w-5 text-blue-500" />
                  Location
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Mini map preview */}
                <div className="relative h-48 rounded-lg overflow-hidden bg-gradient-to-br from-blue-100 to-indigo-100 dark:from-blue-900/30 dark:to-indigo-900/30">
                  <div className="absolute inset-0 flex items-center justify-center">
                    <div className="text-center">
                      <Globe className="h-12 w-12 text-blue-400 mx-auto mb-2" />
                      <p className="text-sm text-gray-500">Map Preview</p>
                    </div>
                  </div>
                  <Button variant="secondary" size="sm" className="absolute bottom-2 right-2" onClick={openInMaps}>
                    <ExternalLink className="h-4 w-4 mr-1" />
                    Open in Maps
                  </Button>
                </div>

                {/* Coordinates */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
                    <div className="flex items-center gap-2">
                      <Navigation className="h-4 w-4 text-gray-500" />
                      <span className="text-sm text-gray-600 dark:text-gray-400">Latitude</span>
                    </div>
                    <div className="flex items-center gap-2">
                      {isEditing ? (
                        <Input
                          type="number"
                          step="0.000001"
                          value={editForm.latitude || ""}
                          onChange={(e) => setEditForm((prev) => ({ ...prev, latitude: Number(e.target.value) }))}
                          className="w-[180px] h-8 text-sm"
                        />
                      ) : (
                        <>
                          <code className="text-sm font-mono">{cell.latitude?.toFixed(6)}</code>
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-7 w-7 p-0"
                            onClick={() => copyToClipboard(`${cell.latitude}`, "latitude")}>
                            {copiedField === "latitude" ? (
                              <Check className="h-3 w-3 text-green-500" />
                            ) : (
                              <Copy className="h-3 w-3" />
                            )}
                          </Button>
                        </>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
                    <div className="flex items-center gap-2">
                      <Navigation className="h-4 w-4 text-gray-500" />
                      <span className="text-sm text-gray-600 dark:text-gray-400">Longitude</span>
                    </div>
                    <div className="flex items-center gap-2">
                      {isEditing ? (
                        <Input
                          type="number"
                          step="0.000001"
                          value={editForm.longitude || ""}
                          onChange={(e) => setEditForm((prev) => ({ ...prev, longitude: Number(e.target.value) }))}
                          className="w-[180px] h-8 text-sm"
                        />
                      ) : (
                        <>
                          <code className="text-sm font-mono">{cell.longitude?.toFixed(6)}</code>
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-7 w-7 p-0"
                            onClick={() => copyToClipboard(`${cell.longitude}`, "longitude")}>
                            {copiedField === "longitude" ? (
                              <Check className="h-3 w-3 text-green-500" />
                            ) : (
                              <Copy className="h-3 w-3" />
                            )}
                          </Button>
                        </>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
                    <div className="flex items-center gap-2">
                      <Ruler className="h-4 w-4 text-gray-500" />
                      <span className="text-sm text-gray-600 dark:text-gray-400">Elevation</span>
                    </div>
                    {isEditing ? (
                      <Input
                        type="number"
                        value={editForm.elevation || 0}
                        onChange={(e) => setEditForm((prev) => ({ ...prev, elevation: Number(e.target.value) }))}
                        className="w-[180px] h-8 text-sm"
                      />
                    ) : (
                      <span className="text-sm font-medium">{cell.elevation || 0} m</span>
                    )}
                  </div>

                  {(cell.address || isEditing) && (
                    <div className="p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
                      <div className="flex items-center gap-2 mb-1">
                        <Building2 className="h-4 w-4 text-gray-500" />
                        <span className="text-sm text-gray-600 dark:text-gray-400">Address</span>
                      </div>
                      {isEditing ? (
                        <Input
                          value={editForm.address}
                          onChange={(e) => setEditForm((prev) => ({ ...prev, address: e.target.value }))}
                          placeholder="Full street address"
                          className="text-sm h-8"
                        />
                      ) : (
                        <p className="text-sm">{cell.address}</p>
                      )}
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Technical Details Card */}
            <Card className="overflow-hidden shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <Signal className="h-5 w-5 text-indigo-500" />
                  Technical Details
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <InfoRow
                  label="CID"
                  value={cell.cid || "N/A"}
                  copyable
                  onCopy={copyToClipboard}
                  copiedField={copiedField}
                  isEditing={isEditing}
                  editValue={editForm.cid}
                  onEditChange={(value) => setEditForm((prev) => ({ ...prev, cid: String(value) }))}
                />
                <InfoRow
                  label="LAC"
                  value={cell.lac || "N/A"}
                  copyable
                  onCopy={copyToClipboard}
                  copiedField={copiedField}
                  isEditing={isEditing}
                  editValue={editForm.lac}
                  onEditChange={(value) => setEditForm((prev) => ({ ...prev, lac: String(value) }))}
                />
                <InfoRow
                  label="TAC"
                  value={cell.tac || "N/A"}
                  copyable
                  onCopy={copyToClipboard}
                  copiedField={copiedField}
                  isEditing={isEditing}
                  editValue={editForm.tac}
                  onEditChange={(value) => setEditForm((prev) => ({ ...prev, tac: String(value) }))}
                />
                <InfoRow
                  label="Site ID"
                  value={cell.siteId || "N/A"}
                  isEditing={isEditing}
                  editValue={editForm.siteId}
                  onEditChange={(value) => setEditForm((prev) => ({ ...prev, siteId: String(value) }))}
                />
                <InfoRow
                  label="Tower Type"
                  value={cell.towerType || "N/A"}
                  isEditing={isEditing}
                  editValue={editForm.towerType}
                  onEditChange={(value) => setEditForm((prev) => ({ ...prev, towerType: String(value) }))}
                  fieldType="select"
                  selectOptions={[
                    { label: "Unknown", value: "Unknown" },
                    { label: "Guyed", value: "Guyed" },
                    { label: "Self Supporting", value: "SelfSupporting" },
                    { label: "Monopole", value: "Monopole" },
                    { label: "Stealth", value: "Stealth" },
                    { label: "Rooftop", value: "Rooftop" },
                    { label: "Wall Mounted", value: "WallMounted" },
                  ]}
                />
                <InfoRow label="Group" value={cell.group || "N/A"} isEditing={isEditing} editValue={editForm.group} onEditChange={(value) => setEditForm((prev) => ({ ...prev, group: String(value) }))} />
              </CardContent>
            </Card>
          </motion.div>

          {/* Right Column - Tabs */}
          <motion.div
            initial={{ opacity: 0, x: 20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.3 }}
            className="lg:col-span-2">
            <Tabs defaultValue="overview" className="space-y-6">
              <TabsList className="grid grid-cols-2 md:grid-cols-5 gap-2 bg-white/80 dark:bg-gray-800/80 p-1 rounded-lg shadow-sm">
                <TabsTrigger
                  value="overview"
                  className="data-[state=active]:bg-blue-500 data-[state=active]:text-white">
                  Overview
                </TabsTrigger>
                <TabsTrigger
                  value="cell-sectors"
                  className="data-[state=active]:bg-blue-500 data-[state=active]:text-white">
                  Cell Sectors
                </TabsTrigger>
                <TabsTrigger
                  value="technologies"
                  className="data-[state=active]:bg-blue-500 data-[state=active]:text-white">
                  Technologies
                </TabsTrigger>
                <TabsTrigger
                  value="coverage"
                  className="data-[state=active]:bg-blue-500 data-[state=active]:text-white">
                  Coverage
                </TabsTrigger>
                <TabsTrigger
                  value="compliance"
                  className="data-[state=active]:bg-blue-500 data-[state=active]:text-white">
                  Compliance
                </TabsTrigger>
              </TabsList>

              <TabsContent value="overview">
                <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
                  <CardHeader>
                    <CardTitle>Cell Site Overview</CardTitle>
                    <CardDescription>Complete information about this cell site</CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    {/* Description */}
                    {cell.description && (
                      <div>
                        <h4 className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-2">Description</h4>
                        <p className="text-sm">{cell.description}</p>
                      </div>
                    )}

                    {/* Cell Image */}
                    {cell.image && (
                      <div>
                        <h4 className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-2">Site Image</h4>
                        <div className="relative aspect-video rounded-lg overflow-hidden bg-gray-100 dark:bg-gray-900">
                          <img src={cell.image} alt={cell.name || "Cell site"} className="object-cover w-full h-full" />
                        </div>
                      </div>
                    )}

                    {/* Verification */}
                    <div className="p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <Shield className="h-4 w-4 text-gray-500" />
                          <span className="text-sm text-gray-600 dark:text-gray-400">Verified</span>
                        </div>
                        {isEditing ? (
                          <Checkbox
                            checked={editForm.isVerified}
                            onCheckedChange={(checked) =>
                              setEditForm((prev) => ({ ...prev, isVerified: checked === true }))
                            }
                          />
                        ) : (
                          <span className="text-sm font-medium">{cell.isVerified ? "Yes" : "No"}</span>
                        )}
                      </div>
                    </div>

                    {/* Stats Grid */}
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                      <StatCard
                        icon={<Satellite className="h-5 w-5" />}
                        label="Status"
                        value={statusStyle.label}
                        color={status === "active" ? "emerald" : status === "maintenance" ? "amber" : "slate"}
                      />
                      <StatCard
                        icon={<Shield className="h-5 w-5" />}
                        label="Verified"
                        value={cell.isVerified ? "Yes" : "No"}
                        color={cell.isVerified ? "blue" : "gray"}
                      />
                      <StatCard
                        icon={<Ruler className="h-5 w-5" />}
                        label="Elevation"
                        value={`${cell.elevation || 0}m`}
                        color="purple"
                      />
                      <StatCard
                        icon={<Signal className="h-5 w-5" />}
                        label="Tower Type"
                        value={cell.towerType || "N/A"}
                        color="indigo"
                      />
                    </div>
                  </CardContent>
                </Card>
              </TabsContent>

              <TabsContent value="technologies">
                <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
                  <CardHeader>
                    <CardTitle>Supported Technologies</CardTitle>
                    <CardDescription>Network technologies available at this cell site</CardDescription>
                  </CardHeader>
                  <CardContent>
                    {technologiesLoading ? (
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {[1, 2, 3].map((i) => (
                          <Skeleton key={i} className="h-32 rounded-lg" />
                        ))}
                      </div>
                    ) : technologies.length === 0 ? (
                      <div className="flex flex-col items-center justify-center py-12 text-center">
                        <Signal className="h-12 w-12 text-gray-300 mb-3" />
                        <p className="text-sm text-gray-500">No technologies assigned to this cell site.</p>
                      </div>
                    ) : (
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {technologies.map((tech) => (
                          <div
                            key={tech.technologyId}
                            className="p-4 rounded-lg bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20 border border-blue-100 dark:border-blue-800">
                            <div className="flex items-center gap-2 mb-1">
                              <Signal className="h-5 w-5 text-blue-500 shrink-0" />
                              <span className="font-semibold text-blue-900 dark:text-blue-200">
                                {tech.technologyStandard}
                              </span>
                            </div>
                            {tech.technologyDescription && (
                              <p className="text-xs text-gray-500 mb-3">{tech.technologyDescription}</p>
                            )}
                            {tech.bands && tech.bands.length > 0 && (
                              <div className="flex flex-wrap gap-2">
                                {tech.bands.map((band) => (
                                  <Tooltip key={band.bandId}>
                                    <TooltipTrigger asChild>
                                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-800/40 dark:text-blue-200 border border-blue-200 dark:border-blue-700 cursor-default">
                                        {band.bandName}
                                      </span>
                                    </TooltipTrigger>
                                    {band.bandDescription && (
                                      <TooltipContent>{band.bandDescription}</TooltipContent>
                                    )}
                                  </Tooltip>
                                ))}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    )}
                  </CardContent>
                </Card>
              </TabsContent>

              <TabsContent value="cell-sectors">
                {cell && (
                  <CellSectorsTab
                    cellId={cellId}
                    cell={cell}
                  />
                )}
              </TabsContent>

              <TabsContent value="coverage">
                <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
                  <CardHeader>
                    <CardTitle>Coverage Area</CardTitle>
                    <CardDescription>Signal coverage and range information</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="h-64 rounded-lg bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20 flex items-center justify-center">
                      <div className="text-center">
                        <Globe className="h-12 w-12 text-green-400 mx-auto mb-2" />
                        <p className="text-sm text-gray-500">Coverage map coming soon</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </TabsContent>

              <TabsContent value="history">
                <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
                  <CardHeader>
                    <CardTitle>Activity History</CardTitle>
                    <CardDescription>Recent changes and updates to this cell site</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {[
                        {
                          action: "Status changed to Active",
                          time: "2 hours ago",
                          icon: CheckCircle2,
                          color: "text-green-500",
                        },
                        { action: "Cell site verified", time: "1 day ago", icon: Shield, color: "text-blue-500" },
                        { action: "Coordinates updated", time: "3 days ago", icon: MapPin, color: "text-purple-500" },
                        { action: "Cell site created", time: "1 week ago", icon: RadioTower, color: "text-indigo-500" },
                      ].map((item, i) => (
                        <div key={i} className="flex items-center gap-4 p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
                          <div className={`${item.color}`}>
                            <item.icon className="h-5 w-5" />
                          </div>
                          <div className="flex-1">
                            <p className="text-sm font-medium">{item.action}</p>
                            <p className="text-xs text-gray-500">{item.time}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                    <p className="text-sm text-gray-500 mt-4 text-center">
                      History data is placeholder. Connect to API for real data.
                    </p>
                  </CardContent>
                </Card>
              </TabsContent>

              <TabsContent value="compliance">
                <Card className="shadow-lg border-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Shield className="h-5 w-5" />
                      Frequency Compliance
                    </CardTitle>
                    <CardDescription>
                      Authorization status of each technology band against frequency allocations
                    </CardDescription>
                  </CardHeader>
                  <CardContent>
                    {complianceLoading ? (
                      <div className="space-y-4">
                        {[1, 2, 3].map((i) => (
                          <Skeleton key={i} className="h-28 rounded-lg" />
                        ))}
                      </div>
                    ) : !compliance ? (
                      <div className="flex flex-col items-center justify-center py-12 text-center">
                        <Shield className="h-12 w-12 text-gray-300 mb-3" />
                        <p className="text-sm text-gray-500">No compliance data available.</p>
                      </div>
                    ) : (
                      <>
                        {/* Overall status banner */}
                        <div
                          className={[
                            "mb-6 flex items-center gap-3 rounded-lg border px-4 py-3",
                            compliance.overallStatus === "Compliant"
                              ? "bg-green-50 border-green-200 text-green-800 dark:bg-green-900/20 dark:border-green-700 dark:text-green-300"
                              : compliance.overallStatus === "Non-Compliant"
                                ? "bg-red-50 border-red-200 text-red-800 dark:bg-red-900/20 dark:border-red-700 dark:text-red-300"
                                : "bg-amber-50 border-amber-200 text-amber-800 dark:bg-amber-900/20 dark:border-amber-700 dark:text-amber-300",
                          ].join(" ")}>
                          {compliance.overallStatus === "Compliant" ? (
                            <CheckCircle2 className="h-5 w-5 shrink-0" />
                          ) : compliance.overallStatus === "Non-Compliant" ? (
                            <XCircle className="h-5 w-5 shrink-0" />
                          ) : (
                            <AlertTriangle className="h-5 w-5 shrink-0" />
                          )}
                          <div className="flex-1">
                            <span className="font-semibold text-sm">Overall Status: {compliance.overallStatus}</span>
                            <span className="ml-4 text-xs font-normal opacity-75">
                              {compliance.items.filter((i) => i.isAuthorized).length} of {compliance.items.length} bands authorized
                            </span>
                          </div>
                        </div>

                        {/* Grouped items: provider → technology → bands */}
                        {(() => {
                          // Group: serviceProviderId → technologyStandard → items[]
                          type GroupedItems = Record<string, Record<string, typeof compliance.items>>;
                          const byProvider = compliance.items.reduce<GroupedItems>((acc, item) => {
                            if (!acc[item.serviceProviderId]) acc[item.serviceProviderId] = {};
                            const provGroup = acc[item.serviceProviderId]!;
                            if (!provGroup[item.technologyStandard]) provGroup[item.technologyStandard] = [];
                            provGroup[item.technologyStandard]!.push(item);
                            return acc;
                          }, {});

                          return Object.entries(byProvider).map(([providerId, byTech]) => {
                            const providerName = Object.values(byTech).flat()[0]?.providerName ?? providerId;
                            return (
                            <div key={providerId} className="mb-5">
                              {/* Provider header */}
                              <div className="flex items-center gap-2 mb-3">
                                <Building2 className="h-4 w-4 text-gray-400 shrink-0" />
                                <span className="text-sm font-semibold text-gray-700 dark:text-gray-200">{providerName}</span>
                              </div>

                              {/* Tech groups */}
                              <div className="space-y-3 pl-1">
                                {Object.entries(byTech).map(([techStandard, items]) => (
                                  <div
                                    key={techStandard}
                                    className="rounded-lg border bg-gray-50/60 dark:bg-gray-900/30 dark:border-gray-700 overflow-hidden">
                                    {/* Tech header row */}
                                    <div className="flex items-center gap-2 px-3 py-2 border-b dark:border-gray-700 bg-gray-100/80 dark:bg-gray-800/60">
                                      <Signal className="h-4 w-4 text-blue-500 shrink-0" />
                                      <span className="text-xs font-semibold tracking-wide text-blue-700 dark:text-blue-300 uppercase">
                                        {techStandard}
                                      </span>
                                      <span className="ml-auto text-[10px] text-gray-400">
                                        {items.filter((i) => i.isAuthorized).length}/{items.length} authorized
                                      </span>
                                    </div>

                                    {/* Band rows */}
                                    <div className="divide-y dark:divide-gray-700">
                                      {items.map((item) => (
                                        <div
                                          key={item.bandId}
                                          className="flex items-center gap-3 px-3 py-2">
                                          {/* Band name */}
                                          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-800/40 dark:text-blue-200 border border-blue-200 dark:border-blue-700 shrink-0">
                                            {item.bandName}
                                          </span>

                                          {/* Authorized badge */}
                                          {item.isAuthorized ? (
                                            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-800/30 dark:text-green-300 border border-green-200 dark:border-green-700 shrink-0">
                                              <Check className="h-3 w-3" />
                                              Authorized
                                            </span>
                                          ) : (
                                            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800 dark:bg-red-800/30 dark:text-red-300 border border-red-200 dark:border-red-700 shrink-0">
                                              <X className="h-3 w-3" />
                                              Unauthorized
                                            </span>
                                          )}

                                          {/* Matched allocation */}
                                          {item.matchedAllocationName && (
                                            <span className="text-xs text-gray-500 dark:text-gray-400 truncate">
                                              {item.matchedAllocationName}
                                              {item.matchedAllocationNotes && (
                                                <span className="italic ml-1 opacity-70">— {item.matchedAllocationNotes}</span>
                                              )}
                                            </span>
                                          )}
                                        </div>
                                      ))}
                                    </div>
                                  </div>
                                ))}
                              </div>
                            </div>
                          ); });
                        })()}
                      </>
                    )}
                  </CardContent>
                </Card>
              </TabsContent>
            </Tabs>
          </motion.div>
        </div>
      </div>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Cell Site</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete &quot;{cell.name || cell.siteId}&quot;? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-red-600 hover:bg-red-700">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

// Helper Components
function InfoRow({
  label,
  value,
  copyable = false,
  onCopy,
  copiedField,
  isEditing = false,
  editValue,
  onEditChange,
  fieldType = "text",
  selectOptions,
}: {
  label: string;
  value: string;
  copyable?: boolean;
  onCopy?: (text: string, field: string) => void;
  copiedField?: string | null;
  isEditing?: boolean;
  editValue?: string | number;
  onEditChange?: (value: string | number) => void;
  fieldType?: "text" | "number" | "select";
  selectOptions?: { label: string; value: string }[];
}) {
  return (
    <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg">
      <span className="text-sm text-gray-600 dark:text-gray-400">{label}</span>
      <div className="flex items-center gap-2">
        {isEditing && onEditChange ? (
          fieldType === "select" ? (
            <Select value={String(editValue || "")} onValueChange={onEditChange}>
              <SelectTrigger className="w-[180px] h-8">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {selectOptions?.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          ) : (
            <Input
              type={fieldType}
              value={editValue || ""}
              onChange={(e) => onEditChange(fieldType === "number" ? Number(e.target.value) : e.target.value)}
              className="w-[180px] h-8 text-sm"
            />
          )
        ) : (
          <span className="text-sm font-medium font-mono">{value}</span>
        )}
        {!isEditing && copyable && onCopy && value !== "N/A" && (
          <Button variant="ghost" size="sm" className="h-7 w-7 p-0" onClick={() => onCopy(value, label.toLowerCase())}>
            {copiedField === label.toLowerCase() ? (
              <Check className="h-3 w-3 text-green-500" />
            ) : (
              <Copy className="h-3 w-3" />
            )}
          </Button>
        )}
      </div>
    </div>
  );
}

function StatCard({
  icon,
  label,
  value,
  color,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  color: string;
}) {
  const colorClasses: Record<string, string> = {
    emerald: "from-emerald-500 to-green-500",
    amber: "from-amber-500 to-orange-500",
    slate: "from-slate-500 to-gray-500",
    blue: "from-blue-500 to-cyan-500",
    gray: "from-gray-400 to-gray-500",
    purple: "from-purple-500 to-violet-500",
    indigo: "from-indigo-500 to-blue-500",
  };

  return (
    <div className="p-4 rounded-lg bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-900 border border-gray-200 dark:border-gray-700">
      <div
        className={`inline-flex p-2 rounded-lg bg-gradient-to-br ${colorClasses[color] || colorClasses.gray} text-white mb-2`}>
        {icon}
      </div>
      <p className="text-xs text-gray-500 dark:text-gray-400">{label}</p>
      <p className="text-lg font-semibold">{value}</p>
    </div>
  );
}

function CellsiteDetailSkeleton() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50/30 to-indigo-50/20 dark:from-gray-900 dark:via-gray-900 dark:to-gray-900">
      {/* Hero Skeleton */}
      <div className="bg-gradient-to-r from-blue-500 to-indigo-600 px-6 py-8">
        <Skeleton className="h-10 w-40 bg-white/20 mb-6" />
        <div className="flex items-center gap-6">
          <Skeleton className="h-24 w-24 rounded-2xl bg-white/20" />
          <div className="space-y-3">
            <Skeleton className="h-10 w-64 bg-white/20" />
            <Skeleton className="h-6 w-48 bg-white/20" />
            <Skeleton className="h-8 w-24 bg-white/20 rounded-full" />
          </div>
        </div>
      </div>

      {/* Content Skeleton */}
      <div className="px-6 py-8 -mt-4">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="space-y-6">
            <Card className="p-6">
              <Skeleton className="h-6 w-24 mb-4" />
              <Skeleton className="h-48 w-full rounded-lg mb-4" />
              <div className="space-y-3">
                <Skeleton className="h-12 w-full rounded-lg" />
                <Skeleton className="h-12 w-full rounded-lg" />
                <Skeleton className="h-12 w-full rounded-lg" />
              </div>
            </Card>
          </div>
          <div className="lg:col-span-2">
            <Skeleton className="h-12 w-full rounded-lg mb-6" />
            <Card className="p-6">
              <Skeleton className="h-6 w-32 mb-4" />
              <Skeleton className="h-32 w-full rounded-lg" />
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
