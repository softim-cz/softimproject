"use client";

import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { loginRequest } from "./msal-config";
import { useCallback, useRef, useEffect } from "react";

export function useAuth() {
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
