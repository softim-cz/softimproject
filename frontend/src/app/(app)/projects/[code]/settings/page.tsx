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
import { ProjectStatus, ProjectRole } from "@/types";
import type { ProjectCustomFieldValue, ProjectMember } from "@/types";
import { useSearchParams, useRouter } from "next/navigation";

export default function ProjectSettingsPage({ params }: { params: Promise<{ code: string }> }) {
  const { code } = use(params);
  const router = useRouter();
  const { data: project, isLoading, error } = useProjectByCode(code);
  const projectId = project?.id ?? "";
  const updateProject = useUpdateProject();
  const deleteProject = useDeleteProject();
  const [editName, setEditName] = useState("");
  const [editCode, setEditCode] = useState("");
  const [confirmDelete, setConfirmDelete] = useState("");
  const [generalDirty, setGeneralDirty] = useState(false);

  // This synchronizes local draft state with a newly loaded project.
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
        Failed to load project settings.
      </div>
    );
  }

  const handleStatusChange = async (status: ProjectStatus) => {
    try {
      await updateProject.mutateAsync({ id: projectId, status });
      toast.success("Project status updated");
    } catch {
      toast.error("Failed to update status");
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
      toast.success("Project settings saved");
      setGeneralDirty(false);
      if (codeChanged) {
        router.push(`/projects/${editCode}/settings`);
      }
    } catch {
      toast.error("Failed to save project settings");
    }
  };

  const handleClientAccessToggle = async () => {
    try {
      await updateProject.mutateAsync({
        id: projectId,
        clientAccessEnabled: !project.clientAccessEnabled,
      });
      toast.success(
        project.clientAccessEnabled
          ? "Client portal access disabled"
          : "Client portal access enabled"
      );
    } catch {
      toast.error("Failed to update client access");
    }
  };

  return (
    <div className="space-y-8 max-w-3xl">
      <p className="text-sm text-muted-foreground">Configure project settings and integrations</p>

      {/* General settings */}
      <section className="rounded-lg border border-border bg-card p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <Settings className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-card-foreground">General</h2>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">Name</label>
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
              <p className="text-xs text-destructive mt-1">Name is required</p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">Code</label>
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
              <p className="text-xs text-destructive mt-1">2-6 uppercase letters</p>
            )}
          </div>
        </div>

        <div className="flex justify-end pt-2">
          <button
            onClick={handleSaveGeneral}
            disabled={!generalDirty || !nameValid || !codeValid || updateProject.isPending}
            className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            {updateProject.isPending ? "Saving..." : "Save"}
          </button>
        </div>

        <div>
          <label className="block text-sm font-medium text-card-foreground mb-1">Status</label>
          <select
            value={project.status}
            onChange={(e) => handleStatusChange(e.target.value as ProjectStatus)}
            className="rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {Object.values(ProjectStatus).map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </div>

        {project.projectTemplateName && (
          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">Template</label>
            <p className="text-sm text-muted-foreground">{project.projectTemplateName}</p>
          </div>
        )}

        <div>
          <label className="flex items-center gap-2 text-sm cursor-pointer">
            <input
              type="checkbox"
              checked={project.clientAccessEnabled}
              onChange={handleClientAccessToggle}
              disabled={updateProject.isPending}
              className="rounded"
            />
            Client portal access enabled
          </label>
        </div>
      </section>

      {/* Custom Fields */}
      <CustomFieldsSection projectId={projectId} />

      {/* Members */}
      <MembersSection projectId={projectId} members={project.members ?? []} />

      {/* Board configuration */}
      <BoardConfigSection projectId={projectId} projectTemplateId={project.projectTemplateId} />

      {/* GitHub Integration */}
      <GitHubIntegrationSection projectId={projectId} project={project} />

      {/* Danger Zone */}
      <section className="rounded-lg border border-destructive/50 bg-destructive/5 p-6 space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <AlertTriangle className="h-5 w-5 text-destructive" />
          <h2 className="text-lg font-semibold text-destructive">Danger Zone</h2>
        </div>
        <p className="text-sm text-muted-foreground">
          Deleting a project is permanent. All tickets, worklogs, comments, and attachments will be
          removed. Type the project code{" "}
          <span className="font-mono font-semibold text-foreground">{project.code}</span> to
          confirm.
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
                toast.success("Project deleted");
                router.push("/projects");
              } catch {
                toast.error("Failed to delete project");
              }
            }}
            disabled={confirmDelete !== project.code || deleteProject.isPending}
            className="px-4 py-2 rounded-lg bg-destructive text-destructive-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            {deleteProject.isPending ? "Deleting..." : "Delete Project"}
          </button>
        </div>
      </section>
    </div>
  );
}

function CustomFieldsSection({ projectId }: { projectId: string }) {
  const { data: fields, isLoading } = useProjectCustomFieldValues(projectId);
  const saveMutation = useSaveProjectCustomFieldValues();
  const [values, setValues] = useState<Record<string, string>>({});
  const [dirty, setDirty] = useState(false);

  // This resets field drafts when the loaded field set changes.
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
      toast.success("Custom fields saved");
      setDirty(false);
    } catch {
      toast.error("Failed to save custom fields");
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
            <option value="">-- Select --</option>
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
        <h2 className="text-lg font-semibold text-card-foreground">Custom Fields</h2>
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
          {saveMutation.isPending ? "Saving..." : "Save Custom Fields"}
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

  // Handle callback from GitHub OAuth redirect
  useEffect(() => {
    const githubParam = searchParams.get("github");
    if (githubParam === "connected") {
      toast.success("GitHub account connected!");
      refetchStatus();
      // Clean the URL
      window.history.replaceState({}, "", window.location.pathname);
    } else if (githubParam === "error") {
      const message = searchParams.get("message") || "Failed to connect GitHub";
      toast.error(message);
      window.history.replaceState({}, "", window.location.pathname);
    }
  }, [searchParams, refetchStatus]);

  const handleConnect = async () => {
    try {
      const { url } = await authorize.mutateAsync(projectId);
      window.location.href = url;
    } catch {
      toast.error("Failed to start GitHub authorization");
    }
  };

  const handleDisconnect = async () => {
    try {
      await disconnect.mutateAsync();
      toast.success("GitHub disconnected");
    } catch {
      toast.error("Failed to disconnect GitHub");
    }
  };

  const handleLinkRepo = async () => {
    if (!selectedRepo) return;
    try {
      await linkRepo.mutateAsync({ projectId, repositoryFullName: selectedRepo });
      toast.success(`Linked to ${selectedRepo}`);
      setSelectedRepo("");
    } catch {
      toast.error("Failed to link repository");
    }
  };

  const handleUnlinkRepo = async () => {
    try {
      await unlinkRepo.mutateAsync(projectId);
      toast.success("Repository unlinked");
    } catch {
      toast.error("Failed to unlink repository");
    }
  };

  const handleTest = async () => {
    try {
      const result = await testConnection.mutateAsync(projectId);
      if (result.success) {
        toast.success(`Connected to ${result.repositoryName}`);
      } else {
        toast.error(result.error ?? "Connection failed");
      }
    } catch {
      toast.error("Connection test failed");
    }
  };

  const handleSync = async () => {
    try {
      const result = await triggerSync.mutateAsync(projectId);
      if (result.error) {
        toast.error(result.error);
      } else {
        toast.success(
          `Synced ${result.synced} items${result.failed > 0 ? `, ${result.failed} failed` : ""}`
        );
      }
    } catch {
      toast.error("Sync failed");
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
        <h2 className="text-lg font-semibold text-card-foreground">GitHub Integration</h2>
      </div>

      {/* State A: Not connected */}
      {!isConnected && (
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">
            Connect your GitHub account to sync issues and comments.
          </p>
          <button onClick={handleConnect} disabled={authorize.isPending} className={btnPrimary}>
            {authorize.isPending ? (
              <span className="inline-flex items-center gap-1.5">
                <Loader2 className="h-4 w-4 animate-spin" />
                Connecting...
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5">
                <LinkIcon className="h-4 w-4" />
                Connect to GitHub
              </span>
            )}
          </button>
        </div>
      )}

      {/* State B: Connected, no repo linked */}
      {isConnected && !hasLinkedRepo && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-sm text-foreground">
              Connected as <span className="font-semibold">@{ghStatus!.login}</span>
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
              Disconnect
            </button>
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              Repository
            </label>
            <select
              value={selectedRepo}
              onChange={(e) => setSelectedRepo(e.target.value)}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Select repository...</option>
              {reposLoading && <option disabled>Loading repositories...</option>}
              {repos?.map((r) => (
                <option key={r.fullName} value={r.fullName}>
                  {r.fullName}
                  {r.isPrivate ? " (private)" : ""}
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
                Linking...
              </span>
            ) : (
              "Link Repository"
            )}
          </button>
        </div>
      )}

      {/* State C: Connected + repo linked */}
      {isConnected && hasLinkedRepo && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-sm text-foreground">
              Connected as <span className="font-semibold">@{ghStatus!.login}</span>
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
              Disconnect
            </button>
          </div>

          <div className="flex items-center justify-between">
            <p className="text-sm text-foreground">
              Repository:{" "}
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
              Unlink
            </button>
          </div>

          <div className="flex items-center gap-2 pt-2">
            <button onClick={handleTest} disabled={testConnection.isPending} className={btnOutline}>
              {testConnection.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              Test Connection
            </button>
            <button onClick={handleSync} disabled={triggerSync.isPending} className={btnOutline}>
              {triggerSync.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              Sync Now
            </button>
          </div>
        </div>
      )}

      {/* Fallback: repo linked but user not connected (legacy PAT) */}
      {!isConnected && hasLinkedRepo && (
        <div className="space-y-3 mt-4 pt-4 border-t border-border">
          <p className="text-sm text-muted-foreground">
            Repository <span className="font-mono font-semibold">{project.externalProjectId}</span>{" "}
            is linked via a legacy API token. Connect your GitHub account for a better experience.
          </p>
          <div className="flex items-center gap-2">
            <button onClick={handleTest} disabled={testConnection.isPending} className={btnOutline}>
              {testConnection.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              Test Connection
            </button>
            <button onClick={handleSync} disabled={triggerSync.isPending} className={btnOutline}>
              {triggerSync.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              Sync Now
            </button>
          </div>
        </div>
      )}
    </section>
  );
}

function MembersSection({ projectId, members }: { projectId: string; members: ProjectMember[] }) {
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
      toast.success("Member added");
      setAddUserId("");
      setAddRole(ProjectRole.Developer);
    } catch {
      toast.error("Failed to add member");
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
      toast.success("Role updated");
    } catch {
      toast.error("Failed to update role");
    }
  };

  const handleRemove = async (member: ProjectMember) => {
    if (!window.confirm(`Remove ${member.displayName} from project?`)) return;
    try {
      await removeMember.mutateAsync({ projectId, memberId: member.id });
      toast.success("Member removed");
    } catch {
      toast.error("Failed to remove member");
    }
  };

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <Users className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">Members</h2>
      </div>

      {members.length === 0 ? (
        <p className="text-sm text-muted-foreground">No members yet. Add members below.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left">
                <th className="pb-2 font-medium text-muted-foreground">Member</th>
                <th className="pb-2 font-medium text-muted-foreground">Email</th>
                <th className="pb-2 font-medium text-muted-foreground">Role</th>
                <th className="pb-2 font-medium text-muted-foreground w-20"></th>
              </tr>
            </thead>
            <tbody>
              {members.map((member) => (
                <tr key={member.id} className="border-b border-border/50">
                  <td className="py-3">
                    <div className="flex items-center gap-2">
                      {member.avatarUrl ? (
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
        <p className="text-sm font-medium text-card-foreground mb-2">Add member</p>
        <div className="flex items-end gap-3">
          <div className="flex-1">
            <label className="block text-xs text-muted-foreground mb-1">User</label>
            <select
              value={addUserId}
              onChange={(e) => setAddUserId(e.target.value)}
              className={`w-full ${inputClass}`}
            >
              <option value="">Select user...</option>
              {availableUsers.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.displayName} ({u.email})
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-muted-foreground mb-1">Role</label>
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
            {addMember.isPending ? "Adding..." : "Add"}
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
          title={color ?? "None"}
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
}: {
  selected: string[];
  onChange: (ids: string[]) => void;
  taskStates: { id: string; name: string; color: string }[];
}) {
  const toggle = (id: string) => {
    onChange(selected.includes(id) ? selected.filter((s) => s !== id) : [...selected, id]);
  };

  return (
    <div className="space-y-1.5">
      <div className="flex flex-wrap gap-1 min-h-[24px]">
        {selected.map((id) => {
          const ts = taskStates.find((t) => t.id === id);
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
        {taskStates.map((ts) => (
          <label
            key={ts.id}
            className="flex items-center gap-2 px-3 py-1.5 text-sm cursor-pointer hover:bg-muted transition-colors"
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
            {ts.name}
          </label>
        ))}
      </div>
    </div>
  );
}

function SortableColumnRow({
  col,
  isEditing,
  onStartEdit,
  onDelete,
  editState,
  onEditChange,
  onSaveEdit,
  onCancelEdit,
  updatePending,
  deletePending,
  activeTaskStates,
}: {
  col: {
    id: string;
    name: string;
    wipLimit?: number;
    color?: string;
    taskStates: { id: string; name: string; color: string }[];
  };
  isEditing: boolean;
  onStartEdit: () => void;
  onDelete: () => void;
  editState: { name: string; taskStateIds: string[]; wipLimit: string; color: string | undefined };
  onEditChange: (patch: Partial<typeof editState>) => void;
  onSaveEdit: () => void;
  onCancelEdit: () => void;
  updatePending: boolean;
  deletePending: boolean;
  activeTaskStates: { id: string; name: string; color: string }[];
}) {
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
            placeholder="Column name"
          />
          <input
            type="number"
            value={editState.wipLimit}
            onChange={(e) => onEditChange({ wipLimit: e.target.value })}
            className={`w-20 ${inputClass}`}
            placeholder="WIP"
            min={1}
          />
          <button onClick={onSaveEdit} disabled={updatePending} className={btnOutline} title="Save">
            <Check className="h-4 w-4 text-green-600" />
          </button>
          <button onClick={onCancelEdit} className={btnOutline} title="Cancel">
            <X className="h-4 w-4" />
          </button>
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">Task States</label>
          <TaskStateMultiSelect
            selected={editState.taskStateIds}
            onChange={(ids) => onEditChange({ taskStateIds: ids })}
            taskStates={activeTaskStates}
          />
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">Color</label>
          <ColorPicker value={editState.color} onChange={(c) => onEditChange({ color: c })} />
        </div>
      </div>
    );
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="flex items-center gap-2 px-3 py-2 rounded-lg border border-border/50 bg-background"
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
      <span className="flex-1 text-sm font-medium text-foreground">{col.name}</span>
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
        WIP: {col.wipLimit ?? "\u2013"}
      </span>
      <button onClick={onStartEdit} className={btnOutline} title="Edit">
        <Pencil className="h-4 w-4" />
      </button>
      <button onClick={onDelete} disabled={deletePending} className={btnDestructive} title="Delete">
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
          <h2 className="text-lg font-semibold text-card-foreground">Board Configuration</h2>
        </div>
        <p className="text-sm text-muted-foreground">No board found for this project.</p>
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
      toast.success("Column added");
      setNewName("");
      setNewTaskStateIds([]);
      setNewWipLimit("");
      setNewColor(undefined);
    } catch {
      toast.error("Failed to add column");
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
    try {
      await updateColumn.mutateAsync({
        projectId,
        boardId: board.id,
        columnId: editingId,
        name: editState.name.trim(),
        wipLimit: editState.wipLimit ? parseInt(editState.wipLimit, 10) : undefined,
        mapsToTaskStateIds: editState.taskStateIds,
        color: editState.color,
      });
      toast.success("Column updated");
      setEditingId(null);
    } catch {
      toast.error("Failed to update column");
    }
  };

  const handleDeleteColumn = async (columnId: string) => {
    if (
      !window.confirm(
        "Delete this column? Tickets in this column will lose their column assignment but keep their task state."
      )
    )
      return;
    try {
      await deleteColumn.mutateAsync({ projectId, boardId: board.id, columnId });
      toast.success("Column deleted");
    } catch {
      toast.error("Failed to delete column");
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
      toast.error("Failed to reorder columns");
    }
  };

  return (
    <section className="rounded-lg border border-border bg-card p-6 space-y-4">
      <div className="flex items-center gap-2 mb-2">
        <LayoutGrid className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold text-card-foreground">Board Configuration</h2>
      </div>

      {columns.length === 0 ? (
        <p className="text-sm text-muted-foreground">No columns configured. Add columns below.</p>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={columns.map((c) => c.id)} strategy={verticalListSortingStrategy}>
            <div className="space-y-1">
              {columns.map((col) => (
                <SortableColumnRow
                  key={col.id}
                  col={col}
                  isEditing={editingId === col.id}
                  onStartEdit={() => startEditing(col)}
                  onDelete={() => handleDeleteColumn(col.id)}
                  editState={editState}
                  onEditChange={(patch) => setEditState((prev) => ({ ...prev, ...patch }))}
                  onSaveEdit={handleSaveEdit}
                  onCancelEdit={() => setEditingId(null)}
                  updatePending={updateColumn.isPending}
                  deletePending={deleteColumn.isPending}
                  activeTaskStates={activeTaskStates}
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>
      )}

      <div className="pt-2 border-t border-border space-y-3">
        <p className="text-sm font-medium text-card-foreground">Add column</p>
        <div className="flex items-end gap-3">
          <div className="flex-1">
            <label className="block text-xs text-muted-foreground mb-1">Name</label>
            <input
              type="text"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Column name"
              className={`w-full ${inputClass}`}
            />
          </div>
          <div>
            <label className="block text-xs text-muted-foreground mb-1">WIP Limit</label>
            <input
              type="number"
              value={newWipLimit}
              onChange={(e) => setNewWipLimit(e.target.value)}
              placeholder="Optional"
              min={1}
              className={`w-24 ${inputClass}`}
            />
          </div>
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">Task States</label>
          <TaskStateMultiSelect
            selected={newTaskStateIds}
            onChange={setNewTaskStateIds}
            taskStates={activeTaskStates}
          />
        </div>
        <div>
          <label className="block text-xs text-muted-foreground mb-1">Color</label>
          <ColorPicker value={newColor} onChange={setNewColor} />
        </div>
        <button
          onClick={handleAddColumn}
          disabled={!newName.trim() || newTaskStateIds.length === 0 || createColumn.isPending}
          className={btnPrimary}
        >
          {createColumn.isPending ? "Adding..." : "Add Column"}
        </button>
      </div>
    </section>
  );
}
