import { useQueryClient } from "@tanstack/react-query";

export const useInvalidateQuery = () => {
  const queryClient = useQueryClient();

  return (queryKey: string | readonly string[]) => {
    // Placeholder function to invalidate queries
    // Actual implementation would depend on the query library being used
    void queryClient.invalidateQueries({
      predicate: (query) => {
        if (typeof queryKey === "object" && Array.isArray(queryKey)) {
          return queryKey.every((key, index) => query.queryKey[index] === key);
        }
        if (typeof queryKey === "string") {
          return typeof query.queryKey[0] === "string" && query.queryKey[0].startsWith(queryKey);
        }
        return false;
      },
    });
  };
};
