export interface PagedReportResult<TItem = any, TSummary = any> {
  items: TItem[];
  summaryData?: TSummary[];
  grandTotals?: Record<string, any>;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  startRecord?: number;
  endRecord?: number;
}
