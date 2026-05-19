"use client";

import { use, useState, useMemo, useCallback, useRef, useEffect } from "react";
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
import { useWorklogsPaged, useCreateWorklog, useDeleteWorklog } from "@/queries/worklogs";
import { useProjectByCode } from "@/queries/projects";
import { useTickets } from "@/queries/tickets";
import { useAdminUsers } from "@/queries/admin";
import { useCurrentUser } from "@/queries/auth";
import { useViewConfiguration, useUpsertViewConfiguration } from "@/queries/view-configurations";
import { exportXlsx } from "@/queries/export";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { EditWorklogDialog } from "@/components/shared/edit-worklog-dialog";
import {
  Clock,
  Plus,
  X,
  Pencil,
  Trash2,
  ChevronLeft,
  ChevronRight,
  Settings2,
  Download,
  ArrowUp,
  ArrowDown,
  ArrowUpDown,
} from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createWorklogSchema, type CreateWorklogInput } from "@/schemas/worklog";
import { toast } from "sonner";
import { format } from "date-fns";
import { useTranslations } from "next-intl";
import { GlobalRole, ProjectRole, type Worklog } from "@/types";
import { cn } from "@/lib/utils";

const columnHelper = createColumnHelper<Worklog>();

interface WorklogTableMeta {
  onEdit?: (worklog: Worklog) => void;
  onDelete?: (worklog: Worklog) => void;
  canEdit?: (worklog: Worklog) => boolean;
  canDelete?: (worklog: Worklog) => boolean;
  isDeleting?: boolean;
}

function buildAllColumns(t: (key: string) => string) {
  return [
    columnHelper.accessor("date", {
      header: t("date"),
      size: 110,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">
          {format(new Date(row.original.date), "yyyy-MM-dd")}
        </span>
      ),
    }),
    columnHelper.accessor("ticketTitle", {
      id: "ticket",
      header: t("ticket"),
      size: 280,
      minSize: 150,
      cell: ({ row }) => (
        <span className="text-sm text-foreground truncate block">
          {row.original.ticketTitle || "-"}
        </span>
      ),
    }),
    columnHelper.accessor((row) => row.user.displayName, {
      id: "user",
      header: t("user"),
      size: 160,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">{row.original.user.displayName}</span>
      ),
    }),
    columnHelper.accessor("hours", {
      header: t("hours"),
      size: 80,
      cell: ({ row }) => (
        <span className="text-sm font-medium text-foreground">
          {row.original.hours.toFixed(2)}h
        </span>
      ),
    }),
    columnHelper.accessor("description", {
      header: t("description"),
      size: 320,
      minSize: 150,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground truncate block">
          {row.original.description}
        </span>
      ),
    }),
    columnHelper.accessor("isBillable", {
      header: t("billable"),
      size: 90,
      cell: ({ row }) =>
        row.original.isBillable ? (
          <span className="text-green-600 text-xs font-medium">{t("yes")}</span>
        ) : (
          <span className="text-muted-foreground text-xs">{t("no")}</span>
        ),
    }),
    columnHelper.accessor("source", {
      header: t("source"),
      size: 100,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.source}</span>
      ),
    }),
    columnHelper.accessor("invoiced", {
      header: t("invoiced"),
      size: 130,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.invoiced || "-"}</span>
      ),
    }),
    columnHelper.accessor("createdAt", {
      header: t("created"),
      size: 120,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {format(new Date(row.original.createdAt), "yyyy-MM-dd")}
        </span>
      ),
    }),
    columnHelper.display({
      id: "actions",
      header: "",
      size: 80,
      enableResizing: false,
      enableSorting: false,
      cell: ({ row, table }) => {
        const meta = table.options.meta as WorklogTableMeta | undefined;
        const w = row.original;
        return (
          <div className="flex items-center justify-end gap-1">
            {meta?.canEdit?.(w) && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  meta.onEdit?.(w);
                }}
                className="p-1 text-muted-foreground hover:text-foreground rounded"
                title={t("editAriaLabel")}
                aria-label={t("editAriaLabel")}
              >
                <Pencil className="h-3.5 w-3.5" />
              </button>
            )}
            {meta?.canDelete?.(w) && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  meta.onDelete?.(w);
                }}
                disabled={meta?.isDeleting}
                className="p-1 text-muted-foreground hover:text-destructive rounded disabled:opacity-50"
                title={t("deleteAriaLabel")}
                aria-label={t("deleteAriaLabel")}
              >
                <Trash2 className="h-3.5 w-3.5" />
              </button>
            )}
          </div>
        );
      },
    }),
  ];
}

interface WorklogListConfig {
  columnVisibility?: VisibilityState;
  columnSizing?: ColumnSizingState;
  sorting?: SortingState;
}

function AddWorklogDialog({
  open,
  onClose,
  projectId,
  isAdmin,
}: {
  open: boolean;
  onClose: () => void;
  projectId: string;
  isAdmin: boolean;
}) {
  const t = useTranslations("ProjectWorklogs");
  const tCommon = useTranslations("Common");
  const createWorklog = useCreateWorklog();
  const { data: ticketsPage } = useTickets(projectId, { pageSize: 200 });
  const { data: adminUsers } = useAdminUsers();
  const tickets = ticketsPage?.items ?? [];
  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<CreateWorklogInput>({
    resolver: zodResolver(createWorklogSchema),
    defaultValues: {
      projectId,
      date: format(new Date(), "yyyy-MM-dd"),
      isBillable: true,
      description: "",
    },
  });
  const description = watch("description") ?? "";

  const onSubmit = async (data: CreateWorklogInput) => {
    try {
      await createWorklog.mutateAsync({
        ...data,
        overrideUserId: data.overrideUserId || undefined,
      });
      toast.success(t("added"));
      reset();
      onClose();
    } catch {
      toast.error(t("addFailed"));
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={onClose} />
      <div className="relative bg-card rounded-xl shadow-xl border border-border w-full max-w-md mx-4 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-card-foreground">{t("addWorklog")}</h2>
          <button onClick={onClose} className="p-1 rounded hover:bg-muted transition-colors">
            <X className="h-5 w-5 text-muted-foreground" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <input type="hidden" {...register("projectId")} />

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("ticket")} <span className="text-destructive">*</span>
            </label>
            <select
              {...register("ticketId")}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">{t("selectTicket")}</option>
              {tickets.map((tk) => (
                <option key={tk.id} value={tk.id}>
                  #{tk.number} — {tk.title}
                </option>
              ))}
            </select>
            {errors.ticketId && (
              <p className="text-xs text-destructive mt-1">{errors.ticketId.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("date")}
            </label>
            <input
              {...register("date")}
              type="date"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
            {errors.date && <p className="text-xs text-destructive mt-1">{errors.date.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-card-foreground mb-1">
              {t("hours")}
            </label>
            <input
              {...register("hours", { valueAsNumber: true })}
              type="number"
              step="0.25"
              min="0.25"
              max="24"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="1.0"
            />
            {errors.hours && (
              <p className="text-xs text-destructive mt-1">{errors.hours.message}</p>
            )}
          </div>

          <div>
            <label className="flex items-center justify-between text-sm font-medium text-card-foreground mb-1">
              <span>
                {t("description")} <span className="text-destructive">*</span>
              </span>
              <span className="text-xs font-normal text-muted-foreground">
                {t("descriptionCounter", { count: description.length })}
              </span>
            </label>
            <textarea
              {...register("description")}
              rows={3}
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring resize-none"
              placeholder={t("descriptionPlaceholder")}
            />
            {errors.description && (
              <p className="text-xs text-destructive mt-1">{errors.description.message}</p>
            )}
          </div>

          {isAdmin && (
            <div>
              <label className="block text-sm font-medium text-card-foreground mb-1">
                {t("logOnBehalf")}
              </label>
              <select
                {...register("overrideUserId")}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">{t("logAsMyself")}</option>
                {(adminUsers ?? [])
                  .filter((u) => u.isActive)
                  .map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.displayName} ({u.email})
                    </option>
                  ))}
              </select>
            </div>
          )}

          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" {...register("isBillable")} className="rounded" />
            {t("billable")}
          </label>

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
            >
              {tCommon("cancel")}
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {isSubmitting ? t("addingWorklog") : t("addWorklog")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function ProjectWorklogsPage({ params }: { params: Promise<{ code: string }> }) {
  const t = useTranslations("ProjectWorklogs");
  const { code } = use(params);
  const { data: project } = useProjectByCode(code);
  const projectId = project?.id ?? "";
  const allColumns = useMemo(() => buildAllColumns(t), [t]);
  const [page, setPage] = useState(1);
  const { data: worklogsPage, isLoading, error } = useWorklogsPaged({ projectId, page });
  const worklogs = useMemo(() => worklogsPage?.items ?? [], [worklogsPage]);
  const { data: currentUser } = useCurrentUser();
  const deleteWorklog = useDeleteWorklog();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingWorklog, setEditingWorklog] = useState<Worklog | null>(null);
  const [showColumnSettings, setShowColumnSettings] = useState(false);

  const { data: viewConfig } = useViewConfiguration("WorklogList", projectId);
  const upsertConfig = useUpsertViewConfiguration();

  const savedConfig = useMemo<WorklogListConfig>(() => {
    if (!viewConfig?.configurationJson) return {};
    try {
      return JSON.parse(viewConfig.configurationJson) as WorklogListConfig;
    } catch {
      return {};
    }
  }, [viewConfig?.configurationJson]);

  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [columnSizing, setColumnSizing] = useState<ColumnSizingState>({});

  useEffect(() => {
    setSorting(savedConfig.sorting ?? []);
    setColumnVisibility(savedConfig.columnVisibility ?? {});
    setColumnSizing(savedConfig.columnSizing ?? {});
  }, [savedConfig]);

  const saveTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);
  useEffect(
    () => () => {
      if (saveTimeout.current) clearTimeout(saveTimeout.current);
    },
    []
  );

  const persistConfig = useCallback(
    (config: WorklogListConfig) => {
      if (!projectId) return;
      if (saveTimeout.current) clearTimeout(saveTimeout.current);
      saveTimeout.current = setTimeout(() => {
        upsertConfig.mutate({
          projectId,
          viewType: "WorklogList",
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
    (updater: VisibilityState | ((old: VisibilityState) => VisibilityState)) => {
      setColumnVisibility((prev) => {
        const next = typeof updater === "function" ? updater(prev) : updater;
        persistConfig({ sorting, columnVisibility: next, columnSizing });
        return next;
      });
    },
    [persistConfig, sorting, columnSizing]
  );

  const handleColumnSizingChange = useCallback(
    (updater: ColumnSizingState | ((old: ColumnSizingState) => ColumnSizingState)) => {
      setColumnSizing((prev) => {
        const next = typeof updater === "function" ? updater(prev) : updater;
        persistConfig({ sorting, columnVisibility, columnSizing: next });
        return next;
      });
    },
    [persistConfig, sorting, columnVisibility]
  );

  const isProjectManager = (pid: string) =>
    !!currentUser &&
    currentUser.projectRoles.some(
      (pr) => pr.projectId === pid && pr.role === ProjectRole.ProjectManager
    );

  const canEdit = useCallback(
    (worklog: Worklog) =>
      !!currentUser &&
      currentUser.permissions.timeTrackingUpdate &&
      (worklog.userId === currentUser.id ||
        currentUser.globalRole === GlobalRole.Admin ||
        isProjectManager(worklog.projectId)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [currentUser]
  );

  const canDelete = useCallback(
    (worklog: Worklog) =>
      !!currentUser &&
      currentUser.permissions.timeTrackingDelete &&
      (worklog.userId === currentUser.id ||
        currentUser.globalRole === GlobalRole.Admin ||
        isProjectManager(worklog.projectId)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [currentUser]
  );

  const handleDelete = useCallback(
    async (worklog: Worklog) => {
      if (!window.confirm(t("deleteConfirm"))) return;
      try {
        await deleteWorklog.mutateAsync({ projectId: worklog.projectId, worklogId: worklog.id });
        toast.success(t("deleted"));
      } catch {
        toast.error(t("deleteFailed"));
      }
    },
    [deleteWorklog, t]
  );

  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: worklogs,
    columns: allColumns,
    state: { sorting, columnVisibility, columnSizing },
    onSortingChange: handleSortingChange,
    onColumnVisibilityChange: handleColumnVisibilityChange,
    onColumnSizingChange: handleColumnSizingChange,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    columnResizeMode: "onChange",
    meta: {
      onEdit: setEditingWorklog,
      onDelete: handleDelete,
      canEdit,
      canDelete,
      isDeleting: deleteWorklog.isPending,
    } satisfies WorklogTableMeta,
  });

  const handleExport = async () => {
    try {
      const visibleCols = table
        .getVisibleLeafColumns()
        .filter((c) => c.id !== "actions")
        .map((c) => ({
          field: c.id,
          header: typeof c.columnDef.header === "string" ? c.columnDef.header : c.id,
        }));
      const firstSort = sorting[0];
      await exportXlsx({
        projectId,
        viewType: "Worklogs",
        columns: visibleCols,
        sort: firstSort
          ? { sortField: firstSort.id, sortDirection: firstSort.desc ? "desc" : "asc" }
          : undefined,
      });
      toast.success(t("exportDownloaded"));
    } catch {
      toast.error(t("exportFailed"));
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          {t("subtitle")}
          {worklogsPage ? t("totalSuffix", { count: worklogsPage.totalCount }) : ""}
        </p>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setDialogOpen(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-primary-foreground bg-primary rounded-lg hover:opacity-90 transition-opacity"
          >
            <Plus className="h-4 w-4" />
            {t("addWorklog")}
          </button>
          <button
            onClick={handleExport}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-muted transition-colors"
          >
            <Download className="h-4 w-4" />
            {t("export")}
          </button>
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
                {table
                  .getAllLeafColumns()
                  .filter((column) => column.id !== "actions")
                  .map((column) => (
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

      {isLoading && <TableSkeleton rows={8} />}

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
          {t("loadFailed")}
        </div>
      )}

      {worklogs.length === 0 && !isLoading && (
        <EmptyState
          icon={<Clock className="h-12 w-12" />}
          title={t("noWorklogs")}
          description={t("noWorklogsDesc")}
        />
      )}

      {worklogs.length > 0 && (
        <div className="rounded-lg border border-border overflow-auto">
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
                      {header.isPlaceholder ? null : header.column.getCanSort() ? (
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
                      ) : (
                        flexRender(header.column.columnDef.header, header.getContext())
                      )}
                      {header.column.getCanResize() && (
                        <div
                          onMouseDown={header.getResizeHandler()}
                          onTouchStart={header.getResizeHandler()}
                          className={cn(
                            "absolute right-0 top-0 h-full w-1 cursor-col-resize select-none touch-none",
                            header.column.getIsResizing() ? "bg-primary" : "hover:bg-border"
                          )}
                        />
                      )}
                    </th>
                  ))}
                </tr>
              ))}
            </thead>
            <tbody className="divide-y divide-border">
              {table.getRowModel().rows.map((row) => (
                <tr key={row.id} className="hover:bg-muted/30">
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
              ))}
            </tbody>
          </table>
        </div>
      )}

      {worklogsPage && worklogsPage.totalCount > worklogsPage.pageSize && (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>
            {t("pageRange", {
              from: (page - 1) * worklogsPage.pageSize + 1,
              to: Math.min(page * worklogsPage.pageSize, worklogsPage.totalCount),
              total: worklogsPage.totalCount,
            })}
          </span>
          <div className="flex items-center gap-1">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={!worklogsPage.hasPreviousPage}
              className="p-1.5 rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed"
              aria-label={t("previousPage")}
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <span className="px-2">
              {page} / {worklogsPage.totalPages}
            </span>
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={!worklogsPage.hasNextPage}
              className="p-1.5 rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed"
              aria-label={t("nextPage")}
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}

      <AddWorklogDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        projectId={projectId}
        isAdmin={currentUser?.globalRole === GlobalRole.Admin}
      />
      <EditWorklogDialog
        worklog={editingWorklog}
        open={!!editingWorklog}
        onClose={() => setEditingWorklog(null)}
      />
    </div>
  );
}
