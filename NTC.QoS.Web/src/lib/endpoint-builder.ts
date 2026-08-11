import { type ExtractPathParams } from "@/types/reusable/api";

export function buildEndpoint<T extends string, TQueryParams>(
  endpoint: T,
  pathParams: ExtractPathParams<T>,
  queryParams: TQueryParams,
): string {
  const urlWithParams = Object.entries(pathParams).reduce<string>(
    (url, [key, value]) => url.replace(`:${key}`, encodeURIComponent(value)),
    endpoint,
  );

  if (queryParams && Object.keys(queryParams).length > 0) {
    const searchParams = new URLSearchParams();
    Object.entries(queryParams).forEach(([key, value]) => {
      if (value === null || value === undefined) return;
      if (Array.isArray(value)) {
        value.forEach((v) => searchParams.append(key, `${v}`));
      } else if (typeof value === "object") {
        searchParams.set(key, JSON.stringify(value));
      } else if (typeof value === "boolean" || typeof value === "number") {
        searchParams.set(key, `${value}`);
      } else if (typeof value === "string") {
        searchParams.set(key, value);
      }
    });
    return `${urlWithParams}?${searchParams.toString()}`;
  }

  return urlWithParams;
}
