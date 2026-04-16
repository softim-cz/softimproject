import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";

export interface ViewConfigurationDto {
  id: string;
  userId: string;
  projectId?: string;
  viewType: string;
  configurationJson: string;
}

export function useViewConfiguration(viewType: string, projectId?: string) {
  return useQuery({
    queryKey: ["view-configuration", viewType, projectId ?? "global"],
    queryFn: async () => {
      const params = new URLSearchParams({ viewType });
      if (projectId) params.set("projectId", projectId);
      const { data } = await apiClient.get<ViewConfigurationDto | null>(
        `/api/v1/view-configurations?${params.toString()}`
      );
      return data;
    },
  });
}

export function useUpsertViewConfiguration() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      projectId,
      viewType,
      configurationJson,
    }: {
      projectId?: string;
      viewType: string;
      configurationJson: string;
    }) => {
      const { data } = await apiClient.put<string>(`/api/v1/view-configurations`, {
        projectId,
        viewType,
        configurationJson,
      });
      return data;
    },
    onSuccess: (_, { viewType, projectId }) => {
      queryClient.invalidateQueries({
        queryKey: ["view-configuration", viewType, projectId ?? "global"],
      });
    },
  });
}
