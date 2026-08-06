"use client";

import React from "react";

import { DatePicker } from "@/components/DatePicker";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { download } from "@/utils/download-blob";

const RegionNameHeader = () => {
  return (
    <section className="my-4 min-w-full px-2">
      <div className="mb-1 mt-5 flex items-center justify-between px-2 py-1 pl-4">
        <h2 className={cn("text-3xl font-bold")}>Regional X</h2>
        <section className="flex items-center text-sm">
          <DatePicker />
          <Button onClick={download} className="h-10 w-24 bg-QOS-blue">
            Download
          </Button>
        </section>
      </div>
    </section>
  );
};

export default RegionNameHeader;
