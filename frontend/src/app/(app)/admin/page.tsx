"use client";

import React, { useState } from "react";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import {
  Users,
  Shield,
  Activity,
  Link2,
  CheckCircle2,
  XCircle,
  ChevronDown,
  ChevronUp,
} from "lucide-react";
import type { AdminUser, ApplicationRoleEntity } from "@/types";
import { GlobalRole } from "@/types";
import { cn } from "@/lib/utils";
import {
  useAdminUsers,
  useUpdateUserRoles,
  useUpdateUserGlobalRole,
  useUpdateUserActive,
  useDeadLetterEntries,
  useReplayDeadLetter,
  useDismissDeadLetter,
  type DeadLetterEntry,
} from "@/queries/admin";
import { useApplicationRoles } from "@/queries/lookups";
import { useCurrentUser } from "@/queries/auth";
import { useAdminAiUsage } from "@/queries/ai";
import { AlertTriangle, RotateCw, Trash2, Sparkles } from "lucide-react";

function UserManagement() {
  const { data: users, isLoading, error } = useAdminUsers();
  const { data: appRoles } = useApplicationRoles();
  const { data: currentUser } = useCurrentUser();
  const updateRoles = useUpdateUserRoles();
  const updateGlobalRole = useUpdateUserGlobalRole();
  const updateActive = useUpdateUserActive();
  const [expandedUser, setExpandedUser] = useState<string | null>(null);
  const [rowError, setRowError] = useState<{ userId: string; message: string } | null>(null);

  if (isLoading) return <TableSkeleton rows={6} />;

  if (error) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        Failed to load users.
      </div>
    );
  }

  if (!users || users.length === 0) {
    return <EmptyState icon={<Users className="h-10 w-10" />} title="No users found" />;
  }

  const toggleRole = (user: AdminUser, roleId: string) => {
    const newIds = user.applicationRoleIds.includes(roleId)
      ? user.applicationRoleIds.filter((id) => id !== roleId)
      : [...user.applicationRoleIds, roleId];
    updateRoles.mutate({ userId: user.id, applicationRoleIds: newIds });
  };

  const extractMessage = (err: unknown, fallback: string) => {
    if (err && typeof err === "object" && "response" in err) {
      const data = (err as { response?: { data?: { message?: string; errors?: string[] } } })
        .response?.data;
      return data?.errors?.[0] ?? data?.message ?? fallback;
    }
    return fallback;
  };

  const changeGlobalRole = (user: AdminUser, role: GlobalRole) => {
    setRowError(null);
    updateGlobalRole.mutate(
      { userId: user.id, globalRole: role },
      {
        onError: (err) =>
          setRowError({
            userId: user.id,
            message: extractMessage(err, "Změna role se nezdařila."),
          }),
      }
    );
  };

  const toggleActive = (user: AdminUser) => {
    setRowError(null);
    updateActive.mutate(
      { userId: user.id, isActive: !user.isActive },
      {
        onError: (err) =>
          setRowError({
            userId: user.id,
            message: extractMessage(err, "Změna stavu se nezdařila."),
          }),
      }
    );
  };

  const isSelf = (user: AdminUser) => currentUser?.id === user.id;

  return (
    <div className="rounded-lg border border-border overflow-hidden">
      <table className="w-full">
        <thead>
          <tr className="bg-muted/50">
            <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
              User
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Email
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Company / Role
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Global Role
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Status
            </th>
            <th className="px-4 py-3 w-10" />
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {users.map((user: AdminUser) => (
            <>
              <tr key={user.id} className="hover:bg-muted/30">
                <td className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <div className="h-8 w-8 rounded-full bg-primary-navy text-white flex items-center justify-center text-xs font-bold">
                      {user.displayName
                        .split(" ")
                        .map((n) => n[0])
                        .join("")
                        .slice(0, 2)}
                    </div>
                    <div>
                      <span className="text-sm font-medium text-foreground block">
                        {user.displayName}
                      </span>
                      {(user.firstName || user.lastName) && (
                        <span className="text-xs text-muted-foreground">
                          {[user.firstName, user.lastName].filter(Boolean).join(" ")}
                        </span>
                      )}
                    </div>
                  </div>
                </td>
                <td className="px-4 py-3 text-sm text-muted-foreground">{user.email}</td>
                <td className="px-4 py-3 text-sm text-muted-foreground">
                  {[user.companyName, user.corporateRole].filter(Boolean).join(" / ") || "—"}
                </td>
                <td className="px-4 py-3">
                  <select
                    value={user.globalRole}
                    onChange={(e) => changeGlobalRole(user, e.target.value as GlobalRole)}
                    disabled={isSelf(user) || updateGlobalRole.isPending}
                    title={isSelf(user) ? "Vlastní roli nelze měnit" : undefined}
                    className={cn(
                      "text-xs font-medium rounded-md border border-border bg-card px-2 py-1",
                      "focus:outline-none focus:ring-2 focus:ring-accent-orange",
                      "disabled:cursor-not-allowed disabled:opacity-60"
                    )}
                  >
                    <option value={GlobalRole.Admin}>Admin</option>
                    <option value={GlobalRole.User}>User</option>
                    {user.globalRole === GlobalRole.Manager && (
                      <option value={GlobalRole.Manager}>Manager (legacy)</option>
                    )}
                  </select>
                </td>
                <td className="px-4 py-3">
                  <button
                    type="button"
                    onClick={() => toggleActive(user)}
                    disabled={isSelf(user) || updateActive.isPending}
                    title={isSelf(user) ? "Vlastní účet nelze deaktivovat" : undefined}
                    className={cn(
                      "flex items-center gap-1 text-xs rounded-md px-2 py-1 border",
                      user.isActive
                        ? "text-green-700 border-green-200 bg-green-50 hover:bg-green-100"
                        : "text-muted-foreground border-border bg-muted/30 hover:bg-muted/50",
                      "disabled:cursor-not-allowed disabled:opacity-60"
                    )}
                  >
                    {user.isActive ? (
                      <>
                        <CheckCircle2 className="h-3.5 w-3.5" />
                        Active
                      </>
                    ) : (
                      <>
                        <XCircle className="h-3.5 w-3.5" />
                        Inactive
                      </>
                    )}
                  </button>
                </td>
                <td className="px-4 py-3">
                  <button
                    onClick={() => setExpandedUser(expandedUser === user.id ? null : user.id)}
                    className="p-1 text-muted-foreground hover:text-foreground"
                  >
                    {expandedUser === user.id ? (
                      <ChevronUp className="h-4 w-4" />
                    ) : (
                      <ChevronDown className="h-4 w-4" />
                    )}
                  </button>
                </td>
              </tr>
              {rowError?.userId === user.id && (
                <tr key={`${user.id}-err`}>
                  <td colSpan={6} className="px-4 py-2 bg-destructive/5 text-xs text-destructive">
                    {rowError.message}
                  </td>
                </tr>
              )}
              {expandedUser === user.id && appRoles && (
                <tr key={`${user.id}-roles`}>
                  <td colSpan={6} className="px-4 py-3 bg-muted/20">
                    <p className="text-xs font-medium text-muted-foreground mb-2">
                      Application Roles
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {appRoles.map((role: ApplicationRoleEntity) => (
                        <button
                          key={role.id}
                          onClick={() => toggleRole(user, role.id)}
                          className={cn(
                            "px-3 py-1 text-xs rounded-full border font-medium transition-colors",
                            user.applicationRoleIds.includes(role.id)
                              ? "bg-accent-orange text-white border-accent-orange"
                              : "bg-card text-muted-foreground border-border hover:border-accent-orange"
                          )}
                        >
                          {role.name}
                        </button>
                      ))}
                      {appRoles.length === 0 && (
                        <p className="text-xs text-muted-foreground">
                          No application roles configured. Create them in Lookups.
                        </p>
                      )}
                    </div>
                  </td>
                </tr>
              )}
            </>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SystemHealth() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div className="rounded-lg border border-border bg-card p-5">
        <div className="flex items-center gap-3 mb-3">
          <div className="h-10 w-10 rounded-lg bg-green-100 flex items-center justify-center">
            <Activity className="h-5 w-5 text-green-600" />
          </div>
          <div>
            <p className="text-sm font-medium text-card-foreground">API</p>
            <p className="text-xs text-green-600">Healthy</p>
          </div>
        </div>
      </div>
      <div className="rounded-lg border border-border bg-card p-5">
        <div className="flex items-center gap-3 mb-3">
          <div className="h-10 w-10 rounded-lg bg-green-100 flex items-center justify-center">
            <Activity className="h-5 w-5 text-green-600" />
          </div>
          <div>
            <p className="text-sm font-medium text-card-foreground">SignalR</p>
            <p className="text-xs text-green-600">Connected</p>
          </div>
        </div>
      </div>
      <div className="rounded-lg border border-border bg-card p-5">
        <div className="flex items-center gap-3 mb-3">
          <div className="h-10 w-10 rounded-lg bg-green-100 flex items-center justify-center">
            <Activity className="h-5 w-5 text-green-600" />
          </div>
          <div>
            <p className="text-sm font-medium text-card-foreground">Database</p>
            <p className="text-xs text-green-600">Operational</p>
          </div>
        </div>
      </div>
    </div>
  );
}

function DeadLetterQueue() {
  const [includeResolved, setIncludeResolved] = useState(false);
  const { data: entries, isLoading, error } = useDeadLetterEntries(includeResolved);
  const replay = useReplayDeadLetter();
  const dismiss = useDismissDeadLetter();
  const [rowError, setRowError] = useState<{ id: string; message: string } | null>(null);

  if (isLoading) return <TableSkeleton rows={4} />;
  if (error)
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        Failed to load dead-letter queue.
      </div>
    );
  if (!entries || entries.length === 0)
    return (
      <EmptyState
        icon={<AlertTriangle className="h-10 w-10" />}
        title={includeResolved ? "No dead-letter entries" : "No pending failures"}
      />
    );

  const extractMessage = (err: unknown, fallback: string) => {
    if (err && typeof err === "object" && "response" in err) {
      const data = (err as { response?: { data?: { message?: string; errors?: string[] } } })
        .response?.data;
      return data?.errors?.[0] ?? data?.message ?? fallback;
    }
    return fallback;
  };

  const handleReplay = (entry: DeadLetterEntry) => {
    setRowError(null);
    replay.mutate(entry.id, {
      onError: (err) =>
        setRowError({ id: entry.id, message: extractMessage(err, "Replay selhal.") }),
    });
  };

  const handleDismiss = (entry: DeadLetterEntry) => {
    if (!window.confirm("Dismiss this dead-letter entry?")) return;
    setRowError(null);
    dismiss.mutate(entry.id, {
      onError: (err) =>
        setRowError({ id: entry.id, message: extractMessage(err, "Dismiss selhal.") }),
    });
  };

  return (
    <div className="space-y-3">
      <label className="inline-flex items-center gap-2 text-xs text-muted-foreground">
        <input
          type="checkbox"
          checked={includeResolved}
          onChange={(e) => setIncludeResolved(e.target.checked)}
        />
        Include resolved / dismissed
      </label>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Operation
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Key
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Attempts
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Last failed
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Status
              </th>
              <th className="px-4 py-3 w-40" />
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {entries.map((entry) => (
              <React.Fragment key={entry.id}>
                <tr className="hover:bg-muted/30">
                  <td className="px-4 py-3 text-sm font-medium text-foreground">
                    {entry.operationType}
                  </td>
                  <td
                    className="px-4 py-3 text-xs text-muted-foreground font-mono truncate max-w-xs"
                    title={entry.operationKey}
                  >
                    {entry.operationKey}
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">{entry.attemptCount}</td>
                  <td className="px-4 py-3 text-xs text-muted-foreground" title={entry.lastError}>
                    {new Date(entry.lastFailedAt).toLocaleString()}
                  </td>
                  <td className="px-4 py-3 text-xs">
                    <span
                      className={cn(
                        "px-2 py-0.5 rounded-full font-medium",
                        entry.status === "Pending"
                          ? "bg-red-100 text-red-700"
                          : entry.status === "Replayed"
                            ? "bg-green-100 text-green-700"
                            : "bg-gray-100 text-gray-600"
                      )}
                    >
                      {entry.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    {entry.status === "Pending" && (
                      <div className="flex items-center justify-end gap-1">
                        <button
                          type="button"
                          onClick={() => handleReplay(entry)}
                          disabled={replay.isPending}
                          className="inline-flex items-center gap-1 px-2 py-1 rounded border border-border text-xs hover:bg-muted disabled:opacity-50"
                          title="Replay"
                        >
                          <RotateCw className="h-3.5 w-3.5" /> Replay
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDismiss(entry)}
                          disabled={dismiss.isPending}
                          className="inline-flex items-center gap-1 px-2 py-1 rounded border border-border text-xs hover:bg-destructive/10 text-destructive disabled:opacity-50"
                          title="Dismiss"
                        >
                          <Trash2 className="h-3.5 w-3.5" /> Dismiss
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
                {rowError?.id === entry.id && (
                  <tr>
                    <td colSpan={6} className="px-4 py-2 bg-destructive/5 text-xs text-destructive">
                      {rowError.message}
                    </td>
                  </tr>
                )}
              </React.Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function AiUsage() {
  const [days, setDays] = useState(30);
  const { data, isLoading, error } = useAdminAiUsage(days);

  if (isLoading) return <TableSkeleton rows={3} />;
  if (error)
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        Failed to load AI usage.
      </div>
    );
  if (!data) return null;

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-3">
        <label className="text-xs text-muted-foreground">Window:</label>
        <select
          value={days}
          onChange={(e) => setDays(parseInt(e.target.value, 10))}
          className="text-xs rounded-md border border-border bg-card px-2 py-1"
        >
          <option value={7}>7 days</option>
          <option value={30}>30 days</option>
          <option value={90}>90 days</option>
        </select>
      </div>
      <div className="grid grid-cols-3 gap-3">
        <div className="rounded-lg border border-border bg-card p-4">
          <p className="text-xs text-muted-foreground">Invocations</p>
          <p className="text-xl font-semibold text-foreground">{data.totalInvocations}</p>
        </div>
        <div className="rounded-lg border border-border bg-card p-4">
          <p className="text-xs text-muted-foreground">Total tokens</p>
          <p className="text-xl font-semibold text-foreground">
            {data.totalTokens.toLocaleString()}
          </p>
        </div>
        <div className="rounded-lg border border-border bg-card p-4">
          <p className="text-xs text-muted-foreground">Estimated cost</p>
          <p className="text-xl font-semibold text-foreground">${data.totalCostUsd.toFixed(4)}</p>
        </div>
      </div>
      {data.byProject.length > 0 && (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                  Project
                </th>
                <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase">
                  Calls
                </th>
                <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase">
                  Tokens
                </th>
                <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase">
                  Cost
                </th>
                <th className="px-3 py-2 text-right text-xs font-medium text-muted-foreground uppercase">
                  Failures
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data.byProject.map((row) => (
                <tr key={row.projectId ?? "unknown"} className="hover:bg-muted/30">
                  <td className="px-3 py-2 text-foreground">
                    {row.projectName ?? (
                      <span className="text-muted-foreground italic">(no project)</span>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right">{row.invocationCount}</td>
                  <td className="px-3 py-2 text-right">{row.totalTokens.toLocaleString()}</td>
                  <td className="px-3 py-2 text-right">${row.totalCostUsd.toFixed(4)}</td>
                  <td className="px-3 py-2 text-right">
                    {row.failureCount > 0 ? (
                      <span className="text-destructive">{row.failureCount}</span>
                    ) : (
                      <span className="text-muted-foreground">0</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function IntegrationStatus() {
  const integrations = [
    { name: "Jira", status: "Not configured", connected: false },
    { name: "Redmine", status: "Not configured", connected: false },
    { name: "Azure DevOps", status: "Not configured", connected: false },
  ];

  return (
    <div className="space-y-3">
      {integrations.map((integration) => (
        <div
          key={integration.name}
          className="flex items-center justify-between p-4 rounded-lg border border-border"
        >
          <div className="flex items-center gap-3">
            <Link2 className="h-5 w-5 text-muted-foreground" />
            <div>
              <p className="text-sm font-medium text-foreground">{integration.name}</p>
              <p className="text-xs text-muted-foreground">{integration.status}</p>
            </div>
          </div>
          {integration.connected ? (
            <span className="text-xs text-green-600 font-medium">Connected</span>
          ) : (
            <button className="text-xs text-accent-orange hover:underline font-medium">
              Configure
            </button>
          )}
        </div>
      ))}
    </div>
  );
}

export default function AdminPage() {
  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Administration</h1>
        <p className="text-sm text-muted-foreground mt-1">System management and configuration</p>
      </div>

      {/* System health */}
      <section>
        <div className="flex items-center gap-2 mb-4">
          <Shield className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-foreground">System Health</h2>
        </div>
        <SystemHealth />
      </section>

      {/* User management */}
      <section>
        <div className="flex items-center gap-2 mb-4">
          <Users className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-foreground">User Management</h2>
        </div>
        <UserManagement />
      </section>

      {/* Dead-letter queue */}
      <section>
        <div className="flex items-center gap-2 mb-4">
          <AlertTriangle className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-foreground">Dead-letter queue</h2>
        </div>
        <DeadLetterQueue />
      </section>

      {/* AI usage */}
      <section>
        <div className="flex items-center gap-2 mb-4">
          <Sparkles className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-foreground">AI usage</h2>
        </div>
        <AiUsage />
      </section>

      {/* Integration status */}
      <section>
        <div className="flex items-center gap-2 mb-4">
          <Link2 className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-foreground">Integrations</h2>
        </div>
        <IntegrationStatus />
      </section>
    </div>
  );
}
