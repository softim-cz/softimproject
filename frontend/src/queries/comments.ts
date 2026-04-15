import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { Comment } from "@/types";
import { queryKeys } from "./query-keys";

export function useComments(projectId: string, ticketId: string) {
  return useQuery({
    queryKey: queryKeys.comments.ticket(projectId, ticketId),
    queryFn: async () => {
      const { data } = await apiClient.get<Comment[]>(`/api/v1/projects/${projectId}/tickets/${ticketId}/comments`);
      return data;
    },
    enabled: !!projectId && !!ticketId,
  });
}

export function useProjectComments(projectId: string) {
  return useQuery({
    queryKey: queryKeys.comments.project(projectId),
    queryFn: async () => {
      const { data } = await apiClient.get<Comment[]>(`/api/v1/projects/${projectId}/comments`);
      return data;
    },
    enabled: !!projectId,
  });
}

export function useCreateProjectComment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ projectId, content, isInternal }: { projectId: string; content: string; isInternal: boolean }) => {
      const { data } = await apiClient.post<Comment>(`/api/v1/projects/${projectId}/comments`, { projectId, content, isInternal });
      return data;
    },
    onSuccess: (_, { projectId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.comments.project(projectId) });
    },
  });
}

export function useCreateComment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ projectId, ticketId, content, isInternal }: { projectId: string; ticketId: string; content: string; isInternal: boolean }) => {
      const { data } = await apiClient.post<Comment>(`/api/v1/projects/${projectId}/tickets/${ticketId}/comments`, { content, isInternal });
      return data;
    },
    onSuccess: (_, { projectId, ticketId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.comments.ticket(projectId, ticketId) });
    },
  });
}
