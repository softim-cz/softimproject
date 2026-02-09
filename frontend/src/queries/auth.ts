import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { CurrentUser } from "@/types";

export function useCurrentUser() {
  return useQuery({
    queryKey: ["currentUser"],
    queryFn: async () => {
      const { data } = await apiClient.get<CurrentUser>("/api/v1/me");
      return data;
    },
  });
}
