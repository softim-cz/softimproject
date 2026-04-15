import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { Worklog } from "@/types";
import { normalizeQueryParams, queryKeys, type QueryParams } from "./query-keys";

interface WorklogQueryParams extends QueryParams {
  projectId?: string;
  from?: string;
  to?: string;
}

export function useWorklogs(params?: WorklogQueryParams) {
  const normalizedParams = normalizeQueryParams(params);

  return useQuery({
    queryKey: queryKeys.worklogs.list(normalizedParams),
    queryFn: async () => {
      const { data } = await apiClient.get<Worklog[]>("/api/v1/worklogs", {
        params: normalizedParams,
      });
      return data;
    },
  });
}

export interface CreateWorklogRequest {
  projectId: string;
  ticketId?: string;
  date: string;
  hours: number;
  description?: string;
  isBillable: boolean;
}

export function useCreateWorklog() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (worklog: CreateWorklogRequest) => {
      const { data } = await apiClient.post<Worklog>("/api/v1/worklogs", worklog);
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.worklogs.all() }),
  });
}

export function useUpdateWorklog() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...worklog }: Partial<Worklog> & { id: string }) => {
      const { data } = await apiClient.put<Worklog>(`/api/v1/worklogs/${id}`, worklog);
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.worklogs.all() }),
  });
}

export function useDeleteWorklog() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/worklogs/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.worklogs.all() }),
  });
}
