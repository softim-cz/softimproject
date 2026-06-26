import axios from "axios";
import { isDevAuthMode, getDevUserId } from "@/lib/auth/dev-mode";

const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || "http://localhost:5249",
  headers: { "Content-Type": "application/json" },
});

// Token will be set by the auth interceptor
let getTokenFn: (() => Promise<string | null>) | null = null;

export function setTokenProvider(fn: () => Promise<string | null>) {
  getTokenFn = fn;
}

// Wired from the app layout (MSAL mode only). Called when a request is rejected
// with 401 and the token can't be refreshed silently — triggers an interactive
// loginRedirect that actually re-establishes the session. Without this we'd hard
// reload to /login, which bounces straight back to the dashboard while a stale
// account lingers in the MSAL cache, producing an endless reload loop.
let onAuthFailure: (() => void) | null = null;

export function setAuthFailureHandler(fn: (() => void) | null) {
  onAuthFailure = fn;
}

// Guard against a tight interactive-login loop when re-auth keeps failing (e.g.
// misconfigured Entra). If we already tried recently, fall back to the static
// login screen instead of bouncing to Microsoft again.
const REAUTH_COOLDOWN_MS = 30_000;
function shouldAttemptReauth(): boolean {
  try {
    const last = Number(window.sessionStorage.getItem("softim-last-reauth") || 0);
    if (Date.now() - last < REAUTH_COOLDOWN_MS) return false;
    window.sessionStorage.setItem("softim-last-reauth", String(Date.now()));
    return true;
  } catch {
    return true;
  }
}

apiClient.interceptors.request.use(async (config) => {
  if (isDevAuthMode) {
    config.headers["X-Dev-User-Id"] = getDevUserId();
    return config;
  }
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

    // Silent refresh failed (or no token provider). Force an interactive
    // re-login that re-establishes the session, instead of a hard reload to
    // /login that would just loop back to the dashboard with a stale account.
    if (error.response?.status === 401 && typeof window !== "undefined" && !isRedirecting) {
      isRedirecting = true;
      if (onAuthFailure && shouldAttemptReauth()) {
        onAuthFailure();
      } else {
        window.location.href = "/login";
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
