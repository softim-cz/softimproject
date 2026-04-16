import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { KanbanBoard } from "@/types";
import { queryKeys } from "./query-keys";

export function useBoard(projectId: string) {
  return useQuery({
    queryKey: queryKeys.kanban.board(projectId),
    queryFn: async () => {
      const { data } = await apiClient.get<KanbanBoard>(`/api/v1/projects/${projectId}/board`);
      return data;
    },
    enabled: !!projectId,
  });
}

function invalidateBoard(queryClient: ReturnType<typeof useQueryClient>, projectId: string) {
  return queryClient.invalidateQueries({ queryKey: queryKeys.kanban.board(projectId) });
}

export function useCreateColumn() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      projectId: string;
      boardId: string;
      name: string;
      wipLimit?: number;
      mapsToTaskStateIds: string[];
      color?: string;
    }) => {
      const { data } = await apiClient.post<string>(
        `/api/v1/projects/${payload.projectId}/boards/${payload.boardId}/columns`,
        payload
      );
      return data;
    },
    onSuccess: (_, { projectId }) => invalidateBoard(queryClient, projectId),
  });
}

export function useUpdateColumn() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      projectId: string;
      boardId: string;
      columnId: string;
      name: string;
      wipLimit?: number;
      mapsToTaskStateIds: string[];
      color?: string;
    }) => {
      await apiClient.put(
        `/api/v1/projects/${payload.projectId}/boards/${payload.boardId}/columns/${payload.columnId}`,
        payload
      );
    },
    onSuccess: (_, { projectId }) => invalidateBoard(queryClient, projectId),
  });
}

export function useDeleteColumn() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      boardId,
      columnId,
    }: {
      projectId: string;
      boardId: string;
      columnId: string;
    }) => {
      await apiClient.delete(`/api/v1/projects/${projectId}/boards/${boardId}/columns/${columnId}`);
    },
    onSuccess: (_, { projectId }) => invalidateBoard(queryClient, projectId),
  });
}

export function useReorderColumns() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { projectId: string; boardId: string; columnIds: string[] }) => {
      await apiClient.put(
        `/api/v1/projects/${payload.projectId}/boards/${payload.boardId}/columns/reorder`,
        payload
      );
    },
    onSuccess: (_, { projectId }) => invalidateBoard(queryClient, projectId),
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
      await apiClient.put(`/api/v1/projects/${projectId}/tickets/${ticketId}/move`, {
        targetColumnId,
        position,
      });
    },
    onSuccess: (_, { projectId }) => invalidateBoard(queryClient, projectId),
  });
}
