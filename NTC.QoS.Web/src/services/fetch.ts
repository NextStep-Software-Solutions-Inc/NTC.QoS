import { getBackendUrl } from "@/lib/api-utils";

export type RequestConfig = {
  headers?: Record<string, string>;
  baseUrl?: string;
  credentials?: RequestCredentials;
};

export type ApiErrorResponse = {
  message?: string;
  error?: string;
  statusCode?: number;
};

export class ApiError extends Error {
  statusCode: number;
  data: ApiErrorResponse;

  constructor(message: string, statusCode: number, data: ApiErrorResponse = {}) {
    super(message);
    this.name = "ApiError";
    this.statusCode = statusCode;
    this.data = data;
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  const contentType = response.headers.get("content-type");
  const isJson = contentType?.includes("application/json");

  if (!response.ok) {
    let errorData: ApiErrorResponse = {};
    let errorMessage = `HTTP error! status: ${response.status}`;

    try {
      if (isJson) {
        errorData = await response.json();
        errorMessage = errorData.message || errorData.error || errorMessage;
      } else {
        const text = await response.text();
        if (text) errorMessage = text;
      }
    } catch {
      // Failed to parse error response
    }

    throw new ApiError(errorMessage, response.status, errorData);
  }

  if (response.status === 24) {
    return {} as T;
  }

  if (isJson) {
    return response.json();
  }

  return response.text() as unknown as T;
}

export function createRequest(baseUrl: string, defaultConfig: RequestConfig = {}) {
  const getFullUrl = (url: string): string => {
    if (url.startsWith("http://") || url.startsWith("https://")) {
      return url;
    }
    const cleanBaseUrl = baseUrl.endsWith("/") ? baseUrl.slice(0, -1) : baseUrl;
    const cleanUrl = url.startsWith("/") ? url : `/${url}`;
    return `${cleanBaseUrl}${cleanUrl}`;
  };

  const getHeaders = (headers?: Record<string, string>): Record<string, string> => {
    return {
      "Content-Type": "application/json",
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

    const config: RequestInit = {
      method,
      headers: getHeaders(init?.headers as Record<string, string>),
      credentials: defaultConfig.credentials ?? init?.credentials,
      ...init,
    };

    if (payload !== undefined && payload !== null) {
      config.body = JSON.stringify(payload);
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
