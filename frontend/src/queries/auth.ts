import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { User } from "@/types";

export function useCurrentUser() {
  return useQuery({
    queryKey: ["currentUser"],
    queryFn: async () => {
      const { data } = await apiClient.get<User>("/api/v1/auth/me");
      return data;
    },
  });
}
