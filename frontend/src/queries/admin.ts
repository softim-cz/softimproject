import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import type { AdminUser, GlobalRole } from "@/types";

export function useAdminUsers() {
  return useQuery({
    queryKey: ["admin", "users"],
    queryFn: async () => {
      const { data } = await apiClient.get<AdminUser[]>("/api/v1/admin/users");
      return data;
    },
  });
}

export function useUpdateUserRoles() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { userId: string; applicationRoleIds: string[] }) => {
      await apiClient.put(`/api/v1/admin/users/${body.userId}/roles`, body);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      qc.invalidateQueries({ queryKey: ["currentUser"] });
    },
  });
}

export function useUpdateUserGlobalRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { userId: string; globalRole: GlobalRole }) => {
      await apiClient.put(`/api/v1/admin/users/${body.userId}/global-role`, body);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      qc.invalidateQueries({ queryKey: ["currentUser"] });
    },
  });
}

export function useUpdateUserActive() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { userId: string; isActive: boolean }) => {
      await apiClient.put(`/api/v1/admin/users/${body.userId}/active`, body);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      qc.invalidateQueries({ queryKey: ["currentUser"] });
    },
  });
}
