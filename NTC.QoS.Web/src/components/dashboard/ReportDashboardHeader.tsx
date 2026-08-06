"use client";

import { cn } from "@/lib/utils";

const ReportDashboardHeader = () => {
  return (
    <section className="my-4 min-w-full px-2">
      <div className="mb-1 mt-5 flex items-center justify-between px-2 py-1">
        <h2 className={cn("text-3xl font-bold")}>Reports</h2>
      </div>
    </section>
  );
};

export default ReportDashboardHeader;
