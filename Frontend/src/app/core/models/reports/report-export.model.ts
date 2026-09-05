export interface ExportColumn {
  header: string;
  field: string;
  align?: 'left' | 'center' | 'right';
  formatter?: (val: any, row?: any) => string;
}
