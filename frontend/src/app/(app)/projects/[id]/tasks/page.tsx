"use client";

import { use, useState, useMemo, useCallback, useRef } from "react";
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  flexRender,
  createColumnHelper,
  type SortingState,
  type VisibilityState,
  type ColumnSizingState,
} from "@tanstack/react-table";
import { useTickets } from "@/queries/tickets";
import {
  useViewConfiguration,
  useUpsertViewConfiguration,
} from "@/queries/view-configurations";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { PriorityBadge } from "@/components/shared/priority-badge";
import { StatusBadge, DynamicStateBadge } from "@/components/shared/status-badge";
import { TaskPreviewSidebar } from "@/components/task-list/TaskPreviewSidebar";
import { FilterBar } from "@/components/filters/FilterBar";
import { useFilterStore } from "@/stores/filter-store";
import { exportXlsx } from "@/queries/export";
import { List, Settings2, ArrowUpDown, ArrowUp, ArrowDown, Download } from "lucide-react";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import type { Ticket } from "@/types";

const taskFilterFields = [
  { value: "title", label: "Title" },
  { value: "status", label: "Status" },
  { value: "priority", label: "Priority" },
  { value: "assignee", label: "Assignee" },
  { value: "taskTypeName", label: "Task Type" },
  { value: "taskStateName", label: "Task State" },
  { value: "dueDate", label: "Due Date" },
];

const columnHelper = createColumnHelper<Ticket>();

const allColumns = [
  columnHelper.accessor("title", {
    header: "Title",
    size: 300,
    minSize: 150,
    cell: ({ row, table }) => {
      const meta = table.options.meta as TableMeta | undefined;
      return (
        <button
          onClick={() => meta?.onRowClick?.(row.original)}
          className="text-left text-sm font-medium text-foreground hover:text-primary truncate block w-full"
        >
          {row.original.taskTypeIcon && (
            <span className="mr-1">{row.original.taskTypeIcon}</span>
          )}
          {row.original.title}
        </button>
      );
    },
  }),
  columnHelper.accessor("status", {
    header: "Status",
    size: 120,
    cell: ({ row }) => <StatusBadge status={row.original.status} />,
  }),
  columnHelper.accessor("taskStateName", {
    header: "State",
    size: 120,
    cell: ({ row }) =>
      row.original.taskStateName && row.original.taskStateColor ? (
        <DynamicStateBadge
          name={row.original.taskStateName}
          color={row.original.taskStateColor}
        />
      ) : (
        <span className="text-xs text-muted-foreground">-</span>
      ),
  }),
  columnHelper.accessor("priority", {
    header: "Priority",
    size: 100,
    cell: ({ row }) => <PriorityBadge priority={row.original.priority} />,
  }),
  columnHelper.accessor("assignee", {
    header: "Assignee",
    size: 150,
    cell: ({ row }) => (
      <span className="text-sm text-foreground">
        {row.original.assignee?.displayName || "Unassigned"}
      </span>
    ),
    sortingFn: (rowA, rowB) => {
      const a = rowA.original.assignee?.displayName || "";
      const b = rowB.original.assignee?.displayName || "";
      return a.localeCompare(b);
    },
  }),
  columnHelper.accessor("taskTypeName", {
    header: "Type",
    size: 120,
    cell: ({ row }) => (
      <span className="text-sm text-foreground">
        {row.original.taskTypeIcon && (
          <span className="mr-1">{row.original.taskTypeIcon}</span>
        )}
        {row.original.taskTypeName || "-"}
      </span>
    ),
  }),
  columnHelper.accessor("dueDate", {
    header: "Due Date",
    size: 110,
    cell: ({ row }) => (
      <span className="text-sm text-foreground">
        {row.original.dueDate
          ? new Date(row.original.dueDate).toLocaleDateString()
          : "-"}
      </span>
    ),
  }),
  columnHelper.accessor("estimatedHours", {
    header: "Est. Hours",
    size: 100,
    cell: ({ row }) => (
      <span className="text-sm text-foreground">
        {row.original.estimatedHours != null
          ? `${row.original.estimatedHours}h`
          : "-"}
      </span>
    ),
  }),
  columnHelper.accessor("cumulativeWorkedHours", {
    header: "Worked",
    size: 90,
    cell: ({ row }) => (
      <span className="text-sm text-foreground">
        {row.original.cumulativeWorkedHours != null &&
        row.original.cumulativeWorkedHours > 0
          ? `${row.original.cumulativeWorkedHours}h`
          : "-"}
      </span>
    ),
  }),
  columnHelper.accessor("commentsCount", {
    header: "Comments",
    size: 90,
    cell: ({ row }) => (
      <span className="text-sm text-muted-foreground">
        {row.original.commentsCount}
      </span>
    ),
  }),
  columnHelper.accessor("createdAt", {
    header: "Created",
    size: 110,
    cell: ({ row }) => (
      <span className="text-sm text-muted-foreground">
        {new Date(row.original.createdAt).toLocaleDateString()}
      </span>
    ),
  }),
];

interface TableMeta {
  onRowClick?: (ticket: Ticket) => void;
}

interface TaskListConfig {
  columnVisibility?: VisibilityState;
  columnSizing?: ColumnSizingState;
  sorting?: SortingState;
}

function getFieldValue(ticket: Ticket, field: string): string {
  switch (field) {
    case "title":
      return ticket.title;
    case "status":
      return ticket.status;
    case "priority":
      return ticket.priority;
    case "assignee":
      return ticket.assignee?.displayName ?? "";
    case "taskTypeName":
      return ticket.taskTypeName ?? "";
    case "taskStateName":
      return ticket.taskStateName ?? "";
    case "dueDate":
      return ticket.dueDate ?? "";
    default:
      return "";
  }
}

export default function TaskListPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id: projectId } = use(params);
  const { data: tickets, isLoading, error } = useTickets(projectId);
  const { data: viewConfig } = useViewConfiguration("TaskList", projectId);
  const upsertConfig = useUpsertViewConfiguration();

  const [selectedTicket, setSelectedTicket] = useState<Ticket | null>(null);
  const [showColumnSettings, setShowColumnSettings] = useState(false);
  const viewKey = `TaskList:${projectId}`;
  const activeFilters = useFilterStore((s) => s.activeFilters[viewKey] ?? []);

  // Parse saved configuration
  const savedConfig = useMemo<TaskListConfig>(() => {
    if (!viewConfig?.configurationJson) return {};
    try {
      return JSON.parse(viewConfig.configurationJson);
    } catch {
      return {};
    }
  }, [viewConfig?.configurationJson]);

  const [sorting, setSorting] = useState<SortingState>(
    savedConfig.sorting ?? []
  );
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>(
    savedConfig.columnVisibility ?? {}
  );
  const [columnSizing, setColumnSizing] = useState<ColumnSizingState>(
    savedConfig.columnSizing ?? {}
  );

  // Client-side filtering
  const filteredTickets = useMemo(() => {
    if (!tickets || activeFilters.length === 0) return tickets ?? [];
    return tickets.filter((t) =>
      activeFilters.every((f) => {
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
  }, [tickets, activeFilters]);

  // Debounced save
  const saveTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);
  const persistConfig = useCallback(
    (config: TaskListConfig) => {
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
        persistConfig({ sorting: next, columnVisibility, columnSizing });
        return next;
      });
    },
    [persistConfig, columnVisibility, columnSizing]
  );

  const handleColumnVisibilityChange = useCallback(
    (
      updater: VisibilityState | ((old: VisibilityState) => VisibilityState)
    ) => {
      setColumnVisibility((prev) => {
        const next = typeof updater === "function" ? updater(prev) : updater;
        persistConfig({ sorting, columnVisibility: next, columnSizing });
        return next;
      });
    },
    [persistConfig, sorting, columnSizing]
  );

  const handleColumnSizingChange = useCallback(
    (
      updater:
        | ColumnSizingState
        | ((old: ColumnSizingState) => ColumnSizingState)
    ) => {
      setColumnSizing((prev) => {
        const next = typeof updater === "function" ? updater(prev) : updater;
        persistConfig({ sorting, columnVisibility, columnSizing: next });
        return next;
      });
    },
    [persistConfig, sorting, columnVisibility]
  );

  const table = useReactTable({
    data: filteredTickets,
    columns: allColumns,
    state: { sorting, columnVisibility, columnSizing },
    onSortingChange: handleSortingChange,
    onColumnVisibilityChange: handleColumnVisibilityChange,
    onColumnSizingChange: handleColumnSizingChange,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
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
        Failed to load tasks. Please try again.
      </div>
    );
  }

  return (
    <div className="flex h-full gap-0">
      {/* Main table area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Filter bar */}
        <FilterBar
          viewKey={viewKey}
          viewType="TaskList"
          projectId={projectId}
          filterFields={taskFilterFields}
        />

        {/* Toolbar */}
        <div className="flex items-center justify-between mb-4 mt-2">
          <p className="text-sm text-muted-foreground">
            {filteredTickets.length} task{filteredTickets.length !== 1 ? "s" : ""}
            {activeFilters.length > 0 && tickets
              ? ` (of ${tickets.length})`
              : ""}
          </p>
          <div className="flex items-center gap-2">
            <button
              onClick={async () => {
                try {
                  const visibleCols = table
                    .getVisibleLeafColumns()
                    .map((c) => ({
                      field: c.id,
                      header:
                        typeof c.columnDef.header === "string"
                          ? c.columnDef.header
                          : c.id,
                    }));
                  await exportXlsx({
                    projectId,
                    viewType: "TaskList",
                    columns: visibleCols,
                  });
                  toast.success("Export downloaded");
                } catch {
                  toast.error("Export failed");
                }
              }}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
            >
              <Download className="h-4 w-4" />
              Export
            </button>
            <div className="relative">
            <button
              onClick={() => setShowColumnSettings((v) => !v)}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
            >
              <Settings2 className="h-4 w-4" />
              Columns
            </button>
            {showColumnSettings && (
              <div className="absolute right-0 top-full mt-1 z-20 bg-card border border-border rounded-lg shadow-lg p-3 w-56">
                <p className="text-xs font-medium text-muted-foreground mb-2">
                  Toggle columns
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

        {/* Table */}
        {(!tickets || tickets.length === 0) ? (
          <EmptyState
            icon={<List className="h-12 w-12" />}
            title="No tasks yet"
            description="Create tickets on the board or add them via the API."
          />
        ) : (
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
                            {flexRender(
                              header.column.columnDef.header,
                              header.getContext()
                            )}
                            {header.column.getIsSorted() === "asc" ? (
                              <ArrowUp className="h-3 w-3" />
                            ) : header.column.getIsSorted() === "desc" ? (
                              <ArrowDown className="h-3 w-3" />
                            ) : (
                              <ArrowUpDown className="h-3 w-3 opacity-30" />
                            )}
                          </button>
                        )}
                        {/* Column resize handle */}
                        <div
                          onMouseDown={header.getResizeHandler()}
                          onTouchStart={header.getResizeHandler()}
                          className={cn(
                            "absolute right-0 top-0 h-full w-1 cursor-col-resize select-none touch-none",
                            header.column.getIsResizing()
                              ? "bg-primary"
                              : "hover:bg-border"
                          )}
                        />
                      </th>
                    ))}
                  </tr>
                ))}
              </thead>
              <tbody className="divide-y divide-border">
                {table.getRowModel().rows.map((row) => (
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
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext()
                        )}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Preview sidebar */}
      {selectedTicket && (
        <TaskPreviewSidebar
          ticket={selectedTicket}
          projectId={projectId}
          onClose={() => setSelectedTicket(null)}
        />
      )}
    </div>
  );
}
