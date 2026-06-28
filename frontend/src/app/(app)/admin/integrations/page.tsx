"use client";

import { useState } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { format } from "date-fns";
import { toast } from "sonner";
import { RefreshCw, Trash2, Save, AlertTriangle } from "lucide-react";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { useCompanies } from "@/queries/lookups";
import {
  useIntegrationConnections,
  useUpdateIntegrationConnection,
  useTriggerIntegrationSync,
  useDeleteIntegrationConnection,
} from "@/queries/integrations";
import type { Company, IntegrationConnection } from "@/types";

const MODES = ["Manual", "FullThenIncremental", "IncrementalOnly"];
const POLICIES = ["SourceOwnedWins", "StrictSourceWins", "PreserveLocalEdits"];
const INTERVALS = [60, 360, 720, 1440];

const selectClass = "px-2 py-1.5 rounded border border-border bg-card text-sm";

function ConnectionCard({
  connection,
  companies,
}: {
  connection: IntegrationConnection;
  companies: Company[];
}) {
  const t = useTranslations("Integrations");
  const update = useUpdateIntegrationConnection();
  const sync = useTriggerIntegrationSync();
  const remove = useDeleteIntegrationConnection();

  const [mode, setMode] = useState(connection.mode);
  const [isEnabled, setIsEnabled] = useState(connection.isEnabled);
  const [intervalMinutes, setIntervalMinutes] = useState(connection.intervalMinutes);
  const [conflictPolicy, setConflictPolicy] = useState(connection.conflictPolicy);
  const [targetCompanyId, setTargetCompanyId] = useState(connection.targetCompanyId ?? "");

  const handleSave = async () => {
    try {
      await update.mutateAsync({
        id: connection.id,
        mode,
        isEnabled,
        intervalMinutes,
        conflictPolicy,
        targetCompanyId: targetCompanyId || null,
      });
      toast.success(t("saved"));
    } catch {
      toast.error(t("saveFailed"));
    }
  };

  const handleSync = async () => {
    try {
      await sync.mutateAsync(connection.id);
      toast.success(t("syncTriggered"));
    } catch {
      toast.error(t("syncFailed"));
    }
  };

  const handleDelete = async () => {
    if (!window.confirm(t("deleteConfirm", { name: connection.name }))) return;
    try {
      await remove.mutateAsync(connection.id);
      toast.success(t("deleted"));
    } catch {
      toast.error(t("deleteFailed"));
    }
  };

  return (
    <div className="rounded-lg border border-border bg-card p-4 space-y-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <h2 className="text-base font-semibold text-foreground truncate">{connection.name}</h2>
            <span className="text-xs rounded bg-muted px-2 py-0.5 text-muted-foreground">
              {connection.sourceSystem}
            </span>
          </div>
          <p className="text-xs text-muted-foreground truncate">{connection.baseUrl}</p>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <button
            onClick={handleSync}
            disabled={sync.isPending}
            className="inline-flex items-center gap-1.5 rounded-md border border-border px-2.5 py-1.5 text-sm hover:bg-muted disabled:opacity-50"
            title={t("syncNow")}
          >
            <RefreshCw className={`h-4 w-4 ${sync.isPending ? "animate-spin" : ""}`} />
            {t("syncNow")}
          </button>
          <button
            onClick={handleDelete}
            disabled={remove.isPending}
            className="inline-flex items-center justify-center rounded-md border border-border p-1.5 text-destructive hover:bg-destructive/10 disabled:opacity-50"
            title={t("delete")}
            aria-label={t("delete")}
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      </div>

      {!connection.hasToken && (
        <div className="flex items-center gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-xs text-warning">
          <AlertTriangle className="h-4 w-4 shrink-0" />
          {t("noToken")}
        </div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium text-muted-foreground">{t("mode")}</span>
          <select value={mode} onChange={(e) => setMode(e.target.value)} className={selectClass}>
            {MODES.map((m) => (
              <option key={m} value={m}>
                {t(`modes.${m}`)}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium text-muted-foreground">{t("interval")}</span>
          <select
            value={intervalMinutes}
            onChange={(e) => setIntervalMinutes(Number(e.target.value))}
            className={selectClass}
          >
            {INTERVALS.map((v) => (
              <option key={v} value={v}>
                {t(`intervals.${v}`)}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium text-muted-foreground">{t("conflictPolicy")}</span>
          <select
            value={conflictPolicy}
            onChange={(e) => setConflictPolicy(e.target.value)}
            className={selectClass}
          >
            {POLICIES.map((p) => (
              <option key={p} value={p}>
                {t(`policies.${p}`)}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-xs font-medium text-muted-foreground">{t("company")}</span>
          <select
            value={targetCompanyId}
            onChange={(e) => setTargetCompanyId(e.target.value)}
            className={selectClass}
          >
            <option value="">{t("companyNone")}</option>
            {companies
              .filter((c) => c.isActive)
              .map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
          </select>
        </label>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={isEnabled}
            onChange={(e) => setIsEnabled(e.target.checked)}
            className="rounded border-border"
          />
          <span className="text-sm text-foreground">{t("enabled")}</span>
        </label>

        <div className="flex items-center gap-4 text-xs text-muted-foreground">
          <span>{t("projectsCount", { count: connection.projectsCount })}</span>
          <span>
            {t("lastSync")}:{" "}
            {connection.lastSyncStartedAt
              ? format(new Date(connection.lastSyncStartedAt), "d. M. yyyy HH:mm")
              : t("never")}
          </span>
        </div>

        <button
          onClick={handleSave}
          disabled={update.isPending}
          className="inline-flex items-center gap-1.5 rounded-md bg-primary-navy px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-navy-dark disabled:opacity-50"
        >
          <Save className="h-4 w-4" />
          {t("save")}
        </button>
      </div>
    </div>
  );
}

export default function IntegrationsAdminPage() {
  const t = useTranslations("Integrations");
  const { data: connections, isLoading, error } = useIntegrationConnections();
  const { data: companies } = useCompanies();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">{t("title")}</h1>
        <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
      </div>

      {isLoading && <TableSkeleton />}
      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
          {t("loadFailed")}
        </div>
      )}

      {connections && connections.length === 0 && (
        <EmptyState
          title={t("emptyTitle")}
          description={t("emptyDescription")}
          action={
            <Link
              href="/admin/migration"
              className="text-sm font-medium text-accent-orange hover:underline"
            >
              {t("goToMigration")}
            </Link>
          }
        />
      )}

      {connections && connections.length > 0 && (
        <div className="space-y-4">
          {connections.map((c) => (
            <ConnectionCard key={c.id} connection={c} companies={companies ?? []} />
          ))}
        </div>
      )}
    </div>
  );
}
