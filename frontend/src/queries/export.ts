import apiClient from "@/lib/api/client";

export interface ExportColumn {
  field: string;
  header: string;
}

export async function exportXlsx({
  projectId,
  viewType,
  columns,
}: {
  projectId: string;
  viewType: string;
  columns: ExportColumn[];
}) {
  const { data } = await apiClient.post(
    `/api/v1/exports/xlsx`,
    { projectId, viewType, columns },
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
