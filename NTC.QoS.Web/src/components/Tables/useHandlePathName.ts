"use client";

import { usePathname } from "next/navigation";

const useHandlePathName = (name: string) => {
  const pathname = usePathname();
  // Remove trailing slash if present, then append /name
  const normalizedPath = pathname.endsWith("/") && pathname.length > 1 ? pathname.slice(0, -1) : pathname;
  return `${normalizedPath}/${name}`;
};

export default useHandlePathName;
