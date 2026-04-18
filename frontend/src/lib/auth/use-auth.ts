"use client";

import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { loginRequest } from "./msal-config";
import { isDevAuthMode, getDevUserId } from "./dev-mode";
import { useCallback, useRef, useEffect } from "react";

function useAuthDev() {
  const noop = useCallback(async () => {}, []);
  const getAccessToken = useCallback(async (): Promise<string | null> => null, []);
  return {
    isAuthenticated: true,
    inProgress: InteractionStatus.None,
    user: { username: getDevUserId(), name: getDevUserId() },
    login: noop,
    logout: noop,
    getAccessToken,
  };
}

function useAuthMsal() {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  // Keep refs so getAccessToken callback identity stays stable
  const instanceRef = useRef(instance);
  const accountsRef = useRef(accounts);
  useEffect(() => {
    instanceRef.current = instance;
    accountsRef.current = accounts;
  }, [instance, accounts]);

  const login = useCallback(async () => {
    if (inProgress !== InteractionStatus.None) return;
    await instance.loginRedirect(loginRequest);
  }, [instance, inProgress]);

  const logout = useCallback(async () => {
    if (inProgress !== InteractionStatus.None) return;
    await instance.logoutRedirect();
  }, [instance, inProgress]);

  // Stable reference — never triggers redirect, returns null on failure
  const getAccessToken = useCallback(async (): Promise<string | null> => {
    const account = accountsRef.current[0];
    if (!account) return null;
    try {
      const response = await instanceRef.current.acquireTokenSilent({
        ...loginRequest,
        account,
      });
      return response.accessToken;
    } catch {
      return null;
    }
  }, []);

  return {
    isAuthenticated,
    inProgress,
    user: accounts[0],
    login,
    logout,
    getAccessToken,
  };
}

// Pinned at module load; NEXT_PUBLIC_DEV_AUTH is inlined at build time so
// this never flips between calls and satisfies React's hook rules.
export const useAuth = isDevAuthMode ? useAuthDev : useAuthMsal;
