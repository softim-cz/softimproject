// Central switch for the dev-auth bypass. When NEXT_PUBLIC_DEV_AUTH=true,
// the frontend skips MSAL and the API client sends X-Dev-User-Id matching
// a seeded user so the backend's DevAuthenticationHandler picks it up.
// Must stay false in production builds.

export const isDevAuthMode = process.env.NEXT_PUBLIC_DEV_AUTH === "true";

const DEV_USER_STORAGE_KEY = "softim-dev-user-id";

export function getDevUserId(): string {
  if (typeof window !== "undefined") {
    const stored = window.localStorage.getItem(DEV_USER_STORAGE_KEY);
    if (stored) return stored;
  }
  return process.env.NEXT_PUBLIC_DEV_USER_ID || "dev:admin";
}

export function setDevUserId(id: string): void {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(DEV_USER_STORAGE_KEY, id);
  }
}
