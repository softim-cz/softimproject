"use client";

import { useState } from "react";
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
import { useAdminUsers, useUpdateUserRoles } from "@/queries/admin";
import { useApplicationRoles } from "@/queries/lookups";

const roleColors: Record<GlobalRole, string> = {
  [GlobalRole.Admin]: "bg-red-100 text-red-700",
  [GlobalRole.Manager]: "bg-blue-100 text-blue-700",
  [GlobalRole.User]: "bg-gray-100 text-gray-600",
};

function UserManagement() {
  const { data: users, isLoading, error } = useAdminUsers();
  const { data: appRoles } = useApplicationRoles();
  const updateRoles = useUpdateUserRoles();
  const [expandedUser, setExpandedUser] = useState<string | null>(null);

  if (isLoading) return <TableSkeleton rows={6} />;

  if (error) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        Failed to load users.
      </div>
    );
  }

  if (!users || users.length === 0) {
    return (
      <EmptyState
        icon={<Users className="h-10 w-10" />}
        title="No users found"
      />
    );
  }

  const toggleRole = (user: AdminUser, roleId: string) => {
    const newIds = user.applicationRoleIds.includes(roleId)
      ? user.applicationRoleIds.filter((id) => id !== roleId)
      : [...user.applicationRoleIds, roleId];
    updateRoles.mutate({ userId: user.id, applicationRoleIds: newIds });
  };

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
                <td className="px-4 py-3 text-sm text-muted-foreground">
                  {user.email}
                </td>
                <td className="px-4 py-3 text-sm text-muted-foreground">
                  {[user.companyName, user.corporateRole].filter(Boolean).join(" / ") || "—"}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={cn(
                      "px-2 py-0.5 rounded-full text-xs font-medium",
                      roleColors[user.globalRole]
                    )}
                  >
                    {user.globalRole}
                  </span>
                </td>
                <td className="px-4 py-3">
                  {user.isActive ? (
                    <span className="flex items-center gap-1 text-xs text-green-600">
                      <CheckCircle2 className="h-3.5 w-3.5" />
                      Active
                    </span>
                  ) : (
                    <span className="flex items-center gap-1 text-xs text-muted-foreground">
                      <XCircle className="h-3.5 w-3.5" />
                      Inactive
                    </span>
                  )}
                </td>
                <td className="px-4 py-3">
                  <button
                    onClick={() => setExpandedUser(expandedUser === user.id ? null : user.id)}
                    className="p-1 text-muted-foreground hover:text-foreground"
                  >
                    {expandedUser === user.id ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                  </button>
                </td>
              </tr>
              {expandedUser === user.id && appRoles && (
                <tr key={`${user.id}-roles`}>
                  <td colSpan={6} className="px-4 py-3 bg-muted/20">
                    <p className="text-xs font-medium text-muted-foreground mb-2">Application Roles</p>
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
                        <p className="text-xs text-muted-foreground">No application roles configured. Create them in Lookups.</p>
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
            <p className="text-sm font-medium text-card-foreground">
              SignalR
            </p>
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
            <p className="text-sm font-medium text-card-foreground">
              Database
            </p>
            <p className="text-xs text-green-600">Operational</p>
          </div>
        </div>
      </div>
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
              <p className="text-sm font-medium text-foreground">
                {integration.name}
              </p>
              <p className="text-xs text-muted-foreground">
                {integration.status}
              </p>
            </div>
          </div>
          {integration.connected ? (
            <span className="text-xs text-green-600 font-medium">
              Connected
            </span>
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
        <p className="text-sm text-muted-foreground mt-1">
          System management and configuration
        </p>
      </div>

      {/* System health */}
      <section>
        <div className="flex items-center gap-2 mb-4">
          <Shield className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-foreground">
            System Health
          </h2>
        </div>
        <SystemHealth />
      </section>

      {/* User management */}
      <section>
        <div className="flex items-center gap-2 mb-4">
          <Users className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-foreground">
            User Management
          </h2>
        </div>
        <UserManagement />
      </section>

      {/* Integration status */}
      <section>
        <div className="flex items-center gap-2 mb-4">
          <Link2 className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold text-foreground">
            Integrations
          </h2>
        </div>
        <IntegrationStatus />
      </section>
    </div>
  );
}
