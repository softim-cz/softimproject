import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";

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
    queryKey: ["attachments", projectId, ticketId],
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
    onSuccess: (_, { projectId, ticketId }) => {
      queryClient.invalidateQueries({
        queryKey: ["attachments", projectId, ticketId],
      });
      queryClient.invalidateQueries({
        queryKey: ["tickets", projectId, ticketId],
      });
    },
  });
}
