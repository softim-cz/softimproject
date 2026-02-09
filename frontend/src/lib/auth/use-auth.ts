"use client";

import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { loginRequest } from "./msal-config";
import { useCallback } from "react";

export function useAuth() {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const login = useCallback(async () => {
    if (inProgress !== InteractionStatus.None) return;
    await instance.loginRedirect(loginRequest);
  }, [instance, inProgress]);

  const logout = useCallback(async () => {
    if (inProgress !== InteractionStatus.None) return;
    await instance.logoutRedirect();
  }, [instance, inProgress]);

  const getAccessToken = useCallback(async (): Promise<string | null> => {
    if (!accounts[0]) return null;
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account: accounts[0],
      });
      return response.accessToken;
    } catch {
      if (inProgress === InteractionStatus.None) {
        await instance.acquireTokenRedirect(loginRequest);
      }
      return null;
    }
  }, [instance, accounts, inProgress]);

  return {
    isAuthenticated,
    inProgress,
    user: accounts[0],
    login,
    logout,
    getAccessToken,
  };
}
