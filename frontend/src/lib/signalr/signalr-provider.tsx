"use client";

import {
  createContext,
  useContext,
  useEffect,
  useState,
  ReactNode,
} from "react";
import {
  HubConnectionBuilder,
  HubConnection,
  HubConnectionState,
} from "@microsoft/signalr";
import { useAuth } from "@/lib/auth/use-auth";
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
  const { getAccessToken, isAuthenticated } = useAuth();
  const queryClient = useQueryClient();
  const [kanbanConnection, setKanbanConnection] =
    useState<HubConnection | null>(null);
  const [notificationConnection, setNotificationConnection] =
    useState<HubConnection | null>(null);
  const [connectionState, setConnectionState] = useState(
    HubConnectionState.Disconnected
  );

  useEffect(() => {
    if (!isAuthenticated) return;

    const baseUrl =
      process.env.NEXT_PUBLIC_SIGNALR_URL || "https://localhost:7001/hubs";

    const buildConnection = (hub: string) =>
      new HubConnectionBuilder()
        .withUrl(`${baseUrl}/${hub}`, {
          accessTokenFactory: async () => (await getAccessToken()) || "",
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .build();

    const kanban = buildConnection("kanban");
    const notifications = buildConnection("notifications");

    // Kanban events -> invalidate queries
    kanban.on("TicketMoved", () =>
      queryClient.invalidateQueries({ queryKey: ["kanban"] })
    );
    kanban.on("TicketCreated", () =>
      queryClient.invalidateQueries({ queryKey: ["kanban"] })
    );
    kanban.on("TicketUpdated", () =>
      queryClient.invalidateQueries({ queryKey: ["tickets"] })
    );
    kanban.on("CommentAdded", () =>
      queryClient.invalidateQueries({ queryKey: ["comments"] })
    );

    // Notification events
    notifications.on("NotificationReceived", () =>
      queryClient.invalidateQueries({ queryKey: ["notifications"] })
    );

    kanban.onreconnecting(() =>
      setConnectionState(HubConnectionState.Reconnecting)
    );
    kanban.onreconnected(() =>
      setConnectionState(HubConnectionState.Connected)
    );
    kanban.onclose(() => setConnectionState(HubConnectionState.Disconnected));

    Promise.all([kanban.start(), notifications.start()])
      .then(() => setConnectionState(HubConnectionState.Connected))
      .catch(console.error);

    setKanbanConnection(kanban);
    setNotificationConnection(notifications);

    return () => {
      kanban.stop();
      notifications.stop();
    };
  }, [isAuthenticated, getAccessToken, queryClient]);

  return (
    <SignalRContext.Provider
      value={{ kanbanConnection, notificationConnection, connectionState }}
    >
      {children}
    </SignalRContext.Provider>
  );
}

export const useSignalR = () => useContext(SignalRContext);
