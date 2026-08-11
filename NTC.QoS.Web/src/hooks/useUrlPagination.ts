import { useCallback } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

interface PaginationParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

interface UsePaginationResult {
  page: number;
  pageSize: number;
  search: string;
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
  setSearch: (search: string) => void;
  updateParams: (params: Partial<PaginationParams>) => void;
}

export function useUrlPagination(defaultPageSize = 10): UsePaginationResult {
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const router = useRouter();

  const page = parseInt(searchParams.get("page") || "1", 10);
  const pageSize = parseInt(searchParams.get("pageSize") || String(defaultPageSize), 10);
  const search = searchParams.get("search") || "";

  const updateParams = useCallback(
    (params: Partial<PaginationParams>) => {
      // Read searchParams fresh on each call to avoid stale closures
      const current = new URLSearchParams(window.location.search);
      const currentSearch = current.get("search") || "";

      if (params.page !== undefined) {
        current.set("page", String(params.page));
      }
      if (params.pageSize !== undefined) {
        current.set("pageSize", String(params.pageSize));
        // Reset to page 1 when changing page size
        current.set("page", "1");
      }
      if (params.search !== undefined) {
        const newSearch = params.search || "";
        if (newSearch) {
          current.set("search", newSearch);
        } else {
          current.delete("search");
        }
        // Only reset to page 1 if the search value actually changed
        if (newSearch !== currentSearch) {
          current.set("page", "1");
        }
      }

      const search = current.toString();
      const query = search ? `?${search}` : "";
      router.push(`${pathname}${query}`);
    },
    [pathname, router],
  );

  const setPage = useCallback(
    (newPage: number) => {
      updateParams({ page: newPage });
    },
    [updateParams],
  );

  const setPageSize = useCallback(
    (newPageSize: number) => {
      updateParams({ pageSize: newPageSize });
    },
    [updateParams],
  );

  const setSearch = useCallback(
    (newSearch: string) => {
      updateParams({ search: newSearch });
    },
    [updateParams],
  );

  return { page, pageSize, search, setPage, setPageSize, setSearch, updateParams };
}
