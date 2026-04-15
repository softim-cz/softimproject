"use client";

import { MsalProvider } from "@azure/msal-react";
import {  PublicClientApplication,
  type AuthenticationResult,
} from "@azure/msal-browser";
import { msalConfig } from "./msal-config";
import { ReactNode, useEffect, useRef, useState } from "react";

const msalInstance = new PublicClientApplication(msalConfig);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isInitialized, setIsInitialized] = useState(false);
  const initializing = useRef(false);

  useEffect(() => {
    if (initializing.current) return;
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

  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}


