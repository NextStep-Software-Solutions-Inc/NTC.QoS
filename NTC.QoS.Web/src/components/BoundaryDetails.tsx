"use client";

import Link from "next/link";
import { ExternalLink } from "lucide-react";

interface CellSiteListProps {
  label?: string;
  addButton?: React.ReactNode;
  children?: React.ReactNode;
  externalLink?: string;
}

export default function BoundaryDetails({ label, addButton, children, externalLink }: CellSiteListProps) {
  return (
    <div className="">
      <div className="flex  items-center mx-2">
        <h1 className="text-xl font-bold flex-1">{label}</h1>
        <div className="flex items-center space-x-2">
          {addButton}
          <Link
            prefetch={false}
            href={externalLink ?? "#"}
            className="hover:text-blue-500 border p-2 rounded-md border-gray-200 ">
            <ExternalLink size={16} />
          </Link>
        </div>
      </div>
      {children}
    </div>
  );
}
