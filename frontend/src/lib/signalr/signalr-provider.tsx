"use client";

import { createContext, useContext, useEffect, useRef, useState, ReactNode } from "react";
import {
  HubConnectionBuilder,
  HubConnection,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useAuth } from "@/lib/auth/use-auth";
import { InteractionStatus } from "@azure/msal-browser";
import { useQueryClient } from "@tanstack/react-query";

interface SignalRContextType {
  kanbanConnection: HubConnection | null;
  notificationConnection: HubConnection | null;
  connectionState: HubConnectionState;
}

const SignalRContext = createContext<SignalRContextType>({
  kanbanConnection: null,
  notificationConnection: null,
  connectionState: HubConnectionState.Disconnected,
});

export function SignalRProvider({ children }: { children: ReactNode }) {
  const { getAccessToken, isAuthenticated, inProgress } = useAuth();
  const queryClient = useQueryClient();
  const [kanbanConnection, setKanbanConnection] = useState<HubConnection | null>(null);
  const [notificationConnection, setNotificationConnection] = useState<HubConnection | null>(null);
  const [connectionState, setConnectionState] = useState(HubConnectionState.Disconnected);

  // Stable ref — getAccessToken is already stable from useAuth, but ref ensures
  // the useEffect below never re-runs due to reference changes
  const getAccessTokenRef = useRef(getAccessToken);
  useEffect(() => {
    getAccessTokenRef.current = getAccessToken;
  }, [getAccessToken]);

  const disposedRef = useRef(false);

  useEffect(() => {
    if (!isAuthenticated || inProgress !== InteractionStatus.None) return;

    disposedRef.current = false;

    const baseUrl = process.env.NEXT_PUBLIC_SIGNALR_URL || "http://localhost:5249/hubs";

    const buildConnection = (hub: string) =>
      new HubConnectionBuilder()
        .withUrl(`${baseUrl}/${hub}`, {
          accessTokenFactory: async () => (await getAccessTokenRef.current()) || "",
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(LogLevel.Warning)
        .build();

    const kanban = buildConnection("kanban");
    const notifications = buildConnection("notifications");

    // Derive aggregate state from both connections
    const updateState = () => {
      if (disposedRef.current) return;
      const k = kanban.state;
      const n = notifications.state;
      if (k === HubConnectionState.Reconnecting || n === HubConnectionState.Reconnecting) {
        setConnectionState(HubConnectionState.Reconnecting);
      } else if (k === HubConnectionState.Connected && n === HubConnectionState.Connected) {
        setConnectionState(HubConnectionState.Connected);
      } else {
        setConnectionState(HubConnectionState.Disconnected);
      }
    };

    // Kanban events -> invalidate queries (scoped by projectId from server)
    kanban.on("TicketMoved", (projectId: string) => {
      queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
      queryClient.invalidateQueries({ queryKey: ["tickets", projectId] });
    });
    kanban.on("TicketCreated", (projectId: string) => {
      queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
      queryClient.invalidateQueries({ queryKey: ["tickets", projectId] });
    });
    kanban.on("TicketUpdated", (projectId: string) => {
      queryClient.invalidateQueries({ queryKey: ["tickets", projectId] });
      queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
    });
    kanban.on("CommentAdded", (projectId: string, ticketId: string) =>
      queryClient.invalidateQueries({
        queryKey: ["comments", projectId, ticketId],
      })
    );

    // Notification events
    notifications.on("NotificationReceived", () =>
      queryClient.invalidateQueries({ queryKey: ["notifications"] })
    );

    // Wire reconnect/close events on BOTH connections
    for (const conn of [kanban, notifications]) {
      conn.onreconnecting(() => updateState());
      conn.onreconnected(() => updateState());
      conn.onclose(() => updateState());
    }

    // Start each connection independently (no Promise.all)
    const startConnection = async (conn: HubConnection, name: string) => {
      try {
        await conn.start();
        updateState();
      } catch (err: unknown) {
        if (!disposedRef.current) {
          const msg = err instanceof Error ? err.message : String(err);
          console.warn(`SignalR ${name} connection failed:`, msg);
          updateState();
        }
      }
    };
    startConnection(kanban, "kanban");
    startConnection(notifications, "notifications");

    setKanbanConnection(kanban);
    setNotificationConnection(notifications);

    return () => {
      disposedRef.current = true;
      kanban.stop();
      notifications.stop();
    };
  }, [isAuthenticated, inProgress, queryClient]);

  return (
    <SignalRContext.Provider value={{ kanbanConnection, notificationConnection, connectionState }}>
      {children}
    </SignalRContext.Provider>
  );
}

export const useSignalR = () => useContext(SignalRContext);
