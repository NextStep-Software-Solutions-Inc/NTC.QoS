"use client";

import React, { Fragment, useCallback, useState } from "react";
import Link from "next/link";
import { type CheckedState } from "@radix-ui/react-checkbox";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { cn } from "@/lib/utils";
import { Separator } from "../ui/separator";
import { Skeleton } from "../ui/skeleton";
import WithPlaceholder from "../WithPlaceholder";

export interface GenericListItem {
  id: string;
  name: string;
  href: string;
}

interface GenericListProps<T extends GenericListItem> {
  items: T[];
  searchPlaceholder?: string;
  onClick?: (id: string) => void;
  onEdit?: (id: string) => void;
  renderItemExtra?: (item: T) => React.ReactNode;
  addComponent?: React.ReactNode;
  deleteComponent?: React.ReactNode;
  selectedClassName?: string;
  getSelectedItemsIds?: (selectedIds: Set<string>) => void;
  activeClassName?: string;
  activeItemIds?: Set<string>;
  isLoading: boolean;
}

// Define a placeholder for the list items
const randomLength = Math.floor(Math.random() * (15 - 10 + 1)) + 8;
const listPlaceholder = Array.from({ length: randomLength }).map((_, index) => (
  <div key={index} className="flex items-center space-x-4 p-3 shadow-md border rounded-md">
    <Skeleton className="h-4 w-4 rounded-sm" />
    <div className="flex-1 space-y-2">
      <Skeleton className="h-4 w-full" />
    </div>
    <Skeleton className="h-8 w-[80px]" />
  </div>
));
export default function GenericList<T extends GenericListItem>({
  items,
  searchPlaceholder = "Search...",
  onEdit,
  onClick,
  renderItemExtra,
  addComponent,
  deleteComponent,
  selectedClassName,
  activeClassName,
  activeItemIds = new Set(),
  getSelectedItemsIds,
  isLoading,
}: GenericListProps<T>) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const handleCheck = useCallback(
    (id: string, checked: CheckedState) => {
      setSelectedIds((prev) => {
        const newSelectedIds = new Set(prev);
        if (checked) {
          newSelectedIds.add(id);
        } else {
          newSelectedIds.delete(id);
        }
        if (getSelectedItemsIds) {
          getSelectedItemsIds(newSelectedIds);
        }
        return newSelectedIds;
      });
    },
    [getSelectedItemsIds],
  );

  return (
    <div className="flex flex-col flex-1 min-h-0 border rounded-lg pb-2 md:mb-0 mb-2">
      <div className="m-2 flex flex-wrap items-center justify-between gap-2 ">
        <h2 className="font-semibold">{searchPlaceholder.replace("Search ", "")}</h2>
        <div className="flex items-center gap-1 min-w-[0]">
          <div
            className={cn(
              "transition-all duration-300",
              deleteComponent && selectedIds.size > 0
                ? "ml-[-100px]" // Adjust -56px to match your delete button width
                : "ml-0",
            )}
            style={{ transitionProperty: "margin" }}>
            {addComponent}
          </div>
          {deleteComponent && (
            <div
              className={cn(
                "transition-all duration-300",
                selectedIds.size > 0 ? "opacity-100 scale-100 " : "opacity-0 scale-95 pointer-events-none ml-[-100px]",
              )}
              style={{ transitionProperty: "opacity, transform, margin" }}>
              {deleteComponent}
            </div>
          )}
        </div>
      </div>
      <Separator className="mb-2" />
      <div
        className="flex flex-1 min-h-0 flex-col overflow-auto space-y-2 mx-2"
        style={{
          scrollbarWidth: "none", // Firefox
          msOverflowStyle: "none", // IE 10+
        }}>
        <WithPlaceholder isLoading={isLoading} placeholder={listPlaceholder}>
          {items.map((data) => (
            <Fragment key={data.id}>
              <div
                className={cn("w-full flex items-center justify-between p-3 shadow-md border rounded-md", {
                  "bg-gray-50": selectedIds.has(data.id),
                  "hover:bg-gray-100 cursor-pointer": !selectedIds.has(data.id),
                  ...(selectedClassName && selectedIds.has(data.id) ? { [selectedClassName]: true } : {}),
                  ...(activeClassName && activeItemIds.has(data.id) ? { [activeClassName]: true } : {}),
                })}
                onClick={() => onClick?.(data.id)}>
                <div className="flex items-center gap-2 w-full">
                  <div onClick={(e) => e.stopPropagation()}>
                    <Checkbox
                      checked={selectedIds.has(data.id)}
                      onCheckedChange={(checked) => {
                        if (typeof checked === "boolean") {
                          handleCheck(data.id, checked);
                        }
                      }}
                    />
                  </div>
                  <Link
                    onClick={(e) => e.stopPropagation()}
                    href={data.href}
                    className="hover:underline text-sm truncate">
                    <div className="flex items-center gap-x-1">{data.name}</div>
                  </Link>
                  <div className="flex flex-1 justify-end">{renderItemExtra?.(data)}</div>
                </div>
                {onEdit && (
                  <Button size="sm" variant="outline" onClick={() => onEdit(data.id)}>
                    Edit
                  </Button>
                )}
              </div>
            </Fragment>
          ))}
          {items.length === 0 && (
            <div className="flex justify-center items-center">
              <p>Search term not found</p>
            </div>
          )}
        </WithPlaceholder>
      </div>
    </div>
  );
}
