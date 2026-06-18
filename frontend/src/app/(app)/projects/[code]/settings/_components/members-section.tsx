"use client";

import { useState } from "react";
import { Users, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import {
  useProjectUsers,
  useAddProjectMember,
  useUpdateProjectMember,
  useRemoveProjectMember,
} from "@/queries/projects";
import { ProjectRole } from "@/types";
import { Avatar } from "@/components/shared/avatar";
import type { ProjectMember } from "@/types";

export function MembersSection({
  projectId,
  members,
}: {
  projectId: string;
  members: ProjectMember[];
}) {
  const t = useTranslations("ProjectSettings");
  const { data: users } = useProjectUsers();
  const addMember = useAddProjectMember();
  const updateMember = useUpdateProjectMember();
  const removeMember = useRemoveProjectMember();
  const [addUserId, setAddUserId] = useState("");
  const [addRole, setAddRole] = useState<ProjectRole>(ProjectRole.Developer);

  const inputClass =
    "rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";
  const btnPrimary =
    "px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50";
  const btnDestructive =
    "inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium text-destructive hover:bg-destructive/10 transition-colors disabled:opacity-50";

  const existingUserIds = new Set(members.map((m) => m.userId));
  const availableUsers = users?.filter((u) => !existingUserIds.has(u.id)) ?? [];

  const handleAdd = async () => {
    if (!addUserId) return;
    try {
      await addMember.mutateAsync({ projectId, userId: addUserId, role: addRole });
      toast.success(t("memberAdded"));
      setAddUserId("");
      setAddRole(ProjectRole.Developer);
    } catch {
      toast.error(t("memberAddFailed"));
    }
  };

  const handleRoleChange = async (member: ProjectMember, role: ProjectRole) => {
    try {
      await updateMember.mutateAsync({
        projectId,
        memberId: member.id,
        role,
        hourlyRateOverride: member.hourlyRateOverride,
      });
      toast.success(t("roleUpdated"));
    } catch {
      toast.error(t("roleUpdateFailed"));
    }
  };

  const handleRemove = async (member: ProjectMember) => {
    if (!window.confirm(t("removeConfirm", { name: member.displayName }))) return;
    try {
      await removeMember.mutateAsync({ projectId, memberId: member.id });
      toast.success(t("memberRemoved"));
    } catch {
      toast.error(t("memberRemoveFailed"));
    }
  };

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <Users className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">{t("members")}</h2>
      </div>

      {members.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t("noMembersYet")}</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left">
                <th className="pb-2 font-medium text-muted-foreground">{t("memberCol")}</th>
                <th className="pb-2 font-medium text-muted-foreground">{t("emailCol")}</th>
                <th className="pb-2 font-medium text-muted-foreground">{t("roleCol")}</th>
                <th className="pb-2 font-medium text-muted-foreground w-20"></th>
              </tr>
            </thead>
            <tbody>
              {members.map((member) => (
                <tr key={member.id} className="border-b border-border/50">
                  <td className="py-3">
                    <div className="flex items-center gap-2">
                      <Avatar
                        name={member.displayName}
                        src={member.avatarUrl}
                        size="md"
                        variant="muted"
                      />
                      <span className="font-medium text-foreground">{member.displayName}</span>
                    </div>
                  </td>
                  <td className="py-3 text-muted-foreground">{member.email}</td>
                  <td className="py-3">
                    <select
                      value={member.role}
                      onChange={(e) => handleRoleChange(member, e.target.value as ProjectRole)}
                      disabled={updateMember.isPending}
                      className={inputClass}
                    >
                      {Object.values(ProjectRole).map((role) => (
                        <option key={role} value={role}>
                          {role}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="py-3">
                    <button
                      onClick={() => handleRemove(member)}
                      disabled={removeMember.isPending}
                      className={btnDestructive}
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="pt-2 border-t border-border">
        <p className="text-sm font-medium text-card-foreground mb-2">{t("addMember")}</p>
        <div className="flex items-end gap-3">
          <div className="flex-1">
            <label className="block text-xs text-muted-foreground mb-1">{t("user")}</label>
            <select
              value={addUserId}
              onChange={(e) => setAddUserId(e.target.value)}
              className={`w-full ${inputClass}`}
            >
              <option value="">{t("selectUser")}</option>
              {availableUsers.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.displayName} ({u.email})
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-muted-foreground mb-1">{t("role")}</label>
            <select
              value={addRole}
              onChange={(e) => setAddRole(e.target.value as ProjectRole)}
              className={inputClass}
            >
              {Object.values(ProjectRole).map((role) => (
                <option key={role} value={role}>
                  {role}
                </option>
              ))}
            </select>
          </div>
          <button
            onClick={handleAdd}
            disabled={!addUserId || addMember.isPending}
            className={btnPrimary}
          >
            {addMember.isPending ? t("adding") : t("add")}
          </button>
        </div>
      </div>
    </section>
  );
}
