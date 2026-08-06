import React from "react";

interface BoundaryTabsProps {
  boundaryName?: string;
  description?: string;
  info?: string;
  dialog?: React.ReactNode;
}

const BoundaryNoType = ({ boundaryName, description, info, dialog }: BoundaryTabsProps) => {
  return (
    <div className="px-2">
      <div className="mb-4">
        <h1 className="text-lg font-bold">{boundaryName}</h1>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>

      <div className="border rounded-lg p-6 bg-slate-50">
        <div className="flex flex-col items-center justify-center gap-4 text-center text-gray-600">
          <p>{info}</p>
          {dialog}
        </div>
      </div>
    </div>
  );
};

export default BoundaryNoType;
