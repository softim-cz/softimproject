import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { ApiKey, GenerateApiKeyResult } from "@/types";

const API_KEYS = ["api-keys"] as const;

export function useApiKeys() {
  return useQuery({
    queryKey: API_KEYS,
    queryFn: async () => {
      const { data } = await apiClient.get<ApiKey[]>("/api/v1/api-keys");
      return data;
    },
  });
}

export function useGenerateApiKey() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; expiresInDays?: number }) => {
      const { data } = await apiClient.post<GenerateApiKeyResult>("/api/v1/api-keys", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: API_KEYS }),
  });
}

export function useRevokeApiKey() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/api-keys/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: API_KEYS }),
  });
}
