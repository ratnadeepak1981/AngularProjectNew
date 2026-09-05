export interface ReportColumn {
  header: string;
  field: string;
  align?: 'left' | 'center' | 'right';
  width?: string;
  sortable?: boolean;
  formatter?: (val: any, row?: any) => string;
  badgeClass?: (val: any, row?: any) => string;
}
