"use client";

import { useEffect, useState } from "react";
import { Edit, FileText, Loader2, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { createReportTemplateRequest } from "@/components/features/report-template/handlers/createReportTemplateRequest";
import { deleteReportTemplateRequest } from "@/components/features/report-template/handlers/deleteReportTemplateRequest";
import { getReportTemplateRequest } from "@/components/features/report-template/handlers/getReportTemplateRequest";
import { getReportTemplatesRequest } from "@/components/features/report-template/handlers/getReportTemplatesRequest";
import { updateReportTemplateRequest } from "@/components/features/report-template/handlers/updateReportTemplateRequest";
import type { ReportTemplateListItem } from "@/components/features/report-template/types/ReportTemplate";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
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
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Pagination } from "@/components/ui/pagination";
import { Switch } from "@/components/ui/switch";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { useUrlPagination } from "@/hooks/useUrlPagination";
import { buildEndpoint } from "@/lib/endpoint-builder";
import { request } from "@/services/fetch";

const useGetAllReportTemplates = createQueryHookFromTemplate(getReportTemplatesRequest);
const useDeleteReportTemplate = createMutationHookFromTemplate(deleteReportTemplateRequest);
const useCreateReportTemplate = createMutationHookFromTemplate(createReportTemplateRequest);
const useEditReportTemplate = createMutationHookFromTemplate(updateReportTemplateRequest);

export default function ReportTemplatesManagementPage() {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedTemplate, setSelectedTemplate] = useState<ReportTemplateListItem | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [isLoadingTemplateDetails, setIsLoadingTemplateDetails] = useState(false);
  const [formValues, setFormValues] = useState({
    title: "",
    description: "",
    instruction: "",
    isActive: true,
  });

  const { page, pageSize, search, setPage, setPageSize, setSearch } = useUrlPagination();

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchQuery);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery, setSearch]);

  const {
    data: templatesData,
    isLoading,
    refetch,
  } = useGetAllReportTemplates({ pathParams: {}, queryParams: { pageNumber: page, pageSize: pageSize, search: search || undefined } });

  const { mutateAsync: deleteTemplate, isPending: isDeleting } = useDeleteReportTemplate();
  const { mutateAsync: createTemplate, isPending: isCreating } = useCreateReportTemplate();
  const { mutateAsync: editTemplate, isPending: isEditing } = useEditReportTemplate();

  const templates = templatesData?.data ?? [];
  const totalCount = templatesData?.totalRows || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    toast.promise(
      createTemplate({
        pathParams: {},
        payload: {
          title: formValues.title,
          description: formValues.description || null,
          instruction: formValues.instruction,
        },
      }),
      {
        loading: "Creating report template...",
        success: () => {
          setCreateDialogOpen(false);
          setFormValues({ title: "", description: "", instruction: "", isActive: true });
          void refetch();
          return "Report template created successfully";
        },
        error: "Failed to create report template",
      },
    );
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedTemplate) return;
    toast.promise(
      editTemplate({
        pathParams: { id: selectedTemplate.id },
        payload: {
          title: formValues.title || null,
          description: formValues.description || null,
          instruction: formValues.instruction || null,
          isActive: formValues.isActive,
        },
      }),
      {
        loading: "Updating report template...",
        success: () => {
          setEditDialogOpen(false);
          setSelectedTemplate(null);
          setFormValues({ title: "", description: "", instruction: "", isActive: true });
          void refetch();
          return "Report template updated successfully";
        },
        error: "Failed to update report template",
      },
    );
  };

  const handleDelete = async () => {
    if (!selectedTemplate) return;
    toast.promise(deleteTemplate({ pathParams: { id: selectedTemplate.id } }), {
      loading: "Deleting report template...",
      success: () => {
        setDeleteDialogOpen(false);
        setSelectedTemplate(null);
        void refetch();
        return "Report template deleted successfully";
      },
      error: "Failed to delete report template",
    });
  };

  const openEditDialog = async (template: ReportTemplateListItem) => {
    setSelectedTemplate(template);
    setIsLoadingTemplateDetails(true);
    setEditDialogOpen(true);
    
    try {
      // Fetch full template details including instruction and isActive
      const url = buildEndpoint(getReportTemplateRequest.endpoint, { id: template.id }, undefined);
      const response = await request.get<{ data: { title: string | null; description: string | null; instruction: string | null; isActive: boolean } }>(url);
      
      setFormValues({
        title: response.data.title || "",
        description: response.data.description || "",
        instruction: response.data.instruction || "",
        isActive: response.data.isActive,
      });
    } catch {
      // Fallback to list item data if fetch fails
      setFormValues({
        title: template.title || "",
        description: template.description || "",
        instruction: "",
        isActive: true,
      });
      toast.error("Failed to load template details");
    } finally {
      setIsLoadingTemplateDetails(false);
    }
  };

  const openDeleteDialog = (template: ReportTemplateListItem) => {
    setSelectedTemplate(template);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">Report Templates Management</h1>
          <p className="text-muted-foreground mt-1">
            Manage AI report templates for dynamic report generation
          </p>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Create Template
        </Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                <FileText className="h-5 w-5" />
                Report Templates ({totalCount})
              </CardTitle>
              <CardDescription>AI-powered report generation templates</CardDescription>
            </div>
            <div className="w-full sm:w-64">
              <Input
                placeholder="Search templates..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8">Loading report templates...</div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Title</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {templates.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={3} className="text-center py-8 text-muted-foreground">
                        No report templates found
                      </TableCell>
                    </TableRow>
                  ) : (
                    templates.map((template) => (
                      <TableRow key={template.id}>
                        <TableCell className="font-medium">{template.title || "-"}</TableCell>
                        <TableCell className="text-muted-foreground max-w-md truncate">
                          {template.description || "-"}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex justify-end gap-2">
                            <Button variant="ghost" size="sm" onClick={() => openEditDialog(template)}>
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button variant="ghost" size="sm" onClick={() => openDeleteDialog(template)}>
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          )}

          {!isLoading && totalCount > 0 && (
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <ResponsiveDialog
        open={createDialogOpen}
        onOpenChange={setCreateDialogOpen}
        title="Create New Report Template"
        description="Add a new template for AI-powered report generation">
        <form onSubmit={handleCreate} className="space-y-4">
          <div>
            <Label htmlFor="create-title">
              Title <span className="text-red-500">*</span>
            </Label>
            <Input
              id="create-title"
              value={formValues.title}
              onChange={(e) => setFormValues({ ...formValues, title: e.target.value })}
              placeholder="e.g., Network Coverage Analysis"
              maxLength={200}
              required
            />
          </div>
          <div>
            <Label htmlFor="create-description">Description</Label>
            <textarea
              id="create-description"
              className="w-full rounded border border-input p-2 text-sm"
              value={formValues.description}
              onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
              placeholder="Brief description of the template"
              rows={2}
              maxLength={500}
            />
          </div>
          <div>
            <Label htmlFor="create-instruction">
              AI Instruction <span className="text-red-500">*</span>
            </Label>
            <textarea
              id="create-instruction"
              className="w-full rounded border border-input p-2 text-sm"
              value={formValues.instruction}
              onChange={(e) => setFormValues({ ...formValues, instruction: e.target.value })}
              placeholder="Enter the AI instruction that will guide report generation..."
              rows={5}
              required
            />
            <p className="text-xs text-muted-foreground mt-1">
              This instruction will guide AI when generating reports using this template.
            </p>
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isCreating}>
              {isCreating ? "Creating..." : "Create"}
            </Button>
          </div>
        </form>
      </ResponsiveDialog>

      {/* Edit Dialog */}
      <ResponsiveDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        title="Edit Report Template"
        description="Update template information">
        {isLoadingTemplateDetails ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-primary" />
            <span className="ml-2 text-sm text-muted-foreground">Loading template details...</span>
          </div>
        ) : (
          <form onSubmit={handleEdit} className="space-y-4">
            <div>
              <Label htmlFor="edit-title">Title</Label>
              <Input
                id="edit-title"
                value={formValues.title}
                onChange={(e) => setFormValues({ ...formValues, title: e.target.value })}
                maxLength={200}
              />
            </div>
            <div>
              <Label htmlFor="edit-description">Description</Label>
              <textarea
                id="edit-description"
                className="w-full rounded border border-input p-2 text-sm"
                value={formValues.description}
                onChange={(e) => setFormValues({ ...formValues, description: e.target.value })}
                rows={2}
                maxLength={500}
              />
            </div>
            <div>
              <Label htmlFor="edit-instruction">AI Instruction</Label>
              <textarea
                id="edit-instruction"
                className="w-full rounded border border-input p-2 text-sm"
                value={formValues.instruction}
              onChange={(e) => setFormValues({ ...formValues, instruction: e.target.value })}
              placeholder="Enter the AI instruction..."
              rows={5}
            />
            <p className="text-xs text-muted-foreground mt-1">
              This instruction will guide AI when generating reports using this template.
            </p>
          </div>
          <div className="flex items-center space-x-3">
            <Switch
              id="edit-isActive"
              checked={formValues.isActive}
              onCheckedChange={(checked) => setFormValues({ ...formValues, isActive: checked })}
            />
            <Label htmlFor="edit-isActive">Active</Label>
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => setEditDialogOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isEditing}>
              {isEditing ? "Updating..." : "Update"}
            </Button>
          </div>
        </form>
        )}
      </ResponsiveDialog>

      {/* Delete Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Report Template</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete the template &quot;{selectedTemplate?.title}&quot;? This
              action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={isDeleting}>
              {isDeleting ? "Deleting..." : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
