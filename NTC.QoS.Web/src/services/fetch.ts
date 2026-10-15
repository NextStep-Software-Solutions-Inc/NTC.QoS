import { getBackendUrl } from "@/lib/api-utils";
import { extractErrorMessage } from "@/lib/extract-error-message";

export type RequestConfig = {
  headers?: Record<string, string>;
  baseUrl?: string;
  credentials?: RequestCredentials;
};

export type ApiErrorResponse = {
  message?: string;
  error?: string;
  title?: string;
  detail?: string;
  statusCode?: number;
  status?: number;
  type?: string;
  traceId?: string;
  errors?: Record<string, string[] | string> | string[];
  data?: unknown;
  succeeded?: boolean;
  [key: string]: unknown;
};

export class ApiError extends Error {
  statusCode: number;
  data: ApiErrorResponse;
  response?: {
    data?: ApiErrorResponse;
    status?: number;
    statusText?: string;
    message?: string;
    errors?: Record<string, string[] | string> | string[];
  };

  constructor(message: string, statusCode: number, data: ApiErrorResponse = {}) {
    super(message);
    this.name = "ApiError";
    this.statusCode = statusCode;
    this.data = data;
    this.response = {
      data,
      status: statusCode,
      message: (typeof data.message === "string" && data.message) || message,
      errors: data.errors,
    };
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
  const isJson = contentType.includes("json");

  if (!response.ok) {
    let errorData: ApiErrorResponse = {};
    let rawText = "";

    try {
      if (isJson) {
        errorData = (await response.json()) as ApiErrorResponse;
      } else {
        rawText = await response.text();
        const trimmed = rawText.trim();
        if (
          (trimmed.startsWith("{") && trimmed.endsWith("}")) ||
          (trimmed.startsWith("[") && trimmed.endsWith("]"))
        ) {
          try {
            errorData = JSON.parse(trimmed) as ApiErrorResponse;
          } catch {
            // Non-JSON text
          }
        }
      }
    } catch {
      // Failed to parse error response
    }

    const errorMessage =
      extractErrorMessage(Object.keys(errorData).length > 0 ? errorData : rawText, "") ||
      `HTTP error! status: ${response.status}`;

    throw new ApiError(errorMessage, response.status, errorData);
  }

  if (response.status === 204) {
    return {} as T;
  }

  if (isJson) {
    return response.json();
  }

  return response.text() as unknown as T;
}

const isFormData = (val: unknown): val is FormData => {
  return typeof FormData !== "undefined" && val instanceof FormData;
};

export function createRequest(baseUrl: string, defaultConfig: RequestConfig = {}) {
  const getFullUrl = (url: string): string => {
    if (url.startsWith("http://") || url.startsWith("https://")) {
      return url;
    }
    const cleanBaseUrl = baseUrl.endsWith("/") ? baseUrl.slice(0, -1) : baseUrl;
    const cleanUrl = url.startsWith("/") ? url : `/${url}`;
    return `${cleanBaseUrl}${cleanUrl}`;
  };

  const getHeaders = async (headers?: Record<string, string>): Promise<Record<string, string>> => {
    let bearerToken: string | null = null;
    if (
      typeof window !== "undefined" &&
      (window as unknown as { Clerk?: { session?: { getToken: () => Promise<string | null> } } }).Clerk?.session
    ) {
      try {
        bearerToken = await (
          window as unknown as { Clerk?: { session?: { getToken: () => Promise<string | null> } } }
        ).Clerk!.session!.getToken();
      } catch {
        // Ignore token fetch error
      }
    }

    return {
      "Content-Type": "application/json",
      ...(bearerToken ? { Authorization: `Bearer ${bearerToken}` } : {}),
      ...defaultConfig.headers,
      ...headers,
    };
  };

  const requestMethod = async <T = unknown>(
    url: string,
    method: string,
    payload?: unknown,
    init?: RequestInit
  ): Promise<T> => {
    const fullUrl = getFullUrl(url);
    const headers = await getHeaders(init?.headers as Record<string, string>);

    const hasFormData = isFormData(payload) || isFormData(init?.body);
    if (hasFormData) {
      delete headers["Content-Type"];
    }

    const config: RequestInit = {
      method,
      headers,
      credentials: defaultConfig.credentials ?? init?.credentials,
      ...init,
    };

    if (payload !== undefined && payload !== null) {
      if (isFormData(payload)) {
        config.body = payload;
      } else if (typeof payload === "string" || payload instanceof Blob || payload instanceof URLSearchParams) {
        config.body = payload;
      } else {
        config.body = JSON.stringify(payload);
      }
    }

    const response = await fetch(fullUrl, config);
    return handleResponse<T>(response);
  };

  return {
    get: <T = unknown>(url: string, init?: RequestInit) => requestMethod<T>(url, "GET", undefined, init),
    post: <T = unknown>(url: string, payload?: unknown, init?: RequestInit) =>
      requestMethod<T>(url, "POST", payload, init),
    put: <T = unknown>(url: string, payload?: unknown, init?: RequestInit) =>
      requestMethod<T>(url, "PUT", payload, init),
    patch: <T = unknown>(url: string, payload?: unknown, init?: RequestInit) =>
      requestMethod<T>(url, "PATCH", payload, init),
    delete: <T = unknown>(url: string, init?: RequestInit) => requestMethod<T>(url, "DELETE", undefined, init),
  };
}

export const request = createRequest(getBackendUrl(), { headers: { Accept: "application/json" } });

