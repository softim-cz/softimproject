import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type {
  GitHubRepo,
  GitHubStatus,
  Project,
  ProjectAllowedTaskTypes,
  ProjectCustomFieldValue,
  ProjectRole,
  UserOption,
} from "@/types";
import { invalidateQueryKeys, queryKeys } from "./query-keys";

import type { PagedResult } from "./tickets";

// `useProjects` unwraps the paged envelope — most consumers only need the array.
// If you need totalCount/hasNext, use `useProjectsPaged` below.
export function useProjects() {
  return useQuery({
    queryKey: queryKeys.projects.all(),
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<Project>>("/api/v1/projects");
      return data.items;
    },
  });
}

export function useProjectsPaged(page = 1, pageSize = 50) {
  return useQuery({
    queryKey: [...queryKeys.projects.all(), "paged", page, pageSize],
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<Project>>("/api/v1/projects", {
        params: { page, pageSize },
      });
      return data;
    },
  });
}

export function useProject(id: string) {
  return useQuery({
    queryKey: queryKeys.projects.detail(id),
    queryFn: async () => {
      const { data } = await apiClient.get<Project>(`/api/v1/projects/${id}`);
      return data;
    },
    enabled: !!id,
  });
}

export function useProjectByCode(code: string) {
  return useQuery({
    queryKey: queryKeys.projects.byCode(code),
    queryFn: async () => {
      const { data } = await apiClient.get<Project>(`/api/v1/projects/by-code/${code}`);
      return data;
    },
    enabled: !!code,
  });
}

export function useProjectAllowedTaskTypes(projectId: string) {
  return useQuery({
    queryKey: [...queryKeys.projects.detail(projectId), "allowed-task-types"],
    queryFn: async () => {
      const { data } = await apiClient.get<ProjectAllowedTaskTypes>(
        `/api/v1/projects/${projectId}/allowed-task-types`
      );
      return data;
    },
    enabled: !!projectId,
  });
}

export function useSetProjectAllowedTaskTypes(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (taskTypeIds: string[]) => {
      await apiClient.put(`/api/v1/projects/${projectId}/allowed-task-types`, {
        projectId,
        taskTypeIds,
      });
    },
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: [...queryKeys.projects.detail(projectId), "allowed-task-types"],
      }),
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (project: Partial<Project>) => {
      const { data } = await apiClient.post<Project>("/api/v1/projects", project);
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.projects.all() }),
  });
}

export function useUpdateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: Partial<Project> & { id: string }) => {
      const { data } = await apiClient.put<Project>(`/api/v1/projects/${payload.id}`, payload);
      return data;
    },
    onSuccess: async (_, { id }) => {
      await invalidateQueryKeys(
        queryClient,
        queryKeys.projects.all(),
        queryKeys.projects.detail(id),
        queryKeys.projects.byCodeRoot()
      );
    },
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/projects/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.projects.all() }),
  });
}

export function useProjectUsers() {
  return useQuery({
    queryKey: queryKeys.projects.users(),
    queryFn: async () => {
      const { data } = await apiClient.get<UserOption[]>("/api/v1/projects/users");
      return data;
    },
  });
}

export function useAddProjectMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      projectId: string;
      userId: string;
      role: ProjectRole;
      hourlyRateOverride?: number;
    }) => {
      const { data } = await apiClient.post<string>(
        `/api/v1/projects/${payload.projectId}/members`,
        payload
      );
      return data;
    },
    onSuccess: () =>
      invalidateQueryKeys(queryClient, queryKeys.projects.byCodeRoot(), queryKeys.projects.all()),
  });
}

export function useUpdateProjectMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      projectId: string;
      memberId: string;
      role: ProjectRole;
      hourlyRateOverride?: number;
    }) => {
      await apiClient.put(
        `/api/v1/projects/${payload.projectId}/members/${payload.memberId}`,
        payload
      );
    },
    onSuccess: () =>
      invalidateQueryKeys(queryClient, queryKeys.projects.byCodeRoot(), queryKeys.projects.all()),
  });
}

export function useRemoveProjectMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ projectId, memberId }: { projectId: string; memberId: string }) => {
      await apiClient.delete(`/api/v1/projects/${projectId}/members/${memberId}`);
    },
    onSuccess: () =>
      invalidateQueryKeys(queryClient, queryKeys.projects.byCodeRoot(), queryKeys.projects.all()),
  });
}

export function useProjectCustomFieldValues(projectId: string) {
  return useQuery({
    queryKey: queryKeys.projects.customFields(projectId),
    queryFn: async () => {
      const { data } = await apiClient.get<ProjectCustomFieldValue[]>(
        `/api/v1/projects/${projectId}/custom-fields`
      );
      return data;
    },
    enabled: !!projectId,
  });
}

export function useSaveProjectCustomFieldValues() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      values,
    }: {
      projectId: string;
      values: { customFieldDefinitionId: string; value?: string }[];
    }) => {
      await apiClient.put(`/api/v1/projects/${projectId}/custom-fields`, { projectId, values });
    },
    onSuccess: (_, { projectId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.projects.customFields(projectId) });
    },
  });
}

export function useTestGitHubConnection() {
  return useMutation({
    mutationFn: async (projectId: string) => {
      const { data } = await apiClient.post<{
        success: boolean;
        error?: string;
        repositoryName?: string;
      }>(`/api/v1/projects/${projectId}/github/test`);
      return data;
    },
  });
}

export function useTriggerGitHubSync() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (projectId: string) => {
      const { data } = await apiClient.post<{ synced: number; failed: number; error?: string }>(
        `/api/v1/projects/${projectId}/github/sync`
      );
      return data;
    },
    onSuccess: (_, projectId) =>
      invalidateQueryKeys(
        queryClient,
        queryKeys.tickets.listRoot(projectId),
        queryKeys.kanban.board(projectId)
      ),
  });
}

export function useGitHubStatus() {
  return useQuery({
    queryKey: queryKeys.github.status(),
    queryFn: async () => {
      const { data } = await apiClient.get<GitHubStatus>("/api/v1/github/status");
      return data;
    },
  });
}

export function useGitHubRepos(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.github.repos(),
    queryFn: async () => {
      const { data } = await apiClient.get<GitHubRepo[]>("/api/v1/github/repos");
      return data;
    },
    enabled,
  });
}

export function useGitHubAuthorize() {
  return useMutation({
    mutationFn: async (projectId: string) => {
      const { data } = await apiClient.get<{ url: string }>(
        `/api/v1/github/authorize?projectId=${projectId}`
      );
      return data;
    },
  });
}

export function useLinkGitHubRepo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      repositoryFullName,
    }: {
      projectId: string;
      repositoryFullName: string;
    }) => {
      await apiClient.post(`/api/v1/projects/${projectId}/github/link`, {
        projectId,
        repositoryFullName,
      });
    },
    onSuccess: (_, { projectId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.projects.detail(projectId) });
    },
  });
}

export function useUnlinkGitHubRepo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (projectId: string) => {
      await apiClient.post(`/api/v1/projects/${projectId}/github/unlink`);
    },
    onSuccess: (_, projectId) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.projects.detail(projectId) });
    },
  });
}

export function useDisconnectGitHub() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      await apiClient.delete("/api/v1/github/disconnect");
    },
    onSuccess: () =>
      invalidateQueryKeys(queryClient, queryKeys.github.all(), queryKeys.projects.all()),
  });
}

export function useGenerateClientAccessToken() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (projectId: string) => {
      const { data } = await apiClient.post<{ token: string }>(
        `/api/v1/projects/${projectId}/portal/token`
      );
      return data.token;
    },
    onSuccess: () => invalidateQueryKeys(queryClient, queryKeys.projects.all()),
  });
}

export function useRevokeClientAccess() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (projectId: string) => {
      await apiClient.post(`/api/v1/projects/${projectId}/portal/revoke`);
    },
    onSuccess: () => invalidateQueryKeys(queryClient, queryKeys.projects.all()),
  });
}
