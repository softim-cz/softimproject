import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import { queryKeys } from "./query-keys";

export interface TicketsByStateItem {
  stateId: string;
  stateName: string;
  stateColor: string;
  sortOrder: number;
  count: number;
}

export interface MyOpenTicketItem {
  id: string;
  number: number;
  key: string;
  title: string;
  projectId: string;
  projectName: string;
  projectCode: string;
  taskStateName: string;
  taskStateColor: string;
  ticketPriorityName: string;
  ticketPriorityColor: string;
  dueDate: string | null;
}

export interface DashboardStats {
  ticketsByState: TicketsByStateItem[];
  myOpenTickets: MyOpenTicketItem[];
}

export function useDashboardStats() {
  return useQuery({
    queryKey: queryKeys.stats.dashboard(),
    queryFn: async () => {
      const { data } = await apiClient.get<DashboardStats>("/api/v1/stats/dashboard");
      return data;
    },
  });
}
