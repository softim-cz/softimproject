"use client";

import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { loginRequest } from "./msal-config";
import { useCallback } from "react";

export function useAuth() {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const login = useCallback(async () => {
    await instance.loginRedirect(loginRequest);
  }, [instance]);

  const logout = useCallback(async () => {
    await instance.logoutRedirect();
  }, [instance]);

  const getAccessToken = useCallback(async (): Promise<string | null> => {
    if (!accounts[0]) return null;
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0],
      });
      return response.accessToken;
    } catch {
      await instance.acquireTokenRedirect(loginRequest);
      return null;
    }
  }, [instance, accounts]);

  return {
    isAuthenticated,
    user: accounts[0],
    login,
    logout,
    getAccessToken,
  };
}
