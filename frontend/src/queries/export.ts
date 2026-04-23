import apiClient from "@/lib/api/client";

export interface ExportColumn {
  field: string;
  header: string;
}

export interface ExportFilters {
  searchTerm?: string;
  taskStateName?: string;
  ticketPriorityName?: string;
  assigneeName?: string;
  taskTypeName?: string;
  dueDate?: string;
}

export interface ExportSort {
  sortField?: string;
  sortDirection?: "asc" | "desc";
}

export async function exportXlsx({
  projectId,
  viewType,
  columns,
  filters,
  sort,
}: {
  projectId: string;
  viewType: string;
  columns: ExportColumn[];
  filters?: ExportFilters;
  sort?: ExportSort;
}) {
  const { data } = await apiClient.post(
    `/api/v1/exports/xlsx`,
    { projectId, viewType, columns, ...filters, ...sort },
    { responseType: "blob" }
  );

  const blob = new Blob([data], {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `export-${viewType}.xlsx`;
  link.click();
  window.URL.revokeObjectURL(url);
}
