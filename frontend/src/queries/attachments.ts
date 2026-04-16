import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import { queryKeys } from "./query-keys";

export interface AttachmentDto {
  id: string;
  ticketId: string;
  fileName: string;
  blobUrl: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedByName: string;
  createdAt: string;
}

export function useAttachments(projectId: string, ticketId: string) {
  return useQuery({
    queryKey: queryKeys.attachments.ticket(projectId, ticketId),
    queryFn: async () => {
      const { data } = await apiClient.get<AttachmentDto[]>(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/attachments`
      );
      return data;
    },
    enabled: !!projectId && !!ticketId,
  });
}

export function useDeleteAttachment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      ticketId,
      attachmentId,
    }: {
      projectId: string;
      ticketId: string;
      attachmentId: string;
    }) => {
      await apiClient.delete(
        `/api/v1/projects/${projectId}/tickets/${ticketId}/attachments/${attachmentId}`
      );
    },
    onSuccess: async (_, { projectId, ticketId }) => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.attachments.ticket(projectId, ticketId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.tickets.detail(projectId, ticketId),
      });
    },
  });
}
