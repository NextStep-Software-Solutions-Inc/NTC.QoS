"use client";

import React, { createContext, useContext, useEffect, useState } from "react";

type DebounceContextType = {
  search: string;
  setSearch: (value: string) => void;
  queryDebounce: string;
  setQueryDebounce: (value: string) => void;
  providerName?: string;
  setProviderName?: (value: string) => void;
  genericSearch?: string;
  setGenericSearch?: (value: string) => void;
  debounceValue?: string;
  setDebounceValue?: (value: string) => void;
};

const DebounceContext = createContext<DebounceContextType | undefined>(undefined);

export const DebounceProvider = ({ children }: { children: React.ReactNode }) => {
  const [search, setSearch] = useState<string>("");
  const [queryDebounce, setQueryDebounce] = useState<string>("");
  const [providerName, setProviderName] = useState<string | undefined>(undefined);
  const [genericSearch, setGenericSearch] = useState<string>("");
  const [debounceValue, setDebounceValue] = useState<string>("");

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebounceValue?.(genericSearch ?? "");
    }, 400);

    return () => {
      clearTimeout(handler);
    };
  }, [genericSearch, setDebounceValue]);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebounceValue?.(genericSearch ?? "");
    }, 400);

    return () => {
      clearTimeout(handler);
    };
  }, [genericSearch, setDebounceValue]);

  return (
    <DebounceContext.Provider
      value={{
        search,
        setSearch,
        queryDebounce,
        setQueryDebounce,
        providerName,
        setProviderName,
        genericSearch,
        setGenericSearch,
        debounceValue,
        setDebounceValue,
      }}>
      {children}
    </DebounceContext.Provider>
  );
};

export const useDebounceContext = (): DebounceContextType => {
  const context = useContext(DebounceContext);
  if (!context) {
    throw new Error("useDebounceContext must be used within a DebounceProvider");
  }
  return context;
};
