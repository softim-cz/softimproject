import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { IntegrationConnection } from "@/types";

const listKey = ["admin", "integration-connections"] as const;

export function useIntegrationConnections() {
  return useQuery({
    queryKey: listKey,
    queryFn: async () => {
      const { data } = await apiClient.get<IntegrationConnection[]>(
        "/api/v1/admin/integration-connections"
      );
      return data;
    },
  });
}

export interface UpdateIntegrationConnectionPayload {
  id: string;
  mode: string;
  isEnabled: boolean;
  intervalMinutes: number;
  conflictPolicy: string;
  targetCompanyId: string | null;
}

export function useUpdateIntegrationConnection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: UpdateIntegrationConnectionPayload) => {
      await apiClient.put(`/api/v1/admin/integration-connections/${payload.id}`, payload);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: listKey }),
  });
}

export function useTriggerIntegrationSync() {
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/api/v1/admin/integration-connections/${id}/sync`, {});
    },
  });
}

export function useDeleteIntegrationConnection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/admin/integration-connections/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: listKey }),
  });
}
