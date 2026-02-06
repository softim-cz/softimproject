"use client";

import { useQuery } from "@tanstack/react-query";
import apiClient from "@/lib/api/client";
import { TableSkeleton, Skeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import {
  Users,
  Shield,
  Activity,
  Link2,
  CheckCircle2,
  XCircle,
} from "lucide-react";
import type { User } from "@/types";
import { GlobalRole } from "@/types";
import { cn } from "@/lib/utils";

function useUsers() {
  return useQuery({
    queryKey: ["admin", "users"],
    queryFn: async () => {
      const { data } = await apiClient.get<User[]>("/api/v1/admin/users");
      return data;
    },
  });
}

const roleColors: Record<GlobalRole, string> = {
  [GlobalRole.Admin]: "bg-red-100 text-red-700",
  [GlobalRole.Manager]: "bg-blue-100 text-blue-700",
  [GlobalRole.User]: "bg-gray-100 text-gray-600",
};

function UserManagement() {
  const { data: users, isLoading, error } = useUsers();

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
              Role
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Status
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {users.map((user: User) => (
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
                  <span className="text-sm font-medium text-foreground">
                    {user.displayName}
                  </span>
                </div>
              </td>
              <td className="px-4 py-3 text-sm text-muted-foreground">
                {user.email}
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
            </tr>
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
