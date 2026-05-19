"use client";

/* eslint-disable react-hooks/set-state-in-effect */
import { use, useState, useEffect } from "react";
import {
  useProjectByCode,
  useUpdateProject,
  useDeleteProject,
  useProjectCustomFieldValues,
  useSaveProjectCustomFieldValues,
  useTestGitHubConnection,
  useTriggerGitHubSync,
  useGitHubStatus,
  useGitHubRepos,
  useGitHubAuthorize,
  useLinkGitHubRepo,
  useUnlinkGitHubRepo,
  useDisconnectGitHub,
  useProjectUsers,
  useAddProjectMember,
  useUpdateProjectMember,
  useRemoveProjectMember,
  useGenerateClientAccessToken,
  useRevokeClientAccess,
} from "@/queries/projects";
import {
  useBoard,
  useCreateColumn,
  useUpdateColumn,
  useDeleteColumn,
  useReorderColumns,
} from "@/queries/kanban";
import { useTaskStates } from "@/queries/lookups";
import { Skeleton } from "@/components/shared/loading-skeleton";
import {
  Settings,
  Users,
  LayoutGrid,
  SlidersHorizontal,
  Github,
  Loader2,
  LinkIcon,
  Unlink,
  X,
  AlertTriangle,
  Trash2,
  Pencil,
  Check,
  GripVertical,
  Copy,
  ExternalLink,
  Eye,
  EyeOff,
} from "lucide-react";
import {
  DndContext,
  closestCenter,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { SortableContext, verticalListSortingStrategy, useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils";
import { ProjectStatus, ProjectRole } from "@/types";
import type { ProjectCustomFieldValue, ProjectMember } from "@/types";
import { useSearchParams, useRouter } from "next/navigation";

export default function ProjectSettingsPage({ params }: { params: Promise<{ code: string }> }) {
  const t = useTranslations("ProjectSettings");
  const tProjects = useTranslations("Projects");
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
        router.push(`/projects/${editCode}/settings`);
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
      <p className="text-sm text-muted-foreground">{t("subtitle")}</p>

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
                router.push("/projects");
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

function ClientPortalLink({ token }: { token: string }) {
  const t = useTranslations("ProjectSettings");
  const [copied, setCopied] = useState(false);
  const url =
    typeof window !== "undefined"
      ? `${window.location.origin}/portal/${token}`
      : `/portal/${token}`;

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      toast.error(t("copyFailed"));
    }
  };

  return (
    <div className="flex items-center gap-2 rounded-lg border border-border bg-muted/30 px-3 py-2">
      <input
        type="text"
        value={url}
        readOnly
        className="flex-1 bg-transparent text-xs text-foreground font-mono focus:outline-none"
        onFocus={(e) => e.currentTarget.select()}
      />
      <button
        onClick={handleCopy}
        className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
        title={t("copyLink")}
      >
        {copied ? (
          <Check className="h-3.5 w-3.5 text-green-600" />
        ) : (
          <Copy className="h-3.5 w-3.5" />
        )}
        {copied ? t("copied") : t("copy")}
      </button>
      <a
        href={url}
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
        title={t("openPortal")}
      >
        <ExternalLink className="h-3.5 w-3.5" />
        {t("openLink")}
      </a>
    </div>
  );
}

function CustomFieldsSection({ projectId }: { projectId: string }) {
  const t = useTranslations("ProjectSettings");
  const { data: fields, isLoading } = useProjectCustomFieldValues(projectId);
  const saveMutation = useSaveProjectCustomFieldValues();
  const [values, setValues] = useState<Record<string, string>>({});
  const [dirty, setDirty] = useState(false);

  useEffect(() => {
    if (fields) {
      const initial: Record<string, string> = {};
      fields.forEach((f: ProjectCustomFieldValue) => {
        if (f.value) initial[f.customFieldDefinitionId] = f.value;
      });
      setValues(initial);
      setDirty(false);
    }
  }, [fields]);

  if (isLoading) {
    return (
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-32 w-full" />
      </section>
    );
  }

  if (!fields || fields.length === 0) return null;

  const handleChange = (defId: string, value: string) => {
    setValues((prev) => ({ ...prev, [defId]: value }));
    setDirty(true);
  };

  const handleSave = async () => {
    try {
      await saveMutation.mutateAsync({
        projectId,
        values: fields.map((f: ProjectCustomFieldValue) => ({
          customFieldDefinitionId: f.customFieldDefinitionId,
          value: values[f.customFieldDefinitionId] || undefined,
        })),
      });
      toast.success(t("customFieldsSaved"));
      setDirty(false);
    } catch {
      toast.error(t("customFieldsSaveFailed"));
    }
  };

  const renderInput = (field: ProjectCustomFieldValue) => {
    const val = values[field.customFieldDefinitionId] || "";
    const inputClass =
      "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

    switch (field.fieldType) {
      case "Number":
        return (
          <input
            type="number"
            value={val}
            onChange={(e) => handleChange(field.customFieldDefinitionId, e.target.value)}
            className={inputClass}
          />
        );
      case "Date":
        return (
          <input
            type="date"
            value={val}
            onChange={(e) => handleChange(field.customFieldDefinitionId, e.target.value)}
            className={inputClass}
          />
        );
      case "Select": {
        let opts: string[] = [];
        try {
          opts = JSON.parse(field.options || "[]");
        } catch {
          /* ignore */
        }
        return (
          <select
            value={val}
            onChange={(e) => handleChange(field.customFieldDefinitionId, e.target.value)}
            className={inputClass}
          >
            <option value="">{t("selectPlaceholder")}</option>
            {opts.map((o) => (
              <option key={o} value={o}>
                {o}
              </option>
            ))}
          </select>
        );
      }
      default:
        return (
          <input
            type="text"
            value={val}
            onChange={(e) => handleChange(field.customFieldDefinitionId, e.target.value)}
            className={inputClass}
          />
        );
    }
  };

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <SlidersHorizontal className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">{t("customFields")}</h2>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {fields.map((field: ProjectCustomFieldValue) => (
          <div key={field.customFieldDefinitionId}>
            <label className="block text-sm font-medium text-card-foreground">
              {field.fieldName}
              {field.isRequired && <span className="text-destructive ml-1">*</span>}
            </label>
            {field.description && (
              <p className="text-xs text-muted-foreground mb-1">{field.description}</p>
            )}
            {renderInput(field)}
          </div>
        ))}
      </div>

      <div className="flex justify-end pt-2">
        <button
          onClick={handleSave}
          disabled={!dirty || saveMutation.isPending}
          className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          {saveMutation.isPending ? t("saving") : t("saveCustomFields")}
        </button>
      </div>
    </section>
  );
}

function GitHubIntegrationSection({
  projectId,
  project,
}: {
  projectId: string;
  project: {
    externalSystem?: string;
    externalProjectId?: string;
    gitHubConnectedByUserId?: string;
  };
}) {
  const t = useTranslations("ProjectSettings");
  const { data: ghStatus, refetch: refetchStatus } = useGitHubStatus();
  const { data: repos, isLoading: reposLoading } = useGitHubRepos(ghStatus?.connected ?? false);
  const authorize = useGitHubAuthorize();
  const linkRepo = useLinkGitHubRepo();
  const unlinkRepo = useUnlinkGitHubRepo();
  const disconnect = useDisconnectGitHub();
  const testConnection = useTestGitHubConnection();
  const triggerSync = useTriggerGitHubSync();
  const searchParams = useSearchParams();

  const [selectedRepo, setSelectedRepo] = useState("");

  useEffect(() => {
    const githubParam = searchParams.get("github");
    if (githubParam === "connected") {
      toast.success(t("githubConnected"));
      refetchStatus();
      window.history.replaceState({}, "", window.location.pathname);
    } else if (githubParam === "error") {
      const message = searchParams.get("message") || t("githubConnectFailed");
      toast.error(message);
      window.history.replaceState({}, "", window.location.pathname);
    }
  }, [searchParams, refetchStatus, t]);

  const handleConnect = async () => {
    try {
      const { url } = await authorize.mutateAsync(projectId);
      window.location.href = url;
    } catch {
      toast.error(t("githubAuthFailed"));
    }
  };

  const handleDisconnect = async () => {
    try {
      await disconnect.mutateAsync();
      toast.success(t("githubDisconnected"));
    } catch {
      toast.error(t("githubDisconnectFailed"));
    }
  };

  const handleLinkRepo = async () => {
    if (!selectedRepo) return;
    try {
      await linkRepo.mutateAsync({ projectId, repositoryFullName: selectedRepo });
      toast.success(t("linkedTo", { repo: selectedRepo }));
      setSelectedRepo("");
    } catch {
      toast.error(t("linkFailed"));
    }
  };

  const handleUnlinkRepo = async () => {
    try {
      await unlinkRepo.mutateAsync(projectId);
      toast.success(t("unlinked"));
    } catch {
      toast.error(t("unlinkFailed"));
    }
  };

  const handleTest = async () => {
    try {
      const result = await testConnection.mutateAsync(projectId);
      if (result.success) {
        toast.success(t("connectedTo", { repo: result.repositoryName ?? "" }));
      } else {
        toast.error(result.error ?? t("connectionFailed"));
      }
    } catch {
      toast.error(t("connectionFailed"));
    }
  };

  const handleSync = async () => {
    try {
      const result = await triggerSync.mutateAsync(projectId);
      if (result.error) {
        toast.error(result.error);
      } else {
        toast.success(
          t("synced", {
            synced: result.synced,
            failedSuffix: result.failed > 0 ? t("failedSuffix", { failed: result.failed }) : "",
          })
        );
      }
    } catch {
      toast.error(t("syncFailed"));
    }
  };

  const isConnected = ghStatus?.connected ?? false;
  const hasLinkedRepo = project.externalSystem === "GitHub" && !!project.externalProjectId;

  const btnPrimary =
    "px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50";
  const btnOutline =
    "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-border text-sm font-medium text-foreground hover:bg-muted transition-colors disabled:opacity-50";
  const btnDestructive =
    "inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium text-destructive hover:bg-destructive/10 transition-colors disabled:opacity-50";

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <Github className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">{t("github")}</h2>
      </div>

      {!isConnected && (
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">{t("githubConnectIntro")}</p>
          <button onClick={handleConnect} disabled={authorize.isPending} className={btnPrimary}>
            {authorize.isPending ? (
              <span className="inline-flex items-center gap-1.5">
                <Loader2 className="h-4 w-4 animate-spin" />
                {t("githubConnecting")}
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5">
                <LinkIcon className="h-4 w-4" />
                {t("githubConnect")}
              </span>
            )}
          </button>
        </div>
      )}

      {isConnected && !hasLinkedRepo && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-sm text-foreground">
              {t("githubConnectedAs")} <span className="font-semibold">@{ghStatus!.login}</span>
            </p>
            <button
              onClick={handleDisconnect}
              disabled={disconnect.isPending}
              className={btnDestructive}
            >
              {disconnect.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <X className="h-4 w-4" />
              )}
              {t("githubDisconnect")}
            </button>
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("repository")}
            </label>
            <select
              value={selectedRepo}
              onChange={(e) => setSelectedRepo(e.target.value)}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">{t("selectRepo")}</option>
              {reposLoading && <option disabled>{t("loadingRepos")}</option>}
              {repos?.map((r) => (
                <option key={r.fullName} value={r.fullName}>
                  {r.fullName}
                  {r.isPrivate ? ` ${t("private")}` : ""}
                </option>
              ))}
            </select>
          </div>

          <button
            onClick={handleLinkRepo}
            disabled={!selectedRepo || linkRepo.isPending}
            className={btnPrimary}
          >
            {linkRepo.isPending ? (
              <span className="inline-flex items-center gap-1.5">
                <Loader2 className="h-4 w-4 animate-spin" />
                {t("linking")}
              </span>
            ) : (
              t("linkRepo")
            )}
          </button>
        </div>
      )}

      {isConnected && hasLinkedRepo && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-sm text-foreground">
              {t("githubConnectedAs")} <span className="font-semibold">@{ghStatus!.login}</span>
            </p>
            <button
              onClick={handleDisconnect}
              disabled={disconnect.isPending}
              className={btnDestructive}
            >
              {disconnect.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <X className="h-4 w-4" />
              )}
              {t("githubDisconnect")}
            </button>
          </div>

          <div className="flex items-center justify-between">
            <p className="text-sm text-foreground">
              {t("repository")}:{" "}
              <span className="font-semibold font-mono">{project.externalProjectId}</span>
            </p>
            <button
              onClick={handleUnlinkRepo}
              disabled={unlinkRepo.isPending}
              className={btnDestructive}
            >
              {unlinkRepo.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Unlink className="h-4 w-4" />
              )}
              {t("unlink")}
            </button>
          </div>

          <div className="flex items-center gap-2 pt-2">
            <button onClick={handleTest} disabled={testConnection.isPending} className={btnOutline}>
              {testConnection.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              {t("testConnection")}
            </button>
            <button onClick={handleSync} disabled={triggerSync.isPending} className={btnOutline}>
              {triggerSync.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              {t("syncNow")}
            </button>
          </div>
        </div>
      )}

      {!isConnected && hasLinkedRepo && (
        <div className="space-y-3 mt-4 pt-4 border-t border-border">
          <p className="text-sm text-muted-foreground">
            {t("legacyTokenIntro", { repo: project.externalProjectId ?? "" })}
          </p>
          <div className="flex items-center gap-2">
            <button onClick={handleTest} disabled={testConnection.isPending} className={btnOutline}>
              {testConnection.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              {t("testConnection")}
            </button>
            <button onClick={handleSync} disabled={triggerSync.isPending} className={btnOutline}>
              {triggerSync.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              {t("syncNow")}
            </button>
          </div>
        </div>
      )}
    </section>
  );
}

function MembersSection({ projectId, members }: { projectId: string; members: ProjectMember[] }) {
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
                      {member.avatarUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={member.avatarUrl} alt="" className="h-7 w-7 rounded-full" />
                      ) : (
                        <div className="h-7 w-7 rounded-full bg-muted flex items-center justify-center text-xs font-medium text-muted-foreground">
                          {member.displayName.charAt(0).toUpperCase()}
                        </div>
                      )}
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

const COLOR_PRESETS = [
  null,
  "#ef4444",
  "#f97316",
  "#eab308",
  "#22c55e",
  "#06b6d4",
  "#3b82f6",
  "#8b5cf6",
  "#ec4899",
  "#6b7280",
  "#000000",
] as const;

function ColorPicker({
  value,
  onChange,
}: {
  value: string | undefined;
  onChange: (v: string | undefined) => void;
}) {
  const t = useTranslations("ProjectSettings");
  return (
    <div className="flex items-center gap-1.5">
      {COLOR_PRESETS.map((color) => (
        <button
          key={color ?? "none"}
          type="button"
          onClick={() => onChange(color ?? undefined)}
          className="rounded-full w-5 h-5 border transition-all flex items-center justify-center"
          style={{
            backgroundColor: color ?? "transparent",
            borderColor:
              (value ?? null) === color ? "currentColor" : color ? color : "var(--border)",
            boxShadow:
              (value ?? null) === color
                ? `0 0 0 2px var(--background), 0 0 0 4px ${color ?? "var(--foreground)"}`
                : "none",
          }}
          title={color ?? t("colorNone")}
        >
          {color === null && <X className="h-3 w-3 text-muted-foreground" />}
        </button>
      ))}
    </div>
  );
}

function TaskStateMultiSelect({
  selected,
  onChange,
  taskStates,
  stateAssignments = {},
}: {
  selected: string[];
  onChange: (ids: string[]) => void;
  taskStates: { id: string; name: string; color: string }[];
  stateAssignments?: Record<string, string>;
}) {
  const t = useTranslations("ProjectSettings");
  const toggle = (id: string) => {
    onChange(selected.includes(id) ? selected.filter((s) => s !== id) : [...selected, id]);
  };

  return (
    <div className="space-y-1.5">
      <div className="flex flex-wrap gap-1 min-h-[24px]">
        {selected.map((id) => {
          const ts = taskStates.find((tk) => tk.id === id);
          if (!ts) return null;
          return (
            <span
              key={id}
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium"
              style={{ backgroundColor: ts.color + "20", color: ts.color }}
            >
              {ts.name}
              <button type="button" onClick={() => toggle(id)} className="hover:opacity-70">
                <X className="h-3 w-3" />
              </button>
            </span>
          );
        })}
      </div>
      <div className="border border-input rounded-lg bg-background max-h-32 overflow-y-auto">
        {taskStates.map((ts) => {
          const owner = stateAssignments[ts.id];
          const isOwnedByOther = owner && !selected.includes(ts.id);
          return (
            <label
              key={ts.id}
              className="flex items-center gap-2 px-3 py-1.5 text-sm cursor-pointer hover:bg-muted transition-colors"
              title={isOwnedByOther ? t("stateInColumn", { column: owner }) : undefined}
            >
              <input
                type="checkbox"
                checked={selected.includes(ts.id)}
                onChange={() => toggle(ts.id)}
                className="rounded"
              />
              <span
                className="w-2.5 h-2.5 rounded-full flex-shrink-0"
                style={{ backgroundColor: ts.color }}
              />
              <span className={cn(isOwnedByOther && "text-muted-foreground")}>{ts.name}</span>
              {isOwnedByOther && (
                <span className="ml-auto text-xs text-muted-foreground italic">
                  {t("stateInColumnShort", { column: owner })}
                </span>
              )}
            </label>
          );
        })}
      </div>
    </div>
  );
}

function SortableColumnRow({
  col,
  isEditing,
  onStartEdit,
  onDelete,
  onToggleVisibility,
  editState,
  onEditChange,
  onSaveEdit,
  onCancelEdit,
  updatePending,
  deletePending,
  activeTaskStates,
  stateAssignments,
}: {
  col: {
    id: string;
    name: string;
    wipLimit?: number;
    color?: string;
    isVisible: boolean;
    ticketCount: number;
    taskStates: { id: string; name: string; color: string }[];
  };
  isEditing: boolean;
  onStartEdit: () => void;
  onDelete: () => void;
  onToggleVisibility: () => void;
  editState: { name: string; taskStateIds: string[]; wipLimit: string; color: string | undefined };
  onEditChange: (patch: Partial<typeof editState>) => void;
  onSaveEdit: () => void;
  onCancelEdit: () => void;
  updatePending: boolean;
  deletePending: boolean;
  activeTaskStates: { id: string; name: string; color: string }[];
  stateAssignments: Record<string, string>;
}) {
  const t = useTranslations("ProjectSettings");
  const tCommon = useTranslations("Common");
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: col.id,
  });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };
  const btnOutline =
    "inline-flex items-center justify-center p-1.5 rounded-lg border border-border text-muted-foreground hover:bg-muted transition-colors disabled:opacity-50";
  const btnDestructive =
    "inline-flex items-center gap-1.5 p-1.5 rounded-lg text-destructive hover:bg-destructive/10 transition-colors disabled:opacity-50";
  const inputClass =
    "rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";

  if (isEditing) {
    return (
      <div
        ref={setNodeRef}
        style={style}
        className="px-3 py-3 rounded-lg border border-primary/30 bg-background space-y-3"
      >
        <div className="flex items-center gap-2">
          <input
            type="text"
            value={editState.name}
            onChange={(e) => onEditChange({ name: e.target.value })}
            className={`flex-1 ${inputClass}`}
            placeholder={t("columnName")}
          />
          <input
            type="number"
            value={editState.wipLimit}
            onChange={(e) => onEditChange({ wipLimit: e.target.value })}
            className={`w-20 ${inputClass}`}
            placeholder="WIP"
            min={1}
          />
          <button
            onClick={onSaveEdit}
            disabled={updatePending}
            className={btnOutline}
            title={t("save")}
          >
            <Check className="h-4 w-4 text-green-600" />
          </button>
          <button onClick={onCancelEdit} className={btnOutline} title={tCommon("cancel")}>
            <X className="h-4 w-4" />
          </button>
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">{t("taskStates")}</label>
          <TaskStateMultiSelect
            selected={editState.taskStateIds}
            onChange={(ids) => onEditChange({ taskStateIds: ids })}
            taskStates={activeTaskStates}
            stateAssignments={stateAssignments}
          />
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">{t("color")}</label>
          <ColorPicker value={editState.color} onChange={(c) => onEditChange({ color: c })} />
        </div>
      </div>
    );
  }

  const canHide = col.ticketCount === 0;
  const hideTitle = col.isVisible
    ? canHide
      ? t("hideColumn")
      : t("cannotHide", { count: col.ticketCount })
    : t("showColumn");

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        "flex items-center gap-2 px-3 py-2 rounded-lg border border-border/50 bg-background",
        !col.isVisible && "opacity-60"
      )}
    >
      <button
        {...attributes}
        {...listeners}
        className="cursor-grab active:cursor-grabbing text-muted-foreground hover:text-foreground p-0.5"
      >
        <GripVertical className="h-4 w-4" />
      </button>
      {col.color && (
        <span
          className="w-3 h-3 rounded-full flex-shrink-0"
          style={{ backgroundColor: col.color }}
        />
      )}
      <span className="flex-1 text-sm font-medium text-foreground">
        {col.name}
        {!col.isVisible && (
          <span className="ml-2 text-xs text-muted-foreground italic">{t("hidden")}</span>
        )}
      </span>
      <div className="flex flex-wrap gap-1">
        {col.taskStates.map((ts) => (
          <span
            key={ts.id}
            className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
            style={{ backgroundColor: ts.color + "20", color: ts.color }}
          >
            {ts.name}
          </span>
        ))}
      </div>
      <span className="text-xs text-muted-foreground w-16 text-center">
        WIP: {col.wipLimit ?? "–"}
      </span>
      <button
        onClick={onToggleVisibility}
        disabled={col.isVisible && !canHide}
        className={btnOutline}
        title={hideTitle}
      >
        {col.isVisible ? <Eye className="h-4 w-4" /> : <EyeOff className="h-4 w-4" />}
      </button>
      <button onClick={onStartEdit} className={btnOutline} title={tCommon("edit")}>
        <Pencil className="h-4 w-4" />
      </button>
      <button
        onClick={onDelete}
        disabled={deletePending}
        className={btnDestructive}
        title={tCommon("delete")}
      >
        <Trash2 className="h-4 w-4" />
      </button>
    </div>
  );
}

function BoardConfigSection({
  projectId,
  projectTemplateId,
}: {
  projectId: string;
  projectTemplateId?: string;
}) {
  const t = useTranslations("ProjectSettings");
  const { data: board, isLoading: boardLoading } = useBoard(projectId);
  const { data: taskStates } = useTaskStates(projectTemplateId);
  const createColumn = useCreateColumn();
  const updateColumn = useUpdateColumn();
  const deleteColumn = useDeleteColumn();
  const reorderColumns = useReorderColumns();

  const [newName, setNewName] = useState("");
  const [newTaskStateIds, setNewTaskStateIds] = useState<string[]>([]);
  const [newWipLimit, setNewWipLimit] = useState("");
  const [newColor, setNewColor] = useState<string | undefined>(undefined);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editState, setEditState] = useState({
    name: "",
    taskStateIds: [] as string[],
    wipLimit: "",
    color: undefined as string | undefined,
  });

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const inputClass =
    "rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring";
  const btnPrimary =
    "px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50";

  const activeTaskStates = taskStates?.filter((ts) => ts.isActive) ?? [];
  const columns = board?.columns ?? [];

  if (boardLoading) {
    return (
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <Skeleton className="h-6 w-48" />
        <Skeleton className="h-32 w-full" />
      </section>
    );
  }

  if (!board) {
    return (
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <LayoutGrid className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-card-foreground">{t("boardConfig")}</h2>
        </div>
        <p className="text-sm text-muted-foreground">{t("noBoard")}</p>
      </section>
    );
  }

  const handleAddColumn = async () => {
    if (!newName.trim() || newTaskStateIds.length === 0) return;
    try {
      await createColumn.mutateAsync({
        projectId,
        boardId: board.id,
        name: newName.trim(),
        wipLimit: newWipLimit ? parseInt(newWipLimit, 10) : undefined,
        mapsToTaskStateIds: newTaskStateIds,
        color: newColor,
      });
      toast.success(t("columnAdded"));
      setNewName("");
      setNewTaskStateIds([]);
      setNewWipLimit("");
      setNewColor(undefined);
    } catch {
      toast.error(t("columnAddFailed"));
    }
  };

  const startEditing = (col: (typeof columns)[0]) => {
    setEditingId(col.id);
    setEditState({
      name: col.name,
      taskStateIds: col.taskStates.map((ts) => ts.id),
      wipLimit: col.wipLimit?.toString() ?? "",
      color: col.color,
    });
  };

  const handleSaveEdit = async () => {
    if (!editingId || !editState.name.trim() || editState.taskStateIds.length === 0) return;
    const originalColumn = columns.find((c) => c.id === editingId);
    try {
      await updateColumn.mutateAsync({
        projectId,
        boardId: board.id,
        columnId: editingId,
        name: editState.name.trim(),
        wipLimit: editState.wipLimit ? parseInt(editState.wipLimit, 10) : undefined,
        mapsToTaskStateIds: editState.taskStateIds,
        color: editState.color,
        isVisible: originalColumn?.isVisible ?? true,
      });
      toast.success(t("columnUpdated"));
      setEditingId(null);
    } catch {
      toast.error(t("columnUpdateFailed"));
    }
  };

  const handleDeleteColumn = async (columnId: string) => {
    if (!window.confirm(t("columnDeleteConfirm"))) return;
    try {
      await deleteColumn.mutateAsync({ projectId, boardId: board.id, columnId });
      toast.success(t("columnDeleted"));
    } catch {
      toast.error(t("columnDeleteFailed"));
    }
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIndex = columns.findIndex((c) => c.id === active.id);
    const newIndex = columns.findIndex((c) => c.id === over.id);
    if (oldIndex === -1 || newIndex === -1) return;
    const ids = columns.map((c) => c.id);
    ids.splice(oldIndex, 1);
    ids.splice(newIndex, 0, active.id as string);
    try {
      await reorderColumns.mutateAsync({ projectId, boardId: board.id, columnIds: ids });
    } catch {
      toast.error(t("reorderFailed"));
    }
  };

  const handleToggleVisibility = async (col: (typeof columns)[0]) => {
    try {
      await updateColumn.mutateAsync({
        projectId,
        boardId: board.id,
        columnId: col.id,
        name: col.name,
        wipLimit: col.wipLimit,
        mapsToTaskStateIds: col.taskStates.map((ts) => ts.id),
        color: col.color,
        isVisible: !col.isVisible,
      });
      toast.success(col.isVisible ? t("columnHidden") : t("columnShown"));
    } catch (err) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        t("columnHidden_failed");
      toast.error(message);
    }
  };

  const stateOwners = columns.reduce<Record<string, string>>((acc, c) => {
    c.taskStates.forEach((ts) => {
      acc[ts.id] = c.name;
    });
    return acc;
  }, {});

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <LayoutGrid className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">{t("boardConfig")}</h2>
      </div>

      {columns.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t("noColumns")}</p>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={columns.map((c) => c.id)} strategy={verticalListSortingStrategy}>
            <div className="space-y-1">
              {columns.map((col) => {
                const ticketCount = col.tickets?.length ?? 0;
                const assignmentsForThisRow = Object.fromEntries(
                  Object.entries(stateOwners).filter(([, owner]) => owner !== col.name)
                );
                return (
                  <SortableColumnRow
                    key={col.id}
                    col={{
                      id: col.id,
                      name: col.name,
                      wipLimit: col.wipLimit,
                      color: col.color,
                      isVisible: col.isVisible,
                      ticketCount,
                      taskStates: col.taskStates,
                    }}
                    isEditing={editingId === col.id}
                    onStartEdit={() => startEditing(col)}
                    onDelete={() => handleDeleteColumn(col.id)}
                    onToggleVisibility={() => handleToggleVisibility(col)}
                    editState={editState}
                    onEditChange={(patch) => setEditState((prev) => ({ ...prev, ...patch }))}
                    onSaveEdit={handleSaveEdit}
                    onCancelEdit={() => setEditingId(null)}
                    updatePending={updateColumn.isPending}
                    deletePending={deleteColumn.isPending}
                    activeTaskStates={activeTaskStates}
                    stateAssignments={assignmentsForThisRow}
                  />
                );
              })}
            </div>
          </SortableContext>
        </DndContext>
      )}

      <div className="pt-2 border-t border-border space-y-3">
        <p className="text-sm font-medium text-card-foreground">{t("addColumn")}</p>
        <div className="flex items-end gap-3">
          <div className="flex-1">
            <label className="block text-xs text-muted-foreground mb-1">{t("name")}</label>
            <input
              type="text"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder={t("columnName")}
              className={`w-full ${inputClass}`}
            />
          </div>
          <div>
            <label className="block text-xs text-muted-foreground mb-1">{t("wipLimit")}</label>
            <input
              type="number"
              value={newWipLimit}
              onChange={(e) => setNewWipLimit(e.target.value)}
              placeholder={t("wipLimitOptional")}
              min={1}
              className={`w-24 ${inputClass}`}
            />
          </div>
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">{t("taskStates")}</label>
          <TaskStateMultiSelect
            selected={newTaskStateIds}
            onChange={setNewTaskStateIds}
            taskStates={activeTaskStates}
            stateAssignments={stateOwners}
          />
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">{t("color")}</label>
          <ColorPicker value={newColor} onChange={setNewColor} />
        </div>
        <button
          onClick={handleAddColumn}
          disabled={!newName.trim() || newTaskStateIds.length === 0 || createColumn.isPending}
          className={btnPrimary}
        >
          {createColumn.isPending ? t("adding") : t("addColumn")}
        </button>
      </div>
    </section>
  );
}
