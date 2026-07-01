"use client";

/* eslint-disable react-hooks/set-state-in-effect */
import { use, useState, useEffect } from "react";
import {
  useProjectByCode,
  useUpdateProject,
  useDeleteProject,
  useGenerateClientAccessToken,
  useRevokeClientAccess,
} from "@/queries/projects";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { Settings, AlertTriangle, ArrowLeft } from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { ProjectStatus } from "@/types";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ClientPortalLink } from "./_components/client-portal-link";
import { CustomFieldsSection } from "./_components/custom-fields-section";
import { AllowedTaskTypesSection } from "./_components/allowed-task-types-section";
import { GitHubIntegrationSection } from "./_components/github-integration-section";
import { MembersSection } from "./_components/members-section";
import { BoardConfigSection } from "./_components/board-config-section";

export default function ProjectSettingsPage({ params }: { params: Promise<{ code: string }> }) {
  const t = useTranslations("ProjectSettings");
  const tProjects = useTranslations("Projects");
  const tAdmin = useTranslations("AdminProjects");
  const { code } = use(params);
  const router = useRouter();
  const { data: project, isLoading, error } = useProjectByCode(code);
  const projectId = project?.id ?? "";
  const updateProject = useUpdateProject();
  const deleteProject = useDeleteProject();
  const generateToken = useGenerateClientAccessToken();
  const revokeToken = useRevokeClientAccess();
  const [editName, setEditName] = useState("");
  const [editCode, setEditCode] = useState("");
  const [confirmDelete, setConfirmDelete] = useState("");
  const [generalDirty, setGeneralDirty] = useState(false);

  useEffect(() => {
    if (project) {
      setEditName(project.name);
      setEditCode(project.code);
      setGeneralDirty(false);
    }
  }, [project]);

  if (isLoading) {
    return (
      <div className="space-y-4 max-w-3xl">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-4 w-96" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error || !project) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        {t("loadFailed")}
      </div>
    );
  }

  const handleStatusChange = async (status: ProjectStatus) => {
    try {
      await updateProject.mutateAsync({ id: projectId, status });
      toast.success(t("statusUpdated"));
    } catch {
      toast.error(t("statusUpdateFailed"));
    }
  };

  const codeValid = /^[A-Z]{2,6}$/.test(editCode);
  const nameValid = editName.trim().length > 0;

  const handleSaveGeneral = async () => {
    if (!nameValid || !codeValid) return;
    try {
      const codeChanged = editCode !== project.code;
      await updateProject.mutateAsync({
        id: projectId,
        name: editName.trim(),
        code: editCode,
      });
      toast.success(t("settingsSaved"));
      setGeneralDirty(false);
      if (codeChanged) {
        router.push(`/admin/projects/${editCode}/settings`);
      }
    } catch {
      toast.error(t("settingsSaveFailed"));
    }
  };

  const handleClientAccessToggle = async () => {
    try {
      await updateProject.mutateAsync({
        id: projectId,
        clientAccessEnabled: !project.clientAccessEnabled,
      });
      toast.success(
        project.clientAccessEnabled ? t("clientAccessDisabled") : t("clientAccessEnabled")
      );
    } catch {
      toast.error(t("clientAccessUpdateFailed"));
    }
  };

  const handleGenerateToken = async () => {
    const confirmMessage = project.clientAccessToken
      ? t("regenerateConfirm")
      : t("generateConfirm");
    if (!window.confirm(confirmMessage)) return;
    try {
      await generateToken.mutateAsync(projectId);
      toast.success(t("tokenGenerated"));
    } catch {
      toast.error(t("tokenGenerateFailed"));
    }
  };

  const handleRevokeToken = async () => {
    if (!window.confirm(t("revokeConfirm"))) return;
    try {
      await revokeToken.mutateAsync(projectId);
      toast.success(t("accessRevoked"));
    } catch {
      toast.error(t("accessRevokeFailed"));
    }
  };

  return (
    <div className="space-y-8 max-w-3xl">
      <div>
        <Link
          href="/admin/projects"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground mb-2"
        >
          <ArrowLeft className="h-4 w-4" />
          {tAdmin("backToList")}
        </Link>
        <h1 className="text-2xl font-bold text-foreground">{project.name}</h1>
        <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
      </div>

      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <Settings className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-card-foreground">{t("general")}</h2>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("name")}
            </label>
            <input
              type="text"
              value={editName}
              onChange={(e) => {
                setEditName(e.target.value);
                setGeneralDirty(true);
              }}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {!nameValid && editName !== project.name && (
              <p className="text-xs text-destructive mt-1">{t("nameRequired")}</p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("code")}
            </label>
            <input
              type="text"
              value={editCode}
              onChange={(e) => {
                setEditCode(e.target.value.toUpperCase());
                setGeneralDirty(true);
              }}
              maxLength={6}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground font-mono focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {!codeValid && editCode !== project.code && (
              <p className="text-xs text-destructive mt-1">{t("codeValidation")}</p>
            )}
          </div>
        </div>

        <div className="flex justify-end pt-2">
          <button
            onClick={handleSaveGeneral}
            disabled={!generalDirty || !nameValid || !codeValid || updateProject.isPending}
            className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            {updateProject.isPending ? t("saving") : t("save")}
          </button>
        </div>

        <div>
          <label className="block text-sm font-medium text-card-foreground mb-1">
            {t("status")}
          </label>
          <select
            value={project.status}
            onChange={(e) => handleStatusChange(e.target.value as ProjectStatus)}
            className="rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {Object.values(ProjectStatus).map((status) => (
              <option key={status} value={status}>
                {tProjects(`status.${status}` as "status.Active")}
              </option>
            ))}
          </select>
        </div>

        {project.projectTemplateName && (
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("template")}
            </label>
            <p className="text-sm text-muted-foreground">{project.projectTemplateName}</p>
          </div>
        )}

        <div className="space-y-2">
          <label className="flex items-center gap-2 text-sm cursor-pointer">
            <input
              type="checkbox"
              checked={project.clientAccessEnabled}
              onChange={handleClientAccessToggle}
              disabled={updateProject.isPending || !project.clientAccessToken}
              className="rounded"
            />
            {t("clientPortalEnabled")}
          </label>
          {project.clientAccessEnabled && project.clientAccessToken && (
            <ClientPortalLink token={project.clientAccessToken} />
          )}
          <div className="flex items-center gap-2 pt-1">
            <button
              onClick={handleGenerateToken}
              disabled={generateToken.isPending}
              className="px-3 py-1.5 rounded-lg border border-border text-xs font-medium text-foreground hover:bg-muted transition-colors disabled:opacity-50"
            >
              {generateToken.isPending
                ? t("generating")
                : project.clientAccessToken
                  ? t("regenerateToken")
                  : t("generateToken")}
            </button>
            {project.clientAccessToken && (
              <button
                onClick={handleRevokeToken}
                disabled={revokeToken.isPending}
                className="px-3 py-1.5 rounded-lg border border-destructive/50 text-xs font-medium text-destructive hover:bg-destructive/5 transition-colors disabled:opacity-50"
              >
                {revokeToken.isPending ? t("revoking") : t("revokeAccess")}
              </button>
            )}
          </div>
          {!project.clientAccessToken && (
            <p className="text-xs text-muted-foreground">{t("noTokenYet")}</p>
          )}
        </div>
      </section>

      <CustomFieldsSection projectId={projectId} />

      <AllowedTaskTypesSection projectId={projectId} />

      <MembersSection projectId={projectId} members={project.members ?? []} />

      <BoardConfigSection projectId={projectId} projectTemplateId={project.projectTemplateId} />

      <GitHubIntegrationSection projectId={projectId} project={project} />

      <section className="rounded-lg border border-destructive/50 bg-destructive/5 p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <AlertTriangle className="h-5 w-5 text-destructive" />
          <h2 className="text-lg font-semibold text-destructive">{t("dangerZone")}</h2>
        </div>
        <p className="text-sm text-muted-foreground">
          {t("dangerDescription")}{" "}
          <span className="font-mono font-semibold text-foreground">{project.code}</span>.
        </p>
        <div className="flex items-center gap-3">
          <input
            type="text"
            value={confirmDelete}
            onChange={(e) => setConfirmDelete(e.target.value)}
            placeholder={project.code}
            className="w-40 rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground font-mono focus:outline-none focus:ring-2 focus:ring-destructive"
          />
          <button
            onClick={async () => {
              try {
                await deleteProject.mutateAsync(projectId);
                toast.success(t("deleted"));
                router.push("/admin/projects");
              } catch {
                toast.error(t("deleteFailed"));
              }
            }}
            disabled={confirmDelete !== project.code || deleteProject.isPending}
            className="px-4 py-2 rounded-lg bg-destructive text-destructive-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            {deleteProject.isPending ? t("deleting") : t("deleteProject")}
          </button>
        </div>
      </section>
    </div>
  );
}
