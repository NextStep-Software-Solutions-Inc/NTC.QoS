"use client";

import { useMemo, useState } from "react";
import {
  AlertCircle,
  Ban,
  CheckCircle2,
  Clock,
  Copy,
  ExternalLink,
  Globe,
  Mail,
  MapPin,
  RefreshCw,
  RotateCw,
  Search,
  Send,
  Shield,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";

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
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";
import { extractErrorMessage } from "@/lib/extract-error-message";
import { getInvitationsRequest } from "./handlers/getInvitationsRequest";
import { resendInvitationRequest } from "./handlers/resendInvitationRequest";
import { revokeInvitationRequest } from "./handlers/revokeInvitationRequest";
import type { Invitation } from "./types/Invitation";

const useGetInvitations = createQueryHookFromTemplate(getInvitationsRequest);
const useRevokeInvitation = createMutationHookFromTemplate(revokeInvitationRequest);
const useResendInvitation = createMutationHookFromTemplate(resendInvitationRequest);

interface PendingInvitationsTableProps {
  onInviteClick?: () => void;
  regionId?: string;
}

export function PendingInvitationsTable({ onInviteClick, regionId }: PendingInvitationsTableProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [selectedInviteToRevoke, setSelectedInviteToRevoke] = useState<Invitation | null>(null);
  const [revokeError, setRevokeError] = useState<string | null>(null);
  const [resendingId, setResendingId] = useState<string | null>(null);

  const {
    data: invitationsResponse,
    isLoading,
    isFetching,
    isError,
    error,
    refetch,
  } = useGetInvitations({
    pathParams: {},
    queryParams: {},
  });

  const revokeMutation = useRevokeInvitation();
  const resendMutation = useResendInvitation();

  const rawInvitations = useMemo(() => invitationsResponse?.data ?? [], [invitationsResponse?.data]);

  const filteredInvitations = useMemo(() => {
    return rawInvitations.filter((inv) => {
      // Status filter
      if (statusFilter !== "all" && inv.status.toLowerCase() !== statusFilter.toLowerCase()) {
        return false;
      }

      // Region filter: if regionId is provided, show invitations assigned to this region OR platform-wide (unrestricted)
      if (regionId) {
        const matchesThisRegion = inv.regions.some((r) => r.regionId === regionId);
        const isPlatformOrGlobal = inv.roleScope === "Platform" || inv.regions.length === 0;
        if (!matchesThisRegion && !isPlatformOrGlobal) {
          return false;
        }
      }

      // Search filter
      if (searchQuery.trim()) {
        const query = searchQuery.toLowerCase();
        const matchesEmail = inv.email.toLowerCase().includes(query);
        const matchesRole = inv.roleName?.toLowerCase().includes(query) ?? false;
        const matchesRegion = inv.regions.some((r) => r.regionName.toLowerCase().includes(query));
        return matchesEmail || matchesRole || matchesRegion;
      }

      return true;
    });
  }, [rawInvitations, statusFilter, searchQuery, regionId]);

  const handleCopyLink = async (invitation: Invitation) => {
    if (!invitation.url) {
      toast.error("Invitation link is not available for this record.");
      return;
    }

    try {
      await navigator.clipboard.writeText(invitation.url);
      toast.success("Invitation link copied to clipboard!");
    } catch {
      toast.error("Failed to copy invitation link to clipboard.");
    }
  };

  const handleConfirmRevoke = async () => {
    if (!selectedInviteToRevoke) return;
    setRevokeError(null);

    try {
      await revokeMutation.mutateAsync({
        pathParams: { id: selectedInviteToRevoke.id },
      });
      toast.success(`Invitation for ${selectedInviteToRevoke.email} has been revoked.`);
      setSelectedInviteToRevoke(null);
      setRevokeError(null);
      refetch();
    } catch (err) {
      const errMsg = extractErrorMessage(err, "Failed to revoke invitation.");
      setRevokeError(errMsg);
      toast.error(errMsg);
    }
  };

  const handleResend = async (invitation: Invitation) => {
    setResendingId(invitation.id);
    toast.promise(
      resendMutation.mutateAsync({
        pathParams: { id: invitation.id },
      }),
      {
        loading: `Resending invitation to ${invitation.email}...`,
        success: () => {
          setResendingId(null);
          refetch();
          return `Invitation for ${invitation.email} was resent successfully! Old links revoked.`;
        },
        error: (err) => {
          setResendingId(null);
          return extractErrorMessage(err, "Failed to resend invitation.");
        },
      }
    );
  };

  const formatDate = (isoString?: string) => {
    if (!isoString) return "N/A";
    try {
      const date = new Date(isoString);
      return new Intl.DateTimeFormat("en-US", {
        month: "short",
        day: "numeric",
        year: "numeric",
        hour: "numeric",
        minute: "2-digit",
      }).format(date);
    } catch {
      return isoString;
    }
  };

  const renderStatusBadge = (status: string) => {
    switch (status.toLowerCase()) {
      case "pending":
        return (
          <Badge variant="outline" className="bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-400 dark:border-amber-800 gap-1 font-medium">
            <Clock className="h-3 w-3" />
            Pending Acceptance
          </Badge>
        );
      case "accepted":
        return (
          <Badge variant="outline" className="bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-400 dark:border-emerald-800 gap-1 font-medium">
            <CheckCircle2 className="h-3 w-3" />
            Accepted
          </Badge>
        );
      case "revoked":
        return (
          <Badge variant="outline" className="bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-950/40 dark:text-rose-400 dark:border-rose-800 gap-1 font-medium">
            <Ban className="h-3 w-3" />
            Revoked
          </Badge>
        );
      default:
        return (
          <Badge variant="secondary" className="capitalize">
            {status}
          </Badge>
        );
    }
  };

  return (
    <div className="space-y-4">
      {/* Search and Filters Toolbar */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        <div className="flex flex-1 items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search invitations by email, role, or region..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-9 h-9 text-sm"
            />
          </div>

          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-[170px] h-9 text-xs">
              <SelectValue placeholder="Filter status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Statuses ({rawInvitations.length})</SelectItem>
              <SelectItem value="pending">
                Pending ({rawInvitations.filter((i) => i.status.toLowerCase() === "pending").length})
              </SelectItem>
              <SelectItem value="accepted">
                Accepted ({rawInvitations.filter((i) => i.status.toLowerCase() === "accepted").length})
              </SelectItem>
              <SelectItem value="revoked">
                Revoked ({rawInvitations.filter((i) => i.status.toLowerCase() === "revoked").length})
              </SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            className="h-9 gap-1.5 text-xs"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? "animate-spin" : ""}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Main Table */}
      <div className="border rounded-lg overflow-hidden bg-card">
        {isLoading ? (
          <div className="text-center py-16 text-muted-foreground flex flex-col items-center gap-2">
            <RefreshCw className="h-6 w-6 animate-spin text-primary" />
            <span className="text-sm">Loading sent invitations from Clerk...</span>
          </div>
        ) : isError ? (
          <div className="text-center py-16 text-muted-foreground border-dashed px-4">
            <AlertCircle className="h-10 w-10 mx-auto text-destructive mb-2" />
            <p className="font-semibold text-foreground">Failed to load invitations</p>
            <p className="text-xs text-muted-foreground max-w-md mx-auto mt-1 mb-4">
              {extractErrorMessage(
                error,
                "A network error occurred while loading pending invitations. Please check your connection and try again."
              )}
            </p>
            <Button variant="outline" size="sm" onClick={() => refetch()} className="gap-1.5">
              <RefreshCw className="h-3.5 w-3.5" />
              Retry
            </Button>
          </div>
        ) : filteredInvitations.length === 0 ? (
          <div className="text-center py-16 text-muted-foreground border-dashed">
            <Send className="h-10 w-10 mx-auto text-muted-foreground/40 mb-2" />
            <p className="font-semibold text-foreground">No invitations found</p>
            <p className="text-xs max-w-sm mx-auto mt-1 mb-4">
              {searchQuery || statusFilter !== "all"
                ? "No invitations match your current search or status filter criteria."
                : "No personnel invitations have been sent yet. Click 'Invite User' to send an onboarding invitation."}
            </p>
            {onInviteClick && !searchQuery && statusFilter === "all" && (
              <Button size="sm" onClick={onInviteClick} className="gap-1.5">
                <Send className="h-3.5 w-3.5" />
                Invite User Now
              </Button>
            )}
          </div>
        ) : (
          <Table>
            <TableHeader className="bg-muted/50">
              <TableRow>
                <TableHead className="w-[300px]">Invited Recipient</TableHead>
                <TableHead className="w-[200px]">Target Role</TableHead>
                <TableHead className="w-[280px]">Target Jurisdictions</TableHead>
                <TableHead className="w-[180px]">Status</TableHead>
                <TableHead className="text-right w-[140px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredInvitations.map((invitation) => {
                const isPending = invitation.status.toLowerCase() === "pending";
                const isPlatform = invitation.roleScope === "Platform";

                return (
                  <TableRow key={invitation.id} className="hover:bg-muted/30 transition-colors">
                    {/* Recipient */}
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <Avatar className="h-9 w-9 border">
                          <AvatarFallback className="text-xs bg-primary/10 text-primary font-medium">
                            {invitation.email.slice(0, 2).toUpperCase()}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <div className="font-medium text-sm text-foreground flex items-center gap-1.5">
                            {invitation.email}
                          </div>
                          <div className="text-xs text-muted-foreground flex items-center gap-1 mt-0.5">
                            <Clock className="h-3 w-3" />
                            Sent {formatDate(invitation.createdAt)}
                          </div>
                        </div>
                      </div>
                    </TableCell>

                    {/* Target Role */}
                    <TableCell>
                      <div className="space-y-1">
                        <div className="flex items-center gap-1.5">
                          <Shield className="h-3.5 w-3.5 text-primary/70" />
                          <span className="font-medium text-sm">
                            {invitation.roleName || "Default Role"}
                          </span>
                        </div>
                        {invitation.roleScope && (
                          <Badge
                            variant="secondary"
                            className={`text-[10px] px-1.5 py-0 ${
                              isPlatform
                                ? "bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300"
                                : "bg-indigo-50 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300"
                            }`}
                          >
                            {invitation.roleScope} Scope
                          </Badge>
                        )}
                      </div>
                    </TableCell>

                    {/* Target Jurisdictions */}
                    <TableCell>
                      {isPlatform ? (
                        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                          <Globe className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
                          <span className="font-medium text-foreground">Nationwide Jurisdiction</span>
                          <span className="text-[10px] text-muted-foreground">(All Regions)</span>
                        </div>
                      ) : invitation.regions.length === 0 ? (
                        <span className="text-xs text-muted-foreground italic">No regions assigned</span>
                      ) : (
                        <div className="flex flex-wrap gap-1">
                          {invitation.regions.slice(0, 3).map((r) => (
                            <Badge
                              key={r.regionId}
                              variant="outline"
                              className="text-[11px] gap-1 px-1.5 py-0.5 border-muted-foreground/30"
                            >
                              <MapPin className="h-2.5 w-2.5 text-primary" />
                              <span>{r.regionCode || r.regionName}</span>
                              {r.roleName && r.roleName !== invitation.roleName && (
                                <span className="text-[9px] text-muted-foreground font-normal">
                                  ({r.roleName})
                                </span>
                              )}
                            </Badge>
                          ))}
                          {invitation.regions.length > 3 && (
                            <Badge variant="secondary" className="text-[10px] px-1.5 py-0">
                              +{invitation.regions.length - 3} more
                            </Badge>
                          )}
                        </div>
                      )}
                    </TableCell>

                    {/* Status */}
                    <TableCell>{renderStatusBadge(invitation.status)}</TableCell>

                    {/* Actions */}
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        {invitation.url && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleCopyLink(invitation)}
                            title="Copy Invitation Link"
                            className="h-8 w-8 p-0 text-muted-foreground hover:text-foreground"
                          >
                            <Copy className="h-3.5 w-3.5" />
                          </Button>
                        )}

                        {isPending && (
                          <>
                            <Button
                              variant="ghost"
                              size="sm"
                              disabled={resendingId === invitation.id}
                              onClick={() => handleResend(invitation)}
                              title="Resend Invitation (Revokes prior links and sends fresh invite)"
                              className="h-8 w-8 p-0 text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:hover:bg-blue-950/30"
                            >
                              <RotateCw className={`h-3.5 w-3.5 ${resendingId === invitation.id ? "animate-spin" : ""}`} />
                            </Button>

                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setSelectedInviteToRevoke(invitation)}
                              title="Revoke Invitation"
                              className="h-8 w-8 p-0 text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/30"
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        )}
      </div>

      {/* Revoke Confirmation Dialog */}
      <AlertDialog
        open={Boolean(selectedInviteToRevoke)}
        onOpenChange={(open) => {
          if (!open) {
            setSelectedInviteToRevoke(null);
            setRevokeError(null);
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2 text-rose-600">
              <Ban className="h-5 w-5" />
              Revoke User Invitation
            </AlertDialogTitle>
            <AlertDialogDescription className="space-y-2">
              <p>
                Are you sure you want to revoke the invitation sent to{" "}
                <strong className="text-foreground font-semibold">{selectedInviteToRevoke?.email}</strong>?
                Once revoked, the invitation link will be deactivated and the recipient will no longer be able
                to use it to sign up.
              </p>
              {revokeError && (
                <div className="rounded-md bg-destructive/10 border border-destructive/20 p-2.5 text-xs text-destructive flex items-start gap-2 mt-2">
                  <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
                  <div className="flex-1">
                    <p className="font-semibold">Network error</p>
                    <p className="text-[11px] text-destructive/90 mt-0.5">{revokeError}</p>
                  </div>
                </div>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={revokeMutation.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={(e) => {
                e.preventDefault();
                handleConfirmRevoke();
              }}
              disabled={revokeMutation.isPending}
              className="bg-rose-600 hover:bg-rose-700 text-white gap-1.5"
            >
              {revokeMutation.isPending && <RefreshCw className="h-3.5 w-3.5 animate-spin" />}
              Revoke Invitation
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
