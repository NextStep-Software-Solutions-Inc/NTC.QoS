"use client";

import { createContext, useContext, useState } from "react";

interface TableContextType {
  isCellSiteDetails: boolean;
  setIsCellSiteDetails: React.Dispatch<React.SetStateAction<boolean>>;
}

const TableContext = createContext<TableContextType | undefined>(undefined);

export const TableProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isCellSiteDetails, setIsCellSiteDetails] = useState<boolean>(false);

  return <TableContext.Provider value={{ isCellSiteDetails, setIsCellSiteDetails }}>{children}</TableContext.Provider>;
};

export const useTable = () => {
  const context = useContext(TableContext);
  if (!context) {
    throw new Error("useTable must be used within a TableProvider");
  }
  return context;
};
