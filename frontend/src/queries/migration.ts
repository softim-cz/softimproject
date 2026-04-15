import { useQuery, useMutation } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type {
  EpProjectPreview,
  EpLookupsResult,
  EpUserMapping,
  MigrationProgress,
  MigrationJob,
} from "@/types";

export function useTestEpConnection() {
  return useMutation({
    mutationFn: async (params: { baseUrl: string; apiKey: string }) => {
      const { data } = await apiClient.post<{ success: boolean; error?: string }>(
        "/api/v1/migration/test-connection",
        params
      );
      return data;
    },
  });
}

export function useFetchEpProjects() {
  return useMutation({
    mutationFn: async (params: { baseUrl: string; apiKey: string }) => {
      const { data } = await apiClient.post<EpProjectPreview[]>(
        "/api/v1/migration/fetch-projects",
        params
      );
      return data;
    },
  });
}

export function useFetchIssueCounts() {
  return useMutation({
    mutationFn: async (params: {
      baseUrl: string;
      apiKey: string;
      sessionId: string;
      projectIds: number[];
    }) => {
      await apiClient.post("/api/v1/migration/fetch-issue-counts", params);
    },
  });
}

export function useFetchEpLookups() {
  return useMutation({
    mutationFn: async (params: { baseUrl: string; apiKey: string }) => {
      const { data } = await apiClient.post<EpLookupsResult>(
        "/api/v1/migration/fetch-lookups",
        params
      );
      return data;
    },
  });
}

export function useFetchEpUsers() {
  return useMutation({
    mutationFn: async (params: { baseUrl: string; apiKey: string }) => {
      const { data } = await apiClient.post<EpUserMapping[]>(
        "/api/v1/migration/fetch-users",
        params
      );
      return data;
    },
  });
}

export function useStartMigration() {
  return useMutation({
    mutationFn: async (params: {
      baseUrl: string;
      apiKey: string;
      projectIds: number[];
      trackerMapping: Record<number, string | null>;
      statusMapping: Record<number, string>;
      priorityMapping: Record<number, string>;
      userMapping: Record<number, string | null>;
      skipClosedIssues: boolean;
      skipAttachments: boolean;
      importComments: boolean;
      importWorklogs: boolean;
      importChecklists: boolean;
      createMissingUsers: boolean;
      autoCreateTrackers: Record<number, string>;
      autoCreateStatuses: Record<number, string>;
      autoCreateStatusIsClosed: Record<number, boolean>;
      autoCreatePriorities: Record<number, string>;
    }) => {
      const { data } = await apiClient.post<string>(
        "/api/v1/migration/start",
        params
      );
      return data;
    },
  });
}

export function useCancelMigration() {
  return useMutation({
    mutationFn: async (jobId: string) => {
      await apiClient.post(`/api/v1/migration/${jobId}/cancel`);
    },
  });
}

export function useMigrationProgress(jobId: string | null) {
  return useQuery({
    queryKey: ["migration", "progress", jobId],
    queryFn: async () => {
      const { data } = await apiClient.get<MigrationProgress>(
        `/api/v1/migration/${jobId}/progress`
      );
      return data;
    },
    enabled: !!jobId,
    refetchInterval: 2000,
  });
}

export function useMigrationHistory() {
  return useQuery({
    queryKey: ["migration", "history"],
    queryFn: async () => {
      const { data } = await apiClient.get<MigrationJob[]>(
        "/api/v1/migration/history"
      );
      return data;
    },
  });
}

