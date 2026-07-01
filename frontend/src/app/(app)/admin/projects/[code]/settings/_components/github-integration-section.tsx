"use client";

import { useState, useEffect } from "react";
import { Github, Loader2, LinkIcon, Unlink, X } from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import {
  useTestGitHubConnection,
  useTriggerGitHubSync,
  useGitHubStatus,
  useGitHubRepos,
  useGitHubAuthorize,
  useLinkGitHubRepo,
  useUnlinkGitHubRepo,
  useDisconnectGitHub,
} from "@/queries/projects";
import { useSearchParams } from "next/navigation";

export function GitHubIntegrationSection({
  projectId,
  project,
}: {
  projectId: string;
  project: {
    externalSystem?: string;
    externalProjectId?: string;
    gitHubConnectedByUserId?: string;
    gitHubWebhookActive?: boolean;
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

          <p className="text-xs text-muted-foreground">
            {t("webhookLabel")}:{" "}
            {project.gitHubWebhookActive ? (
              <span className="text-green-600 font-medium">{t("webhookActive")}</span>
            ) : (
              <span className="text-muted-foreground">{t("webhookInactive")}</span>
            )}
          </p>

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
