"use client";

import { useAuth } from "@/lib/auth/use-auth";
import { InteractionStatus } from "@azure/msal-browser";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";
import { Footer } from "@/components/layout/footer";
import { useUiStore } from "@/stores/ui-store";
import { setTokenProvider, setAuthFailureHandler } from "@/lib/api/client";
import { isDevAuthMode } from "@/lib/auth/dev-mode";
import { SignalRProvider } from "@/lib/signalr/signalr-provider";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, inProgress, getAccessToken, login } = useAuth();
  const router = useRouter();
  const sidebarCollapsed = useUiStore((s) => s.sidebarCollapsed);

  useEffect(() => {
    setTokenProvider(isAuthenticated ? getAccessToken : async () => null);
  }, [isAuthenticated, getAccessToken]);

  // On an unrecoverable 401 (silent token refresh failed) re-run interactive
  // login instead of looping through /login. Dev-auth never hits 401, so the
  // handler stays unset and the API client keeps its /login fallback.
  useEffect(() => {
    if (isDevAuthMode) return;
    setAuthFailureHandler(() => login());
    return () => setAuthFailureHandler(null);
  }, [login]);

  useEffect(() => {
    if (inProgress !== InteractionStatus.None) return;
    if (!isAuthenticated) router.push("/login");
  }, [isAuthenticated, inProgress, router]);

  if (inProgress !== InteractionStatus.None) return null;
  if (!isAuthenticated) return null;

  return (
    <SignalRProvider>
      <div className="flex h-screen bg-background">
        <Sidebar collapsed={sidebarCollapsed} />
        <div className="flex flex-col flex-1 overflow-hidden">
          <Topbar />
          <main className="flex-1 overflow-y-auto p-6">{children}</main>
          <Footer />
        </div>
      </div>
    </SignalRProvider>
  );
}
