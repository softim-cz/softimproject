"use client";

import { use, useState, useMemo, useCallback, useRef, useEffect } from "react";
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getGroupedRowModel,
  getExpandedRowModel,
  flexRender,
  createColumnHelper,
  type SortingState,
  type VisibilityState,
  type ColumnSizingState,
  type GroupingState,
  type ExpandedState,
} from "@tanstack/react-table";
import { useTickets } from "@/queries/tickets";
import { useViewConfiguration, useUpsertViewConfiguration } from "@/queries/view-configurations";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { PriorityBadge } from "@/components/shared/priority-badge";
import { StatusBadge } from "@/components/shared/status-badge";
import { TaskPreviewSidebar } from "@/components/task-list/TaskPreviewSidebar";
import { FilterBar } from "@/components/filters/FilterBar";
import { useFilterStore, EMPTY_FILTERS, type FilterCondition } from "@/stores/filter-store";
import { exportXlsx } from "@/queries/export";
import { CreateTicketDialog } from "@/components/tickets/CreateTicketDialog";
import { useProjectByCode } from "@/queries/projects";
import {
  List,
  Settings2,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  Download,
  Plus,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  Layers,
} from "lucide-react";
import { toast } from "sonner";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils";
import type { Ticket } from "@/types";

const columnHelper = createColumnHelper<Ticket>();

function buildAllColumns(t: (key: string) => string) {
  return [
    columnHelper.accessor("key", {
      header: t("ticketCol"),
      size: 100,
      minSize: 80,
      cell: ({ row }) => (
        <span className="text-sm font-mono text-muted-foreground">{row.original.key}</span>
      ),
    }),
    columnHelper.accessor("title", {
      header: t("titleCol"),
      size: 300,
      minSize: 150,
      cell: ({ row, table }) => {
        const meta = table.options.meta as TableMeta | undefined;
        return (
          <button
            onClick={() => meta?.onRowClick?.(row.original)}
            className="text-left text-sm font-medium text-foreground hover:text-primary truncate block w-full"
          >
            {row.original.taskTypeIcon && <span className="mr-1">{row.original.taskTypeIcon}</span>}
            {row.original.title}
          </button>
        );
      },
    }),
    columnHelper.accessor("taskStateName", {
      header: t("statusCol"),
      size: 120,
      cell: ({ row }) => (
        <StatusBadge name={row.original.taskStateName} color={row.original.taskStateColor} />
      ),
    }),
    columnHelper.accessor("ticketPriorityName", {
      header: t("priorityCol"),
      size: 100,
      cell: ({ row }) => (
        <PriorityBadge
          name={row.original.ticketPriorityName}
          color={row.original.ticketPriorityColor}
        />
      ),
    }),
    columnHelper.accessor("assignee", {
      header: t("assigneeCol"),
      size: 150,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">
          {row.original.assignee?.displayName || t("unassigned")}
        </span>
      ),
      sortingFn: (rowA, rowB) => {
        const a = rowA.original.assignee?.displayName || "";
        const b = rowB.original.assignee?.displayName || "";
        return a.localeCompare(b);
      },
      getGroupingValue: (row) => row.assignee?.displayName ?? "—",
    }),
    columnHelper.accessor("taskTypeName", {
      header: t("typeCol"),
      size: 120,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">
          {row.original.taskTypeIcon && <span className="mr-1">{row.original.taskTypeIcon}</span>}
          {row.original.taskTypeName || "-"}
        </span>
      ),
    }),
    columnHelper.accessor("dueDate", {
      header: t("dueDateCol"),
      size: 110,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">
          {row.original.dueDate ? new Date(row.original.dueDate).toLocaleDateString() : "-"}
        </span>
      ),
    }),
    columnHelper.accessor("estimatedHours", {
      header: t("estimatedHoursCol"),
      size: 100,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">
          {row.original.estimatedHours != null ? `${row.original.estimatedHours}h` : "-"}
        </span>
      ),
    }),
    columnHelper.accessor("cumulativeWorkedHours", {
      header: t("workedCol"),
      size: 90,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">
          {row.original.cumulativeWorkedHours != null && row.original.cumulativeWorkedHours > 0
            ? `${row.original.cumulativeWorkedHours}h`
            : "-"}
        </span>
      ),
    }),
    columnHelper.accessor("commentsCount", {
      header: t("commentsCol"),
      size: 90,
      enableSorting: false,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.commentsCount}</span>
      ),
    }),
    columnHelper.accessor("createdAt", {
      header: t("createdCol"),
      size: 110,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {new Date(row.original.createdAt).toLocaleDateString()}
        </span>
      ),
    }),
  ];
}

interface TableMeta {
  onRowClick?: (ticket: Ticket) => void;
}

interface TaskListConfig {
  columnVisibility?: VisibilityState;
  columnSizing?: ColumnSizingState;
  sorting?: SortingState;
  grouping?: GroupingState;
}

// Columns offered in the "group by" selector — only those that yield a sensible key.
const GROUPABLE_COLUMNS = [
  "taskStateName",
  "ticketPriorityName",
  "assignee",
  "taskTypeName",
] as const;

const GROUPABLE_COLUMN_LABELS: Record<(typeof GROUPABLE_COLUMNS)[number], string> = {
  taskStateName: "statusCol",
  ticketPriorityName: "priorityCol",
  assignee: "assigneeCol",
  taskTypeName: "typeCol",
};

function getFieldValue(ticket: Ticket, field: string): string {
  switch (field) {
    case "title":
      return ticket.title;
    case "taskStateName":
      return ticket.taskStateName ?? "";
    case "ticketPriorityName":
      return ticket.ticketPriorityName ?? "";
    case "assignee":
      return ticket.assignee?.displayName ?? "";
    case "taskTypeName":
      return ticket.taskTypeName ?? "";
    case "dueDate":
      return ticket.dueDate ?? "";
    default:
      return "";
  }
}

function splitFilters(filters: FilterCondition[]) {
  const serverParams: {
    search?: string;
    taskStateName?: string;
    ticketPriorityName?: string;
    assignee?: string;
    taskTypeName?: string;
    dueDate?: string;
  } = {};
  const localFilters: FilterCondition[] = [];

  for (const filter of filters) {
    if (filter.field === "title" && filter.operator === "contains") {
      serverParams.search = filter.value;
      continue;
    }

    if (filter.operator === "eq") {
      switch (filter.field) {
        case "taskStateName":
          serverParams.taskStateName = filter.value;
          continue;
        case "ticketPriorityName":
          serverParams.ticketPriorityName = filter.value;
          continue;
        case "assignee":
          serverParams.assignee = filter.value;
          continue;
        case "taskTypeName":
          serverParams.taskTypeName = filter.value;
          continue;
        case "dueDate":
          serverParams.dueDate = filter.value;
          continue;
      }
    }

    localFilters.push(filter);
  }

  return { serverParams, localFilters };
}

export default function TaskListPage({ params }: { params: Promise<{ code: string }> }) {
  const t = useTranslations("Tasks");
  const { code } = use(params);
  const { data: project } = useProjectByCode(code);
  const projectId = project?.id ?? "";

  const taskFilterFields = useMemo(
    () => [
      { value: "title", label: t("filterTitle") },
      { value: "taskStateName", label: t("filterStatus") },
      { value: "ticketPriorityName", label: t("filterPriority") },
      { value: "assignee", label: t("filterAssignee") },
      { value: "taskTypeName", label: t("filterTaskType") },
      { value: "dueDate", label: t("filterDueDate") },
    ],
    [t]
  );

  const allColumns = useMemo(() => buildAllColumns(t), [t]);
  const { data: viewConfig } = useViewConfiguration("TaskList", projectId);
  const upsertConfig = useUpsertViewConfiguration();

  const [selectedTicket, setSelectedTicket] = useState<Ticket | null>(null);
  const [showColumnSettings, setShowColumnSettings] = useState(false);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [page, setPage] = useState(1);
  const viewKey = `TaskList:${projectId}`;
  const activeFilters = useFilterStore((s) => s.activeFilters[viewKey] ?? EMPTY_FILTERS);

  const { serverParams, localFilters } = useMemo(
    () => splitFilters(activeFilters),
    [activeFilters]
  );

  useEffect(() => {
    setPage(1);
  }, [activeFilters]);

  const savedConfig = useMemo<TaskListConfig>(() => {
    if (!viewConfig?.configurationJson) return {};
    try {
      return JSON.parse(viewConfig.configurationJson) as TaskListConfig;
    } catch {
      return {};
    }
  }, [viewConfig?.configurationJson]);

  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [columnSizing, setColumnSizing] = useState<ColumnSizingState>({});
  const [grouping, setGrouping] = useState<GroupingState>([]);
  const [expanded, setExpanded] = useState<ExpandedState>(true);

  useEffect(() => {
    setSorting(savedConfig.sorting ?? []);
    setColumnVisibility(savedConfig.columnVisibility ?? {});
    setColumnSizing(savedConfig.columnSizing ?? {});
    setGrouping(savedConfig.grouping ?? []);
  }, [savedConfig]);

  // Sorting is done server-side over the whole result set (not just the current
  // page); translate the active TanStack sort column into the API parameters.
  const serverSort = useMemo(() => {
    const sort = sorting[0];
    return sort ? { sortField: sort.id, sortDirection: sort.desc ? "desc" : "asc" } : {};
  }, [sorting]);

  // Grouping is client-side, so it must see the whole dataset — when active we load
  // up to the server cap on a single page and hide the pager.
  const isGrouped = grouping.length > 0;
  const GROUPED_PAGE_SIZE = 500;

  const {
    data: tickets,
    isLoading,
    error,
  } = useTickets(projectId, {
    ...serverParams,
    ...serverSort,
    page: isGrouped ? 1 : page,
    pageSize: isGrouped ? GROUPED_PAGE_SIZE : 25,
  });

  const filteredTickets = useMemo(() => {
    const items = tickets?.items ?? [];
    if (localFilters.length === 0) return items;
    return items.filter((t) =>
      localFilters.every((f) => {
        const val = getFieldValue(t, f.field);
        switch (f.operator) {
          case "eq":
            return val.toLowerCase() === f.value.toLowerCase();
          case "neq":
            return val.toLowerCase() !== f.value.toLowerCase();
          case "contains":
            return val.toLowerCase().includes(f.value.toLowerCase());
          case "gt":
            return parseFloat(val) > parseFloat(f.value);
          case "lt":
            return parseFloat(val) < parseFloat(f.value);
          default:
            return true;
        }
      })
    );
  }, [tickets, localFilters]);

  const saveTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);
  useEffect(
    () => () => {
      if (saveTimeout.current) {
        clearTimeout(saveTimeout.current);
      }
    },
    []
  );

  const persistConfig = useCallback(
    (config: TaskListConfig) => {
      if (!projectId) return;
      if (saveTimeout.current) clearTimeout(saveTimeout.current);
      saveTimeout.current = setTimeout(() => {
        upsertConfig.mutate({
          projectId,
          viewType: "TaskList",
          configurationJson: JSON.stringify(config),
        });
      }, 1000);
    },
    [projectId, upsertConfig]
  );

  const handleSortingChange = useCallback(
    (updater: SortingState | ((old: SortingState) => SortingState)) => {
      setSorting((prev) => {
        const next = typeof updater === "function" ? updater(prev) : updater;
        persistConfig({ sorting: next, columnVisibility, columnSizing, grouping });
        return next;
      });
      // Server-side sort changes the ordering of the entire dataset — go back to page 1.
      setPage(1);
    },
    [persistConfig, columnVisibility, columnSizing, grouping]
  );

  const handleColumnVisibilityChange = useCallback(
    (updater: VisibilityState | ((old: VisibilityState) => VisibilityState)) => {
      setColumnVisibility((prev) => {
        const next = typeof updater === "function" ? updater(prev) : updater;
        persistConfig({ sorting, columnVisibility: next, columnSizing, grouping });
        return next;
      });
    },
    [persistConfig, sorting, columnSizing, grouping]
  );

  const handleColumnSizingChange = useCallback(
    (updater: ColumnSizingState | ((old: ColumnSizingState) => ColumnSizingState)) => {
      setColumnSizing((prev) => {
        const next = typeof updater === "function" ? updater(prev) : updater;
        persistConfig({ sorting, columnVisibility, columnSizing: next, grouping });
        return next;
      });
    },
    [persistConfig, sorting, columnVisibility, grouping]
  );

  const handleGroupingChange = useCallback(
    (next: GroupingState) => {
      setGrouping(next);
      setExpanded(true);
      setPage(1);
      persistConfig({ sorting, columnVisibility, columnSizing, grouping: next });
    },
    [persistConfig, sorting, columnVisibility, columnSizing]
  );

  // TanStack Table returns non-memoizable functions — React Compiler skips this
  // component. Known library limitation; accepted rather than refactored.
  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: filteredTickets,
    columns: allColumns,
    state: { sorting, columnVisibility, columnSizing, grouping, expanded },
    onSortingChange: handleSortingChange,
    onColumnVisibilityChange: handleColumnVisibilityChange,
    onColumnSizingChange: handleColumnSizingChange,
    onExpandedChange: setExpanded,
    manualSorting: true,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getGroupedRowModel: getGroupedRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
    columnResizeMode: "onChange",
    meta: {
      onRowClick: (ticket: Ticket) => setSelectedTicket(ticket),
    } satisfies TableMeta,
  });

  if (isLoading) {
    return <TableSkeleton rows={10} />;
  }

  if (error) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        {t("loadFailed")}
      </div>
    );
  }

  return (
    <div className="flex h-full gap-0">
      <div className="flex-1 flex flex-col min-w-0">
        <FilterBar viewKey={viewKey} viewType="TaskList" filterFields={taskFilterFields} />

        <div className="flex items-center justify-between mb-4 mt-2">
          <div className="flex items-center gap-3">
            <p className="text-sm text-muted-foreground">
              {localFilters.length > 0
                ? t("pageRange", {
                    from: 1,
                    to: filteredTickets.length,
                    total: tickets?.totalCount ?? 0,
                  })
                : `${tickets?.totalCount ?? 0}`}
            </p>
            {isGrouped && tickets && tickets.totalCount > tickets.items.length && (
              <span className="text-xs text-amber-600">
                {t("groupingTruncated", {
                  shown: tickets.items.length,
                  total: tickets.totalCount,
                })}
              </span>
            )}
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setShowCreateDialog(true)}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-primary-foreground bg-primary rounded-lg hover:opacity-90 transition-opacity"
            >
              <Plus className="h-4 w-4" />
              {t("newTask")}
            </button>
            <button
              onClick={async () => {
                try {
                  const visibleCols = table.getVisibleLeafColumns().map((c) => ({
                    field: c.id,
                    header: typeof c.columnDef.header === "string" ? c.columnDef.header : c.id,
                  }));
                  const firstSort = sorting[0];
                  await exportXlsx({
                    projectId,
                    viewType: "TaskList",
                    columns: visibleCols,
                    filters: {
                      searchTerm: serverParams.search,
                      taskStateName: serverParams.taskStateName,
                      ticketPriorityName: serverParams.ticketPriorityName,
                      assigneeName: serverParams.assignee,
                      taskTypeName: serverParams.taskTypeName,
                      dueDate: serverParams.dueDate,
                    },
                    sort: firstSort
                      ? { sortField: firstSort.id, sortDirection: firstSort.desc ? "desc" : "asc" }
                      : undefined,
                  });
                  if (localFilters.length > 0) {
                    toast.warning(t("exportDownloaded"));
                  } else {
                    toast.success(t("exportDownloaded"));
                  }
                } catch {
                  toast.error(t("exportFailed"));
                }
              }}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
            >
              <Download className="h-4 w-4" />
              {t("export")}
            </button>
            <div className="inline-flex items-center gap-1.5 px-2 py-1.5 text-sm text-muted-foreground border border-border rounded-lg">
              <Layers className="h-4 w-4" />
              <select
                value={grouping[0] ?? ""}
                onChange={(e) => handleGroupingChange(e.target.value ? [e.target.value] : [])}
                className="bg-transparent text-sm text-foreground focus:outline-none"
                aria-label={t("groupBy")}
              >
                <option value="">{t("groupByNone")}</option>
                {GROUPABLE_COLUMNS.map((colId) => (
                  <option key={colId} value={colId}>
                    {t(GROUPABLE_COLUMN_LABELS[colId])}
                  </option>
                ))}
              </select>
            </div>
            <div className="relative">
              <button
                onClick={() => setShowColumnSettings((v) => !v)}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
              >
                <Settings2 className="h-4 w-4" />
                {t("columns")}
              </button>
              {showColumnSettings && (
                <div className="absolute right-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-lg p-3 w-56">
                  <p className="text-xs font-medium text-muted-foreground mb-2">
                    {t("toggleColumns")}
                  </p>
                  {table.getAllLeafColumns().map((column) => (
                    <label
                      key={column.id}
                      className="flex items-center gap-2 py-1 text-sm cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={column.getIsVisible()}
                        onChange={column.getToggleVisibilityHandler()}
                        className="rounded"
                      />
                      {typeof column.columnDef.header === "string"
                        ? column.columnDef.header
                        : column.id}
                    </label>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        {!tickets?.items?.length ? (
          <EmptyState
            icon={<List className="h-12 w-12" />}
            title={t("noTasks")}
            description={t("noTasksDesc")}
          />
        ) : (
          <>
            <div className="rounded-lg border border-border overflow-auto flex-1">
              <table className="w-full" style={{ minWidth: table.getTotalSize() }}>
                <thead>
                  {table.getHeaderGroups().map((headerGroup) => (
                    <tr key={headerGroup.id} className="bg-muted/50">
                      {headerGroup.headers.map((header) => (
                        <th
                          key={header.id}
                          className="relative px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider select-none"
                          style={{ width: header.getSize() }}
                        >
                          {header.isPlaceholder ? null : (
                            <button
                              onClick={header.column.getToggleSortingHandler()}
                              className="flex items-center gap-1 hover:text-foreground transition-colors"
                            >
                              {flexRender(header.column.columnDef.header, header.getContext())}
                              {header.column.getIsSorted() === "asc" ? (
                                <ArrowUp className="h-3 w-3" />
                              ) : header.column.getIsSorted() === "desc" ? (
                                <ArrowDown className="h-3 w-3" />
                              ) : (
                                <ArrowUpDown className="h-3 w-3 opacity-30" />
                              )}
                            </button>
                          )}
                          <div
                            onMouseDown={header.getResizeHandler()}
                            onTouchStart={header.getResizeHandler()}
                            className={cn(
                              "absolute right-0 top-0 h-full w-1 cursor-col-resize select-none touch-none",
                              header.column.getIsResizing() ? "bg-primary" : "hover:bg-border"
                            )}
                          />
                        </th>
                      ))}
                    </tr>
                  ))}
                </thead>
                <tbody className="divide-y divide-border">
                  {table.getRowModel().rows.map((row) =>
                    row.getIsGrouped() ? (
                      <tr key={row.id} className="bg-muted/40">
                        <td colSpan={table.getVisibleLeafColumns().length} className="px-3 py-2">
                          <button
                            onClick={row.getToggleExpandedHandler()}
                            className="flex items-center gap-1.5 text-sm font-medium text-foreground"
                          >
                            {row.getIsExpanded() ? (
                              <ChevronDown className="h-4 w-4" />
                            ) : (
                              <ChevronRight className="h-4 w-4" />
                            )}
                            {row.groupingColumnId
                              ? String(row.getGroupingValue(row.groupingColumnId) ?? "—")
                              : "—"}
                            <span className="text-muted-foreground font-normal">
                              ({row.subRows.length})
                            </span>
                          </button>
                        </td>
                      </tr>
                    ) : (
                      <tr
                        key={row.id}
                        className={cn(
                          "hover:bg-muted/30 cursor-pointer transition-colors",
                          selectedTicket?.id === row.original.id && "bg-muted/50"
                        )}
                        onClick={() => setSelectedTicket(row.original)}
                      >
                        {row.getVisibleCells().map((cell) => (
                          <td
                            key={cell.id}
                            className="px-4 py-3"
                            style={{ width: cell.column.getSize() }}
                          >
                            {flexRender(cell.column.columnDef.cell, cell.getContext())}
                          </td>
                        ))}
                      </tr>
                    )
                  )}
                </tbody>
              </table>
            </div>
            {!isGrouped && tickets && tickets.totalPages > 1 && (
              <div className="flex items-center justify-between pt-3">
                <p className="text-sm text-muted-foreground">
                  {t("pageOf", { page: tickets.page, total: tickets.totalPages })}
                </p>
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => setPage((p) => p - 1)}
                    disabled={!tickets.hasPreviousPage}
                    className="inline-flex items-center gap-1 px-2 py-1.5 text-sm border border-border rounded-lg hover:bg-muted transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                    aria-label={t("previousPage")}
                  >
                    <ChevronLeft className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => setPage((p) => p + 1)}
                    disabled={!tickets.hasNextPage}
                    className="inline-flex items-center gap-1 px-2 py-1.5 text-sm border border-border rounded-lg hover:bg-muted transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                    aria-label={t("nextPage")}
                  >
                    <ChevronRight className="h-4 w-4" />
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {selectedTicket && (
        <TaskPreviewSidebar
          ticket={selectedTicket}
          code={code}
          onClose={() => setSelectedTicket(null)}
        />
      )}

      <CreateTicketDialog
        projectId={projectId}
        projectTemplateId={project?.projectTemplateId}
        open={showCreateDialog}
        onClose={() => setShowCreateDialog(false)}
      />
    </div>
  );
}
