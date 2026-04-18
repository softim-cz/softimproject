"use client";

import { MsalProvider } from "@azure/msal-react";
import { PublicClientApplication, type AuthenticationResult } from "@azure/msal-browser";
import { msalConfig } from "./msal-config";
import { isDevAuthMode } from "./dev-mode";
import { ReactNode, useEffect, useRef, useState } from "react";

const msalInstance = isDevAuthMode ? null : new PublicClientApplication(msalConfig);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isInitialized, setIsInitialized] = useState(isDevAuthMode);
  const initializing = useRef(false);

  useEffect(() => {
    if (isDevAuthMode || !msalInstance || initializing.current) return;
    initializing.current = true;

    msalInstance
      .initialize()
      .then(() => msalInstance.handleRedirectPromise())
      .then((response: AuthenticationResult | null) => {
        if (response) {
          msalInstance.setActiveAccount(response.account);
        } else {
          const accounts = msalInstance.getAllAccounts();
          if (accounts.length > 0) {
            msalInstance.setActiveAccount(accounts[0]);
          }
        }
      })
      .catch((error: unknown) => {
        console.error("MSAL initialization failed:", error);
      })
      .finally(() => {
        setIsInitialized(true);
      });
  }, []);

  if (!isInitialized) return null;
  if (isDevAuthMode || !msalInstance) return <>{children}</>;
  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
