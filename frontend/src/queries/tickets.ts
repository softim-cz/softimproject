import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { Ticket } from "@/types";

export function useTickets(projectId: string) {
  return useQuery({
    queryKey: ["tickets", projectId],
    queryFn: async () => {
      const { data } = await apiClient.get<Ticket[]>(
        `/api/v1/projects/${projectId}/tickets`
      );
      return data;
    },
    enabled: !!projectId,
  });
}

export function useTicket(projectId: string, ticketId: string) {
  return useQuery({
    queryKey: ["tickets", projectId, ticketId],
    queryFn: async () => {
      const { data } = await apiClient.get<Ticket>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}`
      );
      return data;
    },
    enabled: !!projectId && !!ticketId,
  });
}

export function useCreateTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      ...ticket
    }: Partial<Ticket> & { projectId: string }) => {
      const { data } = await apiClient.post<Ticket>(
        `/api/v1/projects/${projectId}/tickets`,
        ticket
      );
      return data;
    },
    onSuccess: (_, { projectId }) => {
      queryClient.invalidateQueries({ queryKey: ["tickets", projectId] });
      queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
    },
  });
}

export function useUpdateTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      id,
      ...ticket
    }: Partial<Ticket> & { projectId: string; id: string }) => {
      const { data } = await apiClient.put<Ticket>(
        `/api/v1/projects/${projectId}/tickets/${id}`,
        ticket
      );
      return data;
    },
    onSuccess: (_, { projectId, id }) => {
      queryClient.invalidateQueries({ queryKey: ["tickets", projectId] });
      queryClient.invalidateQueries({
        queryKey: ["tickets", projectId, id],
      });
      queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
    },
  });
}
