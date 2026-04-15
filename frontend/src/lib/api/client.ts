import axios from "axios";

const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || "http://localhost:5249",
  headers: { "Content-Type": "application/json" },
});

// Token will be set by the auth interceptor
let getTokenFn: (() => Promise<string | null>) | null = null;

export function setTokenProvider(fn: () => Promise<string | null>) {
  getTokenFn = fn;
}

apiClient.interceptors.request.use(async (config) => {
  if (getTokenFn) {
    const token = await getTokenFn();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }
  return config;
});

// Prevent multiple parallel redirects to /login
let isRedirecting = false;

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // On 401, try to refresh token and retry once
    if (
      error.response?.status === 401 &&
      !originalRequest._retry &&
      getTokenFn &&
      typeof window !== "undefined"
    ) {
      originalRequest._retry = true;
      const token = await getTokenFn();
      if (token) {
        originalRequest.headers.Authorization = `Bearer ${token}`;
        return apiClient(originalRequest);
      }
    }

    // If retry failed or no token provider, redirect to login (once)
    if (
      error.response?.status === 401 &&
      typeof window !== "undefined" &&
      !isRedirecting
    ) {
      isRedirecting = true;
      window.location.href = "/login";
    }

    return Promise.reject(error);
  }
);

export default apiClient;
