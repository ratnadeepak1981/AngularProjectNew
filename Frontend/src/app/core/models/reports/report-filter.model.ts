export interface ReportFilter {
  dateFrom?: string;
  dateTo?: string;
  facultyIds?: number[];
  statuses?: string[];
  hostelIds?: number[];
  labIds?: number[];
  categoryIds?: number[];
  certificateTypeIds?: number[];
  feeTypeIds?: number[];
  eventIds?: number[];
  drilldownKey?: string;
  drilldownId?: string | number;
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
}
