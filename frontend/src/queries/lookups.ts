import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type {
  Company,
  ProjectType,
  ProjectState,
  TaskType,
  TaskState,
  ApplicationRoleEntity,
} from "@/types";

// === Companies ===

export function useCompanies() {
  return useQuery({
    queryKey: ["lookups", "companies"],
    queryFn: async () => {
      const { data } = await apiClient.get<Company[]>("/api/v1/lookups/companies");
      return data;
    },
  });
}

export function useCreateCompany() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; description?: string }) => {
      const { data } = await apiClient.post<string>("/api/v1/lookups/companies", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "companies"] }),
  });
}

export function useUpdateCompany() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: Company) => {
      await apiClient.put(`/api/v1/lookups/companies/${body.id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "companies"] }),
  });
}

export function useDeleteCompany() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/lookups/companies/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "companies"] }),
  });
}

// === Project Types ===

export function useProjectTypes() {
  return useQuery({
    queryKey: ["lookups", "project-types"],
    queryFn: async () => {
      const { data } = await apiClient.get<ProjectType[]>("/api/v1/lookups/project-types");
      return data;
    },
  });
}

export function useCreateProjectType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; description?: string; sortOrder: number }) => {
      const { data } = await apiClient.post<string>("/api/v1/lookups/project-types", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "project-types"] }),
  });
}

export function useUpdateProjectType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: ProjectType) => {
      await apiClient.put(`/api/v1/lookups/project-types/${body.id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "project-types"] }),
  });
}

export function useDeleteProjectType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/lookups/project-types/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "project-types"] }),
  });
}

// === Project States ===

export function useProjectStates() {
  return useQuery({
    queryKey: ["lookups", "project-states"],
    queryFn: async () => {
      const { data } = await apiClient.get<ProjectState[]>("/api/v1/lookups/project-states");
      return data;
    },
  });
}

export function useCreateProjectState() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; color: string; sortOrder: number; isDefault: boolean }) => {
      const { data } = await apiClient.post<string>("/api/v1/lookups/project-states", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "project-states"] }),
  });
}

export function useUpdateProjectState() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: ProjectState) => {
      await apiClient.put(`/api/v1/lookups/project-states/${body.id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "project-states"] }),
  });
}

export function useDeleteProjectState() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/lookups/project-states/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "project-states"] }),
  });
}

// === Task Types ===

export function useTaskTypes() {
  return useQuery({
    queryKey: ["lookups", "task-types"],
    queryFn: async () => {
      const { data } = await apiClient.get<TaskType[]>("/api/v1/lookups/task-types");
      return data;
    },
  });
}

export function useCreateTaskType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; icon?: string; sortOrder: number }) => {
      const { data } = await apiClient.post<string>("/api/v1/lookups/task-types", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "task-types"] }),
  });
}

export function useUpdateTaskType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: TaskType) => {
      await apiClient.put(`/api/v1/lookups/task-types/${body.id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "task-types"] }),
  });
}

export function useDeleteTaskType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/lookups/task-types/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "task-types"] }),
  });
}

// === Task States ===

export function useTaskStates() {
  return useQuery({
    queryKey: ["lookups", "task-states"],
    queryFn: async () => {
      const { data } = await apiClient.get<TaskState[]>("/api/v1/lookups/task-states");
      return data;
    },
  });
}

export function useCreateTaskState() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; color: string; sortOrder: number; isDefault: boolean; isClosedState: boolean }) => {
      const { data } = await apiClient.post<string>("/api/v1/lookups/task-states", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "task-states"] }),
  });
}

export function useUpdateTaskState() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: TaskState) => {
      await apiClient.put(`/api/v1/lookups/task-states/${body.id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "task-states"] }),
  });
}

export function useDeleteTaskState() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/lookups/task-states/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "task-states"] }),
  });
}

// === Application Roles ===

export function useApplicationRoles() {
  return useQuery({
    queryKey: ["lookups", "application-roles"],
    queryFn: async () => {
      const { data } = await apiClient.get<ApplicationRoleEntity[]>("/api/v1/lookups/application-roles");
      return data;
    },
  });
}

export function useCreateApplicationRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: Omit<ApplicationRoleEntity, "id">) => {
      const { data } = await apiClient.post<string>("/api/v1/lookups/application-roles", body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "application-roles"] }),
  });
}

export function useUpdateApplicationRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: ApplicationRoleEntity) => {
      await apiClient.put(`/api/v1/lookups/application-roles/${body.id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "application-roles"] }),
  });
}

export function useDeleteApplicationRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/lookups/application-roles/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["lookups", "application-roles"] }),
  });
}
