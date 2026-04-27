import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { Worklog } from "@/types";
import { normalizeQueryParams, queryKeys, type QueryParams } from "./query-keys";
import type { PagedResult } from "./tickets";

interface WorklogQueryParams extends QueryParams {
  projectId?: string;
  ticketId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

// Unwraps paged envelope for legacy consumers. Use `useWorklogsPaged` when you
// need totalCount/hasNext to drive a pagination UI.
export function useWorklogs(params?: WorklogQueryParams) {
  const normalizedParams = normalizeQueryParams(params);

  return useQuery({
    queryKey: queryKeys.worklogs.list(normalizedParams),
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<Worklog>>("/api/v1/worklogs", {
        params: normalizedParams,
      });
      return data.items;
    },
  });
}

export function useWorklogsPaged(params?: WorklogQueryParams) {
  const normalizedParams = normalizeQueryParams(params);

  return useQuery({
    queryKey: [...queryKeys.worklogs.list(normalizedParams), "paged"],
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<Worklog>>("/api/v1/worklogs", {
        params: normalizedParams,
      });
      return data;
    },
  });
}

export interface CreateWorklogRequest {
  projectId: string;
  ticketId: string;
  date: string;
  hours: number;
  description: string;
  isBillable: boolean;
  overrideUserId?: string;
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

export interface UpdateWorklogRequest {
  projectId: string;
  worklogId: string;
  date: string;
  hours: number;
  description: string;
  isBillable: boolean;
  invoiced?: string;
  overrideUserId?: string;
}

export function useUpdateWorklog() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: UpdateWorklogRequest) => {
      await apiClient.put(`/api/v1/worklogs/${request.worklogId}`, request);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.worklogs.all() }),
  });
}

export function useDeleteWorklog() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ projectId, worklogId }: { projectId: string; worklogId: string }) => {
      await apiClient.delete(`/api/v1/worklogs/${worklogId}`, { params: { projectId } });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.worklogs.all() }),
  });
}
