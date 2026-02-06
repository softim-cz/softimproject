import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { KanbanBoard } from "@/types";

export function useBoard(projectId: string) {
  return useQuery({
    queryKey: ["kanban", projectId],
    queryFn: async () => {
      const { data } = await apiClient.get<KanbanBoard>(
        `/api/v1/projects/${projectId}/board`
      );
      return data;
    },
    enabled: !!projectId,
  });
}

export function useMoveTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      ticketId,
      targetColumnId,
      position,
    }: {
      projectId: string;
      ticketId: string;
      targetColumnId: string;
      position: number;
    }) => {
      await apiClient.put(
        `/api/v1/projects/${projectId}/board/tickets/${ticketId}/move`,
        { targetColumnId, position }
      );
    },
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["kanban"] }),
  });
}
