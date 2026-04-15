"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  ArrowRight,
  CheckCircle2,
  XCircle,
  Loader2,
  AlertTriangle,
  Download,
  RefreshCw,
  Search,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useMigrationStore } from "@/stores/migration-store";
import {
  useTestEpConnection,
  useFetchEpProjects,
  useFetchIssueCounts,
  useFetchEpLookups,
  useFetchEpUsers,
  useStartMigration,
  useCancelMigration,
  useMigrationProgress,
  useMigrationHistory,
} from "@/queries/migration";
import { useTaskTypes, useTaskStates, useTicketPriorities } from "@/queries/lookups";
import type { MigrationJob } from "@/types";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { useAuth } from "@/lib/auth/use-auth";
import type { MigrationProgress } from "@/types";

const STEPS = [
  "Connection",
  "Projects",
  "Lookups",
  "Users",
  "Review",
  "Progress",
];

function StepIndicator({ current }: { current: number }) {
  return (
    <div className="flex items-center gap-2 mb-8">
      {STEPS.map((name, i) => (
        <div key={name} className="flex items-center gap-2">
          <div
            className={cn(
              "flex items-center justify-center h-8 w-8 rounded-full text-xs font-bold transition-colors",
              i < current
                ? "bg-green-500 text-white"
                : i === current
                  ? "bg-accent-orange text-white"
                  : "bg-muted text-muted-foreground"
            )}
          >
            {i < current ? (
              <CheckCircle2 className="h-4 w-4" />
            ) : (
              i + 1
            )}
          </div>
          <span
            className={cn(
              "text-sm font-medium hidden sm:inline",
              i === current ? "text-foreground" : "text-muted-foreground"
            )}
          >
            {name}
          </span>
          {i < STEPS.length - 1 && (
            <div className="w-8 h-px bg-border hidden sm:block" />
          )}
        </div>
      ))}
    </div>
  );
}

function StepConnection() {
  const { baseUrl, apiKey, connectionTested, setBaseUrl, setApiKey, setConnectionTested, setStep } =
    useMigrationStore();
  const testConnection = useTestEpConnection();

  const handleTest = () => {
    testConnection.mutate(
      { baseUrl, apiKey },
      {
        onSuccess: (data) => {
          setConnectionTested(data.success);
        },
      }
    );
  };

  return (
    <div className="max-w-lg space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">
          Connect to EasyProject
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Enter your EasyProject instance URL and API key to begin.
        </p>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-foreground mb-1">
            EasyProject URL
          </label>
          <input
            type="url"
            value={baseUrl}
            onChange={(e) => setBaseUrl(e.target.value)}
            placeholder="https://your-company.easyproject.com"
            className="w-full px-3 py-2 rounded-lg border border-border bg-card text-sm focus:outline-none focus:ring-2 focus:ring-accent-orange"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-foreground mb-1">
            API Key
          </label>
          <input
            type="password"
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder="Your EasyProject API key"
            className="w-full px-3 py-2 rounded-lg border border-border bg-card text-sm focus:outline-none focus:ring-2 focus:ring-accent-orange"
          />
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={handleTest}
            disabled={!baseUrl || !apiKey || testConnection.isPending}
            className="px-4 py-2 bg-accent-orange text-white rounded-lg text-sm font-medium hover:bg-accent-orange/90 disabled:opacity-50 flex items-center gap-2"
          >
            {testConnection.isPending && (
              <Loader2 className="h-4 w-4 animate-spin" />
            )}
            Test Connection
          </button>

          {connectionTested && (
            <span className="flex items-center gap-1 text-sm text-green-600">
              <CheckCircle2 className="h-4 w-4" />
              Connected
            </span>
          )}

          {testConnection.isError && (
            <span className="flex items-center gap-1 text-sm text-destructive">
              <XCircle className="h-4 w-4" />
              {testConnection.error?.message || "Connection failed"}
            </span>
          )}

          {testConnection.data && !testConnection.data.success && (
            <span className="flex items-center gap-1 text-sm text-destructive">
              <XCircle className="h-4 w-4" />
              {testConnection.data.error || "Connection failed"}
            </span>
          )}
        </div>
      </div>

      <div className="flex justify-end">
        <button
          onClick={() => setStep(1)}
          disabled={!connectionTested}
          className="px-4 py-2 bg-accent-orange text-white rounded-lg text-sm font-medium hover:bg-accent-orange/90 disabled:opacity-50 flex items-center gap-2"
        >
          Next
          <ArrowRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

function StepProjects() {
  const {
    baseUrl,
    apiKey,
    projects,
    selectedProjectIds,
    setProjects,
    updateProjectIssueCount,
    toggleProject,
    selectAllProjects,
    selectNoProjects,
    setStep,
  } = useMigrationStore();
  const fetchProjects = useFetchEpProjects();
  const fetchIssueCounts = useFetchIssueCounts();
  const { getAccessToken } = useAuth();
  const connectionRef = useRef<ReturnType<typeof HubConnectionBuilder.prototype.build> | null>(null);
  const sessionIdRef = useRef<string>(crypto.randomUUID());
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    if (projects.length > 0) return;

    fetchProjects.mutate(
      { baseUrl, apiKey },
      {
        onSuccess: (data) => {
          setProjects(data);

          const sessionId = sessionIdRef.current;
          const baseSignalRUrl =
            process.env.NEXT_PUBLIC_SIGNALR_URL || "http://localhost:5249/hubs";

          const connection = new HubConnectionBuilder()
            .withUrl(`${baseSignalRUrl}/migration`, {
              accessTokenFactory: async () => (await getAccessToken()) || "",
            })
            .withAutomaticReconnect()
            .build();

          connection.on("ProjectIssueCount", (event: { epId: number; issueCount: number }) => {
            updateProjectIssueCount(event.epId, event.issueCount);
          });

          connection
            .start()
            .then(() => connection.invoke("JoinFetchSession", sessionId))
            .then(() => {
              fetchIssueCounts.mutate({
                baseUrl,
                apiKey,
                sessionId,
                projectIds: data.map((p) => p.epId),
              });
            })
            .catch((err) => {
              console.error("Issue count SignalR failed:", err);
            });

          connectionRef.current = connection;
        },
      }
    );

    return () => {
      connectionRef.current?.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (fetchProjects.isPending) {
    return (
      <div className="flex flex-col items-center gap-4 py-12">
        <Loader2 className="h-6 w-6 animate-spin text-accent-orange" />
        <p className="text-sm text-muted-foreground">
          Fetching project list...
        </p>
      </div>
    );
  }

  const filteredProjects = searchQuery
    ? projects.filter((p) =>
        p.name.toLowerCase().includes(searchQuery.toLowerCase())
      )
    : projects;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">
          Select Projects to Import
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Choose which EasyProject projects to migrate. Already imported
          projects can be re-imported for incremental updates.
        </p>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search projects..."
          className="w-full pl-9 pr-3 py-2 rounded-lg border border-border bg-card text-sm focus:outline-none focus:ring-2 focus:ring-accent-orange"
        />
      </div>

      <div className="flex gap-2">
        <button
          onClick={selectAllProjects}
          className="px-3 py-1.5 text-xs font-medium rounded-lg border border-border hover:bg-muted"
        >
          Select All
        </button>
        <button
          onClick={selectNoProjects}
          className="px-3 py-1.5 text-xs font-medium rounded-lg border border-border hover:bg-muted"
        >
          Select None
        </button>
      </div>

      <div className="space-y-2 max-h-[400px] overflow-y-auto">
        {filteredProjects.map((p) => (
          <label
            key={p.epId}
            className={cn(
              "flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors",
              selectedProjectIds.has(p.epId)
                ? "border-accent-orange bg-accent-orange/5"
                : "border-border hover:bg-muted/30"
            )}
          >
            <input
              type="checkbox"
              checked={selectedProjectIds.has(p.epId)}
              onChange={() => toggleProject(p.epId)}
              className="rounded border-border"
            />
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-foreground">
                  {p.name}
                </span>
                {p.alreadyImported && (
                  <span className="px-2 py-0.5 rounded-full text-[10px] font-medium bg-green-100 text-green-700">
                    Imported
                  </span>
                )}
                {p.status === 9 && (
                  <span className="px-2 py-0.5 rounded-full text-[10px] font-medium bg-gray-100 text-gray-500">
                    Archived
                  </span>
                )}
              </div>
              {p.parentName && (
                <span className="text-xs text-muted-foreground">
                  Parent: {p.parentName}
                </span>
              )}
            </div>
            <span className="text-xs text-muted-foreground whitespace-nowrap">
              {p.issueCount === -1 ? (
                <Loader2 className="h-3 w-3 animate-spin inline" />
              ) : (
                `${p.issueCount} issues`
              )}
            </span>
          </label>
        ))}
      </div>

      <div className="flex justify-between">
        <button
          onClick={() => setStep(0)}
          className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground flex items-center gap-2"
        >
          <ArrowLeft className="h-4 w-4" />
          Back
        </button>
        <button
          onClick={() => setStep(2)}
          disabled={selectedProjectIds.size === 0}
          className="px-4 py-2 bg-accent-orange text-white rounded-lg text-sm font-medium hover:bg-accent-orange/90 disabled:opacity-50 flex items-center gap-2"
        >
          Next
          <ArrowRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

function StepLookups() {
  const {
    baseUrl,
    apiKey,
    trackerMappings,
    statusMappings,
    priorityMappings,
    trackerSelections,
    statusSelections,
    prioritySelections,
    setLookups,
    setTrackerSelection,
    setStatusSelection,
    setPrioritySelection,
    setStep,
  } = useMigrationStore();
  const fetchLookups = useFetchEpLookups();
  const { data: taskTypes } = useTaskTypes();
  const { data: taskStates } = useTaskStates();
  const { data: ticketPriorities } = useTicketPriorities();

  useEffect(() => {
    if (trackerMappings.length === 0) {
      fetchLookups.mutate(
        { baseUrl, apiKey },
        {
          onSuccess: (data) =>
            setLookups(data.trackers, data.statuses, data.priorities),
        }
      );
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (fetchLookups.isPending) {
    return (
      <div className="flex items-center gap-3 text-muted-foreground py-12">
        <Loader2 className="h-5 w-5 animate-spin" />
        Loading lookups...
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">
          Map Lookups
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Map EasyProject trackers, statuses, and priorities to SoftimProject
          equivalents. Auto-suggested mappings are pre-filled.
        </p>
      </div>

      {/* Trackers */}
      <div>
        <h3 className="text-sm font-semibold text-foreground mb-2">
          Trackers → Task Types
        </h3>
        <div className="space-y-2">
          {trackerMappings.map((t) => (
            <div
              key={t.epId}
              className="flex items-center gap-3 p-2 rounded-lg border border-border"
            >
              <span className="text-sm font-medium w-40 shrink-0">
                {t.epName}
              </span>
              <ArrowRight className="h-4 w-4 text-muted-foreground shrink-0" />
              <select
                value={trackerSelections[t.epId] ?? ""}
                onChange={(e) =>
                  setTrackerSelection(t.epId, e.target.value || null)
                }
                className="flex-1 px-2 py-1 rounded border border-border bg-card text-sm"
              >
                <option value="">(Skip)</option>
                <option value="__create__">Auto-create: &quot;{t.epName}&quot;</option>
                {taskTypes?.filter((tt) => tt.isActive).map((tt) => (
                  <option key={tt.id} value={tt.id}>{tt.name}</option>
                ))}
              </select>
            </div>
          ))}
        </div>
      </div>

      {/* Statuses */}
      <div>
        <h3 className="text-sm font-semibold text-foreground mb-2">
          Statuses → Task State
        </h3>
        <div className="space-y-2">
          {statusMappings.map((s) => (
            <div
              key={s.epId}
              className="flex items-center gap-3 p-2 rounded-lg border border-border"
            >
              <span className="text-sm font-medium w-40 shrink-0">
                {s.epName}
                {s.isClosed && (
                  <span className="ml-1 text-[10px] text-muted-foreground">
                    (closed)
                  </span>
                )}
              </span>
              <ArrowRight className="h-4 w-4 text-muted-foreground shrink-0" />
              <select
                value={statusSelections[s.epId] ?? ""}
                onChange={(e) => setStatusSelection(s.epId, e.target.value)}
                className="flex-1 px-2 py-1 rounded border border-border bg-card text-sm"
              >
                <option value="">(Use default)</option>
                <option value="__create__">Auto-create: &quot;{s.epName}&quot;</option>
                {taskStates?.filter((ts) => ts.isActive).map((ts) => (
                  <option key={ts.id} value={ts.id}>{ts.name}</option>
                ))}
              </select>
            </div>
          ))}
        </div>
      </div>

      {/* Priorities */}
      <div>
        <h3 className="text-sm font-semibold text-foreground mb-2">
          Priorities → Ticket Priority
        </h3>
        <div className="space-y-2">
          {priorityMappings.map((p) => (
            <div
              key={p.epId}
              className="flex items-center gap-3 p-2 rounded-lg border border-border"
            >
              <span className="text-sm font-medium w-40 shrink-0">
                {p.epName}
              </span>
              <ArrowRight className="h-4 w-4 text-muted-foreground shrink-0" />
              <select
                value={prioritySelections[p.epId] ?? ""}
                onChange={(e) => setPrioritySelection(p.epId, e.target.value)}
                className="flex-1 px-2 py-1 rounded border border-border bg-card text-sm"
              >
                <option value="">(Use default)</option>
                <option value="__create__">Auto-create: &quot;{p.epName}&quot;</option>
                {ticketPriorities?.filter((tp) => tp.isActive).map((tp) => (
                  <option key={tp.id} value={tp.id}>{tp.name}</option>
                ))}
              </select>
            </div>
          ))}
        </div>
      </div>

      <div className="flex justify-between">
        <button
          onClick={() => setStep(1)}
          className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground flex items-center gap-2"
        >
          <ArrowLeft className="h-4 w-4" />
          Back
        </button>
        <button
          onClick={() => setStep(3)}
          className="px-4 py-2 bg-accent-orange text-white rounded-lg text-sm font-medium hover:bg-accent-orange/90 flex items-center gap-2"
        >
          Next
          <ArrowRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

function StepUsers() {
  const {
    baseUrl,
    apiKey,
    userMappings,
    userSelections,
    setUserMappings,
    setUserSelection,
    setStep,
  } = useMigrationStore();
  const fetchUsers = useFetchEpUsers();

  useEffect(() => {
    if (userMappings.length === 0) {
      fetchUsers.mutate(
        { baseUrl, apiKey },
        { onSuccess: (data) => setUserMappings(data) }
      );
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (fetchUsers.isPending) {
    return (
      <div className="flex items-center gap-3 text-muted-foreground py-12">
        <Loader2 className="h-5 w-5 animate-spin" />
        Loading users...
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">
          Map Users
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Map EasyProject users to existing SoftimProject users. Users matched
          by email are shown with a green check. Unmatched users will be created
          as inactive.
        </p>
      </div>

      <div className="space-y-2 max-h-[400px] overflow-y-auto">
        {userMappings.map((u) => (
          <div
            key={u.epId}
            className="flex items-center gap-3 p-3 rounded-lg border border-border"
          >
            <div className="flex-1 min-w-0">
              <span className="text-sm font-medium text-foreground block">
                {u.epName}
              </span>
              {u.epEmail && (
                <span className="text-xs text-muted-foreground">
                  {u.epEmail}
                </span>
              )}
            </div>
            <ArrowRight className="h-4 w-4 text-muted-foreground shrink-0" />
            <div className="flex items-center gap-2">
              {u.matchedUserId ? (
                <div className="flex items-center gap-1.5">
                  <CheckCircle2 className="h-4 w-4 text-green-500" />
                  <span className="text-sm text-foreground">
                    {u.matchedUserName}
                  </span>
                </div>
              ) : (
                <select
                  value={userSelections[u.epId] ?? ""}
                  onChange={(e) =>
                    setUserSelection(u.epId, e.target.value || null)
                  }
                  className="px-2 py-1 rounded border border-border bg-card text-sm min-w-[200px]"
                >
                  <option value="">Create as inactive</option>
                  <option value="skip">Skip (use admin)</option>
                </select>
              )}
            </div>
          </div>
        ))}
      </div>

      <div className="flex justify-between">
        <button
          onClick={() => setStep(2)}
          className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground flex items-center gap-2"
        >
          <ArrowLeft className="h-4 w-4" />
          Back
        </button>
        <button
          onClick={() => setStep(4)}
          className="px-4 py-2 bg-accent-orange text-white rounded-lg text-sm font-medium hover:bg-accent-orange/90 flex items-center gap-2"
        >
          Next
          <ArrowRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

function StepReview() {
  const store = useMigrationStore();
  const startMigration = useStartMigration();

  const selectedProjects = store.projects.filter((p) =>
    store.selectedProjectIds.has(p.epId)
  );
  const totalIssues = selectedProjects.reduce(
    (sum, p) => sum + Math.max(0, p.issueCount),
    0
  );

  const handleStart = () => {
    // Split tracker selections into mapping vs auto-create
    const trackerMapping: Record<number, string | null> = {};
    const autoCreateTrackers: Record<number, string> = {};
    for (const t of store.trackerMappings) {
      const sel = store.trackerSelections[t.epId];
      if (sel === "__create__") autoCreateTrackers[t.epId] = t.epName;
      else trackerMapping[t.epId] = sel ?? null;
    }

    // Split status selections
    const statusMapping: Record<number, string> = {};
    const autoCreateStatuses: Record<number, string> = {};
    const autoCreateStatusIsClosed: Record<number, boolean> = {};
    for (const s of store.statusMappings) {
      const sel = store.statusSelections[s.epId];
      if (sel === "__create__") {
        autoCreateStatuses[s.epId] = s.epName;
        autoCreateStatusIsClosed[s.epId] = s.isClosed;
      } else if (sel) {
        statusMapping[s.epId] = sel;
      }
    }

    // Split priority selections
    const priorityMapping: Record<number, string> = {};
    const autoCreatePriorities: Record<number, string> = {};
    for (const p of store.priorityMappings) {
      const sel = store.prioritySelections[p.epId];
      if (sel === "__create__") autoCreatePriorities[p.epId] = p.epName;
      else if (sel) priorityMapping[p.epId] = sel;
    }

    startMigration.mutate(
      {
        baseUrl: store.baseUrl,
        apiKey: store.apiKey,
        projectIds: [...store.selectedProjectIds],
        trackerMapping,
        statusMapping,
        priorityMapping,
        userMapping: store.userSelections,
        skipClosedIssues: store.skipClosedIssues,
        skipAttachments: store.skipAttachments,
        importComments: store.importComments,
        importWorklogs: store.importWorklogs,
        importChecklists: store.importChecklists,
        createMissingUsers: store.createMissingUsers,
        autoCreateTrackers,
        autoCreateStatuses,
        autoCreateStatusIsClosed,
        autoCreatePriorities,
      },
      {
        onSuccess: (jobId) => {
          store.setActiveJobId(jobId);
          store.setStep(5);
        },
      }
    );
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">
          Review & Start
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Review your migration settings before starting.
        </p>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="rounded-lg border border-border p-4 text-center">
          <p className="text-2xl font-bold text-foreground">
            {selectedProjects.length}
          </p>
          <p className="text-xs text-muted-foreground">Projects</p>
        </div>
        <div className="rounded-lg border border-border p-4 text-center">
          <p className="text-2xl font-bold text-foreground">{totalIssues}</p>
          <p className="text-xs text-muted-foreground">Issues (approx)</p>
        </div>
        <div className="rounded-lg border border-border p-4 text-center">
          <p className="text-2xl font-bold text-foreground">
            {store.trackerMappings.length}
          </p>
          <p className="text-xs text-muted-foreground">Tracker Mappings</p>
        </div>
        <div className="rounded-lg border border-border p-4 text-center">
          <p className="text-2xl font-bold text-foreground">
            {store.userMappings.filter((u) => u.matchedUserId).length}/
            {store.userMappings.length}
          </p>
          <p className="text-xs text-muted-foreground">Users Matched</p>
        </div>
      </div>

      {/* Options */}
      <div className="rounded-lg border border-border p-4 space-y-3">
        <h3 className="text-sm font-semibold text-foreground">Options</h3>
        {[
          { key: "importComments", label: "Import comments" },
          { key: "importWorklogs", label: "Import worklogs (time entries)" },
          { key: "importChecklists", label: "Import checklists" },
          { key: "createMissingUsers", label: "Create missing users as inactive" },
          { key: "skipClosedIssues", label: "Skip closed issues" },
          { key: "skipAttachments", label: "Skip attachments" },
        ].map(({ key, label }) => (
          <label key={key} className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={store[key as keyof typeof store] as boolean}
              onChange={(e) => store.setOption(key, e.target.checked)}
              className="rounded border-border"
            />
            <span className="text-sm text-foreground">{label}</span>
          </label>
        ))}
      </div>

      <div className="flex justify-between">
        <button
          onClick={() => store.setStep(3)}
          className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground flex items-center gap-2"
        >
          <ArrowLeft className="h-4 w-4" />
          Back
        </button>
        <button
          onClick={handleStart}
          disabled={startMigration.isPending}
          className="px-6 py-2 bg-green-600 text-white rounded-lg text-sm font-medium hover:bg-green-700 disabled:opacity-50 flex items-center gap-2"
        >
          {startMigration.isPending && (
            <Loader2 className="h-4 w-4 animate-spin" />
          )}
          <Download className="h-4 w-4" />
          Start Migration
        </button>
      </div>
    </div>
  );
}

function StepProgress() {
  const { activeJobId, reset, importComments, importWorklogs, skipAttachments } = useMigrationStore();
  const { data: progress } = useMigrationProgress(activeJobId);
  const cancelMigration = useCancelMigration();
  const { getAccessToken, isAuthenticated } = useAuth();
  const [liveProgress, setLiveProgress] = useState<MigrationProgress | null>(null);
  const connectionRef = useRef<ReturnType<typeof HubConnectionBuilder.prototype.build> | null>(null);
  const logEndRef = useRef<HTMLDivElement>(null);

  // SignalR connection for live updates
  useEffect(() => {
    if (!activeJobId || !isAuthenticated) return;

    const baseUrl =
      process.env.NEXT_PUBLIC_SIGNALR_URL || "http://localhost:5249/hubs";

    const connection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}/migration`, {
        accessTokenFactory: async () => (await getAccessToken()) || "",
      })
      .withAutomaticReconnect()
      .build();

    connection.on("MigrationProgress", (data: MigrationProgress) => {
      setLiveProgress(data);
    });

    connection
      .start()
      .then(() => connection.invoke("JoinMigrationJob", activeJobId))
      .catch((err) => console.error("Migration SignalR failed:", err));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [activeJobId, isAuthenticated, getAccessToken]);

  // Auto-scroll log
  useEffect(() => {
    logEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [liveProgress?.recentLog, progress?.recentLog]);

  const p = liveProgress ?? progress;

  const isFinished =
    p?.status === "Completed" ||
    p?.status === "CompletedWithErrors" ||
    p?.status === "Failed" ||
    p?.status === "Cancelled";

  // Phase order matches backend execution order
  const phaseOrder = ["projects", "tickets", "comments", "worklogs", "attachments"] as const;
  const phaseConfig: Record<string, { label: string; enabled: boolean; total: number; done: number }> = {
    projects: { label: "Projects", enabled: true, total: p?.projectsTotal ?? 0, done: p?.projectsMigrated ?? 0 },
    tickets: { label: "Tickets", enabled: true, total: p?.ticketsTotal ?? 0, done: p?.ticketsMigrated ?? 0 },
    comments: { label: "Comments", enabled: importComments, total: p?.commentsTotal ?? 0, done: p?.commentsMigrated ?? 0 },
    worklogs: { label: "Worklogs", enabled: importWorklogs, total: p?.worklogsTotal ?? 0, done: p?.worklogsMigrated ?? 0 },
    attachments: { label: "Attachments", enabled: !skipAttachments, total: p?.attachmentsTotal ?? 0, done: p?.attachmentsMigrated ?? 0 },
  };

  // Determine which phase is currently active based on currentPhase string
  const currentPhaseKey = p?.currentPhase?.toLowerCase().includes("project") ? "projects"
    : p?.currentPhase?.toLowerCase().includes("ticket") ? "tickets"
    : p?.currentPhase?.toLowerCase().includes("comment") ? "comments"
    : p?.currentPhase?.toLowerCase().includes("worklog") ? "worklogs"
    : p?.currentPhase?.toLowerCase().includes("attachment") ? "attachments"
    : null;

  const currentPhaseIndex = currentPhaseKey ? phaseOrder.indexOf(currentPhaseKey) : -1;

  const getPhaseStatus = (key: string, idx: number): "skipped" | "pending" | "active" | "completed" => {
    const cfg = phaseConfig[key];
    if (!cfg.enabled) return "skipped";
    if (isFinished) return cfg.total > 0 || (currentPhaseIndex >= idx) ? "completed" : "pending";
    if (currentPhaseIndex === idx) return "active";
    if (currentPhaseIndex > idx) return "completed";
    return "pending";
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">
          Migration {isFinished ? "Complete" : "in Progress"}
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          {p?.currentPhase ?? "Initializing..."}
        </p>
      </div>

      {/* Overall progress */}
      <div>
        <div className="flex justify-between text-sm mb-1">
          <span className="text-muted-foreground">Overall Progress</span>
          <span className="font-medium text-foreground">
            {p?.overallPercent ?? 0}%
          </span>
        </div>
        <div className="h-3 bg-muted rounded-full overflow-hidden">
          <div
            className={cn(
              "h-full rounded-full transition-all duration-500",
              isFinished && p?.status !== "Failed" && p?.status !== "Cancelled"
                ? "bg-green-500"
                : p?.status === "Failed"
                  ? "bg-destructive"
                  : "bg-accent-orange"
            )}
            style={{ width: `${p?.overallPercent ?? 0}%` }}
          />
        </div>
      </div>

      {/* Per-phase progress */}
      <div className="grid grid-cols-1 sm:grid-cols-5 gap-3">
        {phaseOrder.map((key, idx) => {
          const phase = phaseConfig[key];
          const status = getPhaseStatus(key, idx);
          const isCompleted = status === "completed";

          return (
            <div
              key={key}
              className={cn(
                "rounded-lg border p-3",
                status === "skipped"
                  ? "border-border/50 opacity-50"
                  : "border-border"
              )}
            >
              <p className="text-xs text-muted-foreground mb-1">
                {phase.label}
              </p>
              {status === "skipped" ? (
                <p className="text-sm text-muted-foreground italic">Skipped</p>
              ) : status === "pending" && phase.total === 0 ? (
                <p className="text-sm text-muted-foreground">Pending</p>
              ) : (
                <>
                  <p className="text-lg font-semibold text-foreground">
                    {phase.done}
                    <span className="text-muted-foreground font-normal">
                      /{phase.total}
                    </span>
                  </p>
                  {phase.total > 0 && (
                    <div className="h-1.5 bg-muted rounded-full mt-1">
                      <div
                        className={cn(
                          "h-full rounded-full transition-all",
                          isCompleted ? "bg-green-500" : "bg-accent-orange"
                        )}
                        style={{
                          width: `${Math.min(100, (phase.done / phase.total) * 100)}%`,
                        }}
                      />
                    </div>
                  )}
                </>
              )}
            </div>
          );
        })}
      </div>

      {/* Stats */}
      {isFinished && p && (
        <div className="grid grid-cols-4 gap-3">
          <div className="text-center p-3 rounded-lg bg-green-50 border border-green-200">
            <p className="text-lg font-bold text-green-700">{p.itemsCreated}</p>
            <p className="text-xs text-green-600">Created</p>
          </div>
          <div className="text-center p-3 rounded-lg bg-blue-50 border border-blue-200">
            <p className="text-lg font-bold text-blue-700">{p.itemsUpdated}</p>
            <p className="text-xs text-blue-600">Updated</p>
          </div>
          <div className="text-center p-3 rounded-lg bg-gray-50 border border-gray-200">
            <p className="text-lg font-bold text-gray-700">{p.itemsSkipped}</p>
            <p className="text-xs text-gray-600">Skipped</p>
          </div>
          <div className="text-center p-3 rounded-lg bg-red-50 border border-red-200">
            <p className="text-lg font-bold text-red-700">{p.errorCount}</p>
            <p className="text-xs text-red-600">Failed</p>
          </div>
        </div>
      )}

      {/* Log */}
      <div className="rounded-lg border border-border">
        <div className="px-3 py-2 border-b border-border bg-muted/30">
          <span className="text-xs font-medium text-muted-foreground">
            Log
          </span>
        </div>
        <div className="h-48 overflow-y-auto p-3 font-mono text-xs text-muted-foreground space-y-0.5">
          {(p?.recentLog ?? []).map((line, i) => (
            <div key={i}>{line}</div>
          ))}
          <div ref={logEndRef} />
        </div>
      </div>

      {/* Errors */}
      {(p?.errorCount ?? 0) > 0 && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4">
          <div className="flex items-center gap-2 mb-2">
            <AlertTriangle className="h-4 w-4 text-destructive" />
            <span className="text-sm font-medium text-destructive">
              {p?.errorCount} errors
            </span>
          </div>
          <div className="space-y-1 max-h-32 overflow-y-auto">
            {(p?.recentErrors ?? []).map((err, i) => (
              <p key={i} className="text-xs text-destructive/80">
                {err}
              </p>
            ))}
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-between">
        {isFinished ? (
          <>
            <button
              onClick={() => {
                reset();
              }}
              className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground flex items-center gap-2"
            >
              <RefreshCw className="h-4 w-4" />
              New Migration
            </button>
            <Link href="/projects"
              className="px-4 py-2 bg-accent-orange text-white rounded-lg text-sm font-medium hover:bg-accent-orange/90 flex items-center gap-2"
            >
              View Projects
              <ArrowRight className="h-4 w-4" />
            </Link>
          </>
        ) : (
          <button
            onClick={() => {
              if (activeJobId) cancelMigration.mutate(activeJobId);
            }}
            disabled={cancelMigration.isPending}
            className="px-4 py-2 bg-destructive text-destructive-foreground rounded-lg text-sm font-medium hover:bg-destructive/90 disabled:opacity-50"
          >
            Cancel Migration
          </button>
        )}
      </div>
    </div>
  );
}

function MigrationHistorySection() {
  const { data: history } = useMigrationHistory();

  if (!history || history.length === 0) return null;

  const statusColor = (status: string) => {
    switch (status) {
      case "Completed":
        return "text-green-600";
      case "CompletedWithErrors":
        return "text-yellow-600";
      case "Failed":
        return "text-destructive";
      case "Cancelled":
        return "text-muted-foreground";
      default:
        return "text-accent-orange";
    }
  };

  return (
    <div className="mt-8">
      <h3 className="text-sm font-semibold text-foreground mb-3">
        Migration History
      </h3>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-muted/50">
              <th className="px-4 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                Date
              </th>
              <th className="px-4 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                Source
              </th>
              <th className="px-4 py-2 text-left text-xs font-medium text-muted-foreground uppercase">
                Status
              </th>
              <th className="px-4 py-2 text-right text-xs font-medium text-muted-foreground uppercase">
                Projects
              </th>
              <th className="px-4 py-2 text-right text-xs font-medium text-muted-foreground uppercase">
                Tickets
              </th>
              <th className="px-4 py-2 text-right text-xs font-medium text-muted-foreground uppercase">
                Errors
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {history.map((job: MigrationJob) => (
              <tr key={job.id} className="hover:bg-muted/30">
                <td className="px-4 py-2 text-sm text-foreground">
                  {new Date(job.startedAt).toLocaleString()}
                </td>
                <td className="px-4 py-2 text-sm text-muted-foreground">
                  {job.sourceBaseUrl}
                </td>
                <td className="px-4 py-2">
                  <span
                    className={cn(
                      "text-xs font-medium",
                      statusColor(job.status)
                    )}
                  >
                    {job.status}
                  </span>
                </td>
                <td className="px-4 py-2 text-sm text-right text-foreground">
                  {job.projectsMigrated}
                </td>
                <td className="px-4 py-2 text-sm text-right text-foreground">
                  {job.ticketsMigrated}
                </td>
                <td className="px-4 py-2 text-sm text-right text-destructive">
                  {job.itemsFailed || "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default function MigrationPage() {
  const { step } = useMigrationStore();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">
          EasyProject Migration
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Import projects, tickets, worklogs, and more from EasyProject.
        </p>
      </div>

      <StepIndicator current={step} />

      <div className="rounded-lg border border-border bg-card p-6">
        {step === 0 && <StepConnection />}
        {step === 1 && <StepProjects />}
        {step === 2 && <StepLookups />}
        {step === 3 && <StepUsers />}
        {step === 4 && <StepReview />}
        {step === 5 && <StepProgress />}
      </div>

      {step < 5 && <MigrationHistorySection />}
    </div>
  );
}




