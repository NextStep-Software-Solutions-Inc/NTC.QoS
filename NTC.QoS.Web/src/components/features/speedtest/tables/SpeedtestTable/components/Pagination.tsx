"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, Layers } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { cn } from "@/lib/utils";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  totalCount: number;
}

export function Pagination({
  currentPage,
  totalPages,
  pageSize,
  onPageChange,
  onPageSizeChange,
  totalCount,
}: PaginationProps) {
  const [jumpToPage, setJumpToPage] = useState("");

  const handleJumpToPage = () => {
    const pageNumber = parseInt(jumpToPage);
    if (!isNaN(pageNumber) && pageNumber >= 1 && pageNumber <= totalPages) {
      onPageChange(pageNumber);
      setJumpToPage("");
    }
  };

  // Calculate progress percentage
  const progressPercent = totalPages > 0 ? (currentPage / totalPages) * 100 : 0;

  return (
    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="relative">
      {/* Progress indicator bar - colorful gradient */}
      <div className="absolute top-0 left-0 right-0 h-[3px] bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
        <motion.div
          initial={{ width: 0 }}
          animate={{ width: `${progressPercent}%` }}
          transition={{ duration: 0.5, ease: "easeOut" }}
          className="h-full bg-gradient-to-r from-cyan-500 via-blue-500 to-purple-500"
        />
      </div>

      <div className="flex flex-wrap items-center justify-between gap-4 px-6 py-4 pt-5">
        {/* Left section - Page size selector */}
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
            <Layers className="h-4 w-4 text-blue-500" />
            <span>Show:</span>
          </div>
          <Select value={pageSize.toString()} onValueChange={(value) => onPageSizeChange(parseInt(value))}>
            <SelectTrigger className="h-8 w-[72px] border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 shadow-sm text-sm font-medium">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="10">10</SelectItem>
              <SelectItem value="20">20</SelectItem>
              <SelectItem value="50">50</SelectItem>
              <SelectItem value="100">100</SelectItem>
              <SelectItem value="500">500</SelectItem>
              <SelectItem value="1000">1000</SelectItem>
            </SelectContent>
          </Select>
          <Badge
            variant="outline"
            className="font-medium text-xs bg-gradient-to-r from-cyan-50 to-blue-50 dark:from-cyan-950/30 dark:to-blue-950/30 border-cyan-300/50 dark:border-cyan-700/50 text-cyan-700 dark:text-cyan-300">
            {totalCount.toLocaleString()} total
          </Badge>
        </div>

        {/* Right section - Navigation */}
        <div className="flex items-center gap-4">
          {/* Jump to page */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-gray-600 dark:text-gray-400">Go to:</span>
            <Input
              type="number"
              min={1}
              max={totalPages}
              value={jumpToPage}
              onChange={(e) => setJumpToPage(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  handleJumpToPage();
                }
              }}
              className="h-8 w-14 text-center text-sm border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 shadow-sm"
              placeholder={currentPage.toString()}
            />
            <span className="text-sm text-gray-500 dark:text-gray-400">/ {totalPages.toLocaleString()}</span>
          </div>

          {/* Page navigation buttons */}
          <div className="flex items-center gap-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => onPageChange(1)}
              disabled={currentPage === 1}
              className={cn(
                "h-7 w-7 rounded-md transition-all",
                currentPage === 1 ? "opacity-40" : "hover:bg-white dark:hover:bg-gray-700 hover:shadow-sm",
              )}>
              <ChevronsLeft className="h-3.5 w-3.5" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              onClick={() => onPageChange(currentPage - 1)}
              disabled={currentPage === 1}
              className={cn(
                "h-7 w-7 rounded-md transition-all",
                currentPage === 1 ? "opacity-40" : "hover:bg-white dark:hover:bg-gray-700 hover:shadow-sm",
              )}>
              <ChevronLeft className="h-3.5 w-3.5" />
            </Button>

            {/* Current page indicator */}
            <div className="px-3 py-1 mx-1 rounded-md bg-gradient-to-r from-cyan-500 to-blue-600 text-white text-xs font-bold shadow-sm">
              {currentPage.toLocaleString()}
            </div>

            <Button
              variant="ghost"
              size="icon"
              onClick={() => onPageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
              className={cn(
                "h-7 w-7 rounded-md transition-all",
                currentPage === totalPages ? "opacity-40" : "hover:bg-white dark:hover:bg-gray-700 hover:shadow-sm",
              )}>
              <ChevronRight className="h-3.5 w-3.5" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              onClick={() => onPageChange(totalPages)}
              disabled={currentPage === totalPages}
              className={cn(
                "h-7 w-7 rounded-md transition-all",
                currentPage === totalPages ? "opacity-40" : "hover:bg-white dark:hover:bg-gray-700 hover:shadow-sm",
              )}>
              <ChevronsRight className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      </div>
    </motion.div>
  );
}
