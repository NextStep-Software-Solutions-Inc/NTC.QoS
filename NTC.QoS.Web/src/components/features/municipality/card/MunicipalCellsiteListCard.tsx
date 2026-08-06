"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { BadgeCheck, ChevronDown, X } from "lucide-react";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ScrollArea } from "@/components/ui/scroll-area";
import { createMutationHookFromTemplate } from "@/hooks/createMutationHookFromTemplate";
import { useDebounceContext } from "@/hooks/useDebounceContext";
import { cn } from "@/lib/utils";
import DeleteCellsDialog from "../../cell/dialogs/DeleteCellsDialog";
import { deleteCellsRequest } from "../../cell/handlers/deleteCellsRequest";
import { editCellStatusRequest } from "../../cell/handlers/editCellStatusRequest";
import { type Cell } from "../../cell/types/Cell";

interface CellSiteListProps {
  setIsView?: (isView: boolean) => void;
  data?: Cell[];
}

const useEditCellStatus = createMutationHookFromTemplate(editCellStatusRequest);
const useDeleteCells = createMutationHookFromTemplate(deleteCellsRequest);
type GroupedProvider = { color?: string; sites: (Cell & { barangayId?: string; barangayName?: string })[] };

// Define the available statuses
const SITE_STATUSES = ["Licensed", "Pending", "Unknown"];

export default function MunicipalCellsiteListCard({ data }: CellSiteListProps) {
  const [cellIdToDelete, setCellToDeleteId] = useState<string | null>(null);
  const { mutateAsync } = useEditCellStatus();
  const { mutateAsync: deleteCells } = useDeleteCells();
  const { region } = useParams<{ region: string }>();
  const queryClient = useQueryClient();
  const { debounceValue } = useDebounceContext();

  const groupedCellsByProvider = useMemo(() => {
    const groups: Record<string, GroupedProvider> = {};
    data
      ?.filter((cell) => cell?.serviceProvider?.service.type === "Wireless")
      ?.forEach((cell) => {
        const providerName = cell?.serviceProvider?.provider?.name || "Unknown Provider";
        const providerColor = cell?.serviceProvider?.provider?.color;
        if (
          !debounceValue ||
          cell.siteId?.toLowerCase().includes(debounceValue.toLowerCase()) ||
          cell?.serviceProvider.provider.name.toString().includes(debounceValue.toLocaleLowerCase()) ||
          cell.address?.toString().includes(debounceValue.toLocaleLowerCase()) ||
          cell.latitude?.toString().includes(debounceValue.toLocaleLowerCase()) ||
          cell.longitude?.toString().includes(debounceValue.toLocaleLowerCase())
        ) {
          groups[providerName] ??= { color: providerColor, sites: [] };
          groups[providerName].sites.push(cell);
        }
      });
    return groups;
  }, [data, debounceValue]);

  const handleStatusChange = async (cellId: string, newStatus: string) => {
    try {
      await mutateAsync(
        { pathParams: { id: cellId }, payload: newStatus },
        {
          onSuccess: () => {
            void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells" });
          },
          onError: (error: unknown) => {
            console.error(error);
          },
        },
      );
    } catch (error) {
      console.error("Failed to update cell status:", error);
    }
  };

  return (
    <div className="w-full space-y-2">
      {Object.entries(groupedCellsByProvider).map(([providerName, { color, sites }]) => (
        <Card key={providerName} className="w-full max-w-screen">
          <CardHeader className="flex flex-row items-center justify-between p-4">
            <div className="flex items-center gap-2">
              <Avatar className="h-8 w-8">
                <AvatarFallback className="bg-gray-300 text-black">{providerName[0]}</AvatarFallback>
              </Avatar>
              <div className="space-y-1">
                <Badge variant="default" style={{ backgroundColor: color ?? "#e0e0e0" }}>
                  {providerName}
                </Badge>
                <p className="text-xs text-muted-foreground">
                  <span className="font-bold text-primary">{sites.length}</span> Cells
                </p>
              </div>
            </div>

            {/* <div className="flex items-end gap-3">
              <Avatar className="h-8 w-8">
                <AvatarFallback>
                  <RadioTower size={16} style={{ color: color ?? "#e0e0e0" }} />
                </AvatarFallback>
              </Avatar>
              <div className="flex flex-col">
                <p className="text-sm text-muted-foreground">Available Site</p>
                <p className="text-sm">{sites.length} Sites</p>
              </div>
            </div> */}
          </CardHeader>

          <CardContent className="space-y-2">
            <ScrollArea className="space-y-2">
              {sites.slice(0, 3).map((site, index) => (
                <div key={site.id || index} className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-7 h-7 rounded-full bg-gray-200 flex items-center justify-center text-sm font-bold">
                      {index + 1}
                    </div>
                    <div>
                      <div className="flex flex-row gap-2 items-center ">
                        <p className="font-semibold text-sm">{site.siteId || "Unknown Site"} </p>
                        {site.isVerified && <BadgeCheck className="text-blue-700 pb-1 " size={20} />}
                      </div>
                      <p className="text-xs text-gray-500">
                        {site.latitude}, {site.longitude}
                      </p>
                      <p className="text-xs text-gray-500 line-clamp-1">{site.address || "No Address Provided"}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {site.isVerified && (
                      <Badge className="text-xs" variant="default">
                        Verified
                      </Badge>
                    )}

                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Badge
                          className={cn(`text-xs cursor-pointer gap-2 bg-gray-500 text-white`, {
                            "bg-green-500 text-white": site.status === "Licensed",
                            "bg-yellow-500 text-white": site.status === "Pending",
                            "bg-red-500 text-white": site.status === "Rejected",
                          })}>
                          {site.status}
                          <ChevronDown size={16} />
                        </Badge>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent>
                        {SITE_STATUSES.map((status) => (
                          <DropdownMenuItem key={status} onClick={() => handleStatusChange(site.id, status)}>
                            {status}
                          </DropdownMenuItem>
                        ))}
                      </DropdownMenuContent>
                    </DropdownMenu>

                    <Link href={`/${region}/map?cellId=${site.id}`}>
                      <Button variant="outline" className="text-sm h-8 px-3">
                        View Details
                      </Button>
                    </Link>

                    <Button onClick={() => setCellToDeleteId(site.id)} variant="outline" className="text-sm h-8 px-3">
                      <X size={16} />
                    </Button>
                  </div>
                </div>
              ))}
            </ScrollArea>
            {sites.length > 3 && (
              <Button variant="outline" className="w-full text-sm mt-2">
                See All
              </Button>
            )}
          </CardContent>
        </Card>
      ))}
      {cellIdToDelete && (
        <DeleteCellsDialog
          open={!!cellIdToDelete}
          onOpenChange={(open) => {
            if (!open) {
              setCellToDeleteId(null);
              void queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] === "get-cells", refetchType: "active" });
            }
          }}
          idsToDelete={[cellIdToDelete]}
          onConfirmDelete={async (idsChunk) => {
            await deleteCells({ pathParams: {}, queryParams: { ids: idsChunk } });
          }}
        />
      )}
    </div>
  );
}
