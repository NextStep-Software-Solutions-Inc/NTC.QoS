const getSessionFromCookie = () => {
  let token: string | null = null;
  if (typeof document !== "undefined") {
    token =
      document.cookie
        .split(";")
        .find((c) => c.trim().startsWith("__session"))
        ?.split("=")[1] ?? null;
  }
  return token;
};

const createRequest = (baseUrl: string, baseInit?: RequestInit) => {
  const typeSafeFetch = async <R>(url: string, init?: RequestInit): Promise<R> => {
    const res = await fetch(`${baseUrl}${url}`, init);

    // Handle 204 No Content - return void/undefined
    if (res.status === 204) {
      return undefined as R;
    }

    const contentType = res.headers.get("content-type");

    let response: Promise<R>;

    // For error responses, be more lenient with content-type parsing
    if (!res.ok) {
      // Read response as text first, then try to parse as JSON
      response = res.text().then((text) => {
        try {
          return JSON.parse(text) as R;
        } catch {
          return text as R;
        }
      }) as Promise<R>;
    } else {
      // Handle 204 No Content responses (common for DELETE and PUT operations)
      if (res.status === 204 || !contentType) {
        response = Promise.resolve(undefined as R);
      } else {
        // For successful responses with content, enforce strict content-type checking
        switch (contentType) {
          case "application/json":
          case "application/json; charset=utf-8":
          case "application/problem+json":
          case "application/problem+json; charset=utf-8":
            response = res.json() as Promise<R>;
            break;

          case "application/xml":
          case "text/plain":
            response = res.text() as Promise<R>;
            break;

          case "application/octet-stream":
          case "image/png":
          case "application/pdf":
            response = res.blob() as Promise<R>;
            break;

          case "application/x-www-form-urlencoded":
            response = res.formData() as Promise<R>;
            break;

          default:
            throw new Error(`Unsupported content type ${contentType}`);
        }
      }
    }

    if (!res.ok) {
      const errorResponse = await response;
      const error = new Error(`HTTP ${res.status}: ${res.statusText}`) as Error & { status: number; response: R };
      error.status = res.status;
      error.response = errorResponse;
      throw error;
    }
    return response;
  };

  // Refactored: Unified payload/init handling, improved header/content-type logic, removed console logs.
  const createRequestMethod = <R = unknown>({
    method,
    hasPayload = false,
  }: {
    method: string;
    hasPayload?: boolean;
  }) => {
    return (url: string, payloadOrInit?: unknown, init?: RequestInit): Promise<R> => {
      const token = getSessionFromCookie();

      // Determine payload and init
      let payload: unknown;
      let requestInit: RequestInit | undefined;

      if (hasPayload) {
        payload = payloadOrInit;
        requestInit = init;
      } else {
        payload = undefined;
        requestInit = payloadOrInit as RequestInit | undefined;
      }
      const isJsonPayload =
        hasPayload &&
        payload &&
        !(payload instanceof FormData || payload instanceof Blob || payload instanceof ArrayBuffer);
      // Merge headers and set Content-Type if payload is present

      const headers: HeadersInit = {
        ...(baseInit?.headers ?? {}),
        ...(requestInit?.headers ?? {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(isJsonPayload ? { "Content-Type": "application/json" } : {}),
      };

      // Prepare body based on payload type
      let body: BodyInit | undefined;
      if (hasPayload && payload) {
        if (payload instanceof FormData || payload instanceof Blob || payload instanceof ArrayBuffer) {
          body = payload as BodyInit;
        } else {
          body = JSON.stringify(payload);
        }
      }

      return typeSafeFetch<R>(url, {
        ...baseInit,
        ...requestInit,
        method,
        ...(body ? { body } : {}),
        headers,
      });
    };
  };

  return {
    get: <R = unknown>(url: string, init?: RequestInit) => createRequestMethod<R>({ method: "GET" })(url, init),
    post: <R = unknown>(url: string, payload?: unknown, init?: RequestInit) =>
      createRequestMethod<R>({ method: "POST", hasPayload: true })(url, payload, init),
    put: <R = unknown>(url: string, payload?: unknown, init?: RequestInit) =>
      createRequestMethod<R>({ method: "PUT", hasPayload: true })(url, payload, init),
    patch: <R = unknown>(url: string, payload?: unknown, init?: RequestInit) =>
      createRequestMethod<R>({ method: "PATCH", hasPayload: true })(url, payload, init),
    delete: <R = unknown>(url: string, init?: RequestInit) => createRequestMethod<R>({ method: "DELETE" })(url, init),
  };
};

const QOS_BACKEND_URL =
  process.env.QOS_BACKEND_URL ?? process.env.NEXT_PUBLIC_QOS_BACKEND_URL ?? "http://localhost:3000";

export const request = createRequest(QOS_BACKEND_URL, { headers: { Accept: "application/json" } });
