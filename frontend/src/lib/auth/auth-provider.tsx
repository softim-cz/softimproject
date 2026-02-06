"use client";

import { MsalProvider } from "@azure/msal-react";
import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig } from "./msal-config";
import { ReactNode, useMemo } from "react";

export function AuthProvider({ children }: { children: ReactNode }) {
  const msalInstance = useMemo(
    () => new PublicClientApplication(msalConfig),
    []
  );
  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
