import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { Comment } from "@/types";

export function useComments(projectId: string, ticketId: string) {
  return useQuery({
    queryKey: ["comments", projectId, ticketId],
    queryFn: async () => {
      const { data } = await apiClient.get<Comment[]>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/comments`
      );
      return data;
    },
    enabled: !!projectId && !!ticketId,
  });
}

export function useCreateComment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      ticketId,
      content,
      isInternal,
    }: {
      projectId: string;
      ticketId: string;
      content: string;
      isInternal: boolean;
    }) => {
      const { data } = await apiClient.post<Comment>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/comments`,
        { content, isInternal }
      );
      return data;
    },
    onSuccess: (_, { projectId, ticketId }) => {
      queryClient.invalidateQueries({
        queryKey: ["comments", projectId, ticketId],
      });
    },
  });
}
