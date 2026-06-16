import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { Worklog } from "@/types";
import { normalizeQueryParams, queryKeys, type QueryParams } from "./query-keys";
import type { PagedResult } from "./tickets";

interface WorklogQueryParams extends QueryParams {
  projectId?: string;
  ticketId?: string;
  userId?: string;
  from?: string;
  to?: string;
  includeSubprojects?: boolean;
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

export interface CreateWorklogsBatchItem {
  date: string;
  hours: number;
  description: string;
  isBillable: boolean;
}

export interface CreateWorklogsBatchRequest {
  projectId: string;
  ticketId: string;
  items: CreateWorklogsBatchItem[];
  overrideUserId?: string;
}

export function useCreateWorklogsBatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateWorklogsBatchRequest) => {
      const { data } = await apiClient.post<string[]>("/api/v1/worklogs/batch", request);
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.worklogs.all() }),
  });
}

export interface ImportWorklogIssue {
  row: number;
  type: "Duplicate" | "Error";
  message: string;
}

export interface ImportWorklogsResult {
  totalRows: number;
  created: number;
  duplicates: number;
  errors: number;
  issues: ImportWorklogIssue[];
}

export function useImportWorklogs() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      file,
      overrideUserId,
    }: {
      projectId: string;
      file: File;
      overrideUserId?: string;
    }) => {
      const form = new FormData();
      form.append("projectId", projectId);
      form.append("file", file);
      if (overrideUserId) form.append("overrideUserId", overrideUserId);
      const { data } = await apiClient.post<ImportWorklogsResult>("/api/v1/worklogs/import", form);
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.worklogs.all() }),
  });
}

export interface UpdateWorklogRequest {
  projectId: string;
  worklogId: string;
  ticketId: string;
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
