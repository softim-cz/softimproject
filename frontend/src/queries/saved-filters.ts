import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";

export interface SavedFilterDto {
  id: string;
  name: string;
  userId?: string;
  projectId?: string;
  viewType: string;
  filterJson: string;
  isSystem: boolean;
  sortOrder: number;
}

export function useSavedFilters(viewType: string, projectId?: string) {
  return useQuery({
    queryKey: ["saved-filters", viewType, projectId ?? "global"],
    queryFn: async () => {
      const params = new URLSearchParams({ viewType });
      if (projectId) params.set("projectId", projectId);
      const { data } = await apiClient.get<SavedFilterDto[]>(
        `/api/v1/saved-filters?${params.toString()}`
      );
      return data;
    },
  });
}

export function useCreateSavedFilter() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (input: {
      name: string;
      projectId?: string;
      viewType: string;
      filterJson: string;
      isSystem: boolean;
      sortOrder: number;
    }) => {
      const { data } = await apiClient.post<string>(`/api/v1/saved-filters`, input);
      return data;
    },
    onSuccess: (_, { viewType, projectId }) => {
      queryClient.invalidateQueries({
        queryKey: ["saved-filters", viewType, projectId ?? "global"],
      });
    },
  });
}

export function useUpdateSavedFilter() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (input: {
      id: string;
      name: string;
      filterJson: string;
      sortOrder: number;
    }) => {
      await apiClient.put(`/api/v1/saved-filters/${input.id}`, input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["saved-filters"] });
    },
  });
}

export function useDeleteSavedFilter() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/v1/saved-filters/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["saved-filters"] });
    },
  });
}
