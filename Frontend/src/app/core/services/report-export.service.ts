import { Injectable } from '@angular/core';
import { ExportColumn } from '../models/reports/report-export.model';

@Injectable({
  providedIn: 'root',
})
export class ReportExportService {
  /**
   * Export dataset as CSV
   */
  exportToCsv(filename: string, columns: ExportColumn[], rows: any[], title?: string): void {
    const csvRows: string[] = [];

    if (title) {
      csvRows.push(`"${title.replace(/"/g, '""')}"`);
      csvRows.push(`"Generated on: ${new Date().toLocaleString()}"`);
      csvRows.push('');
    }

    // Header row
    const headers = columns.map((c) => `"${c.header.replace(/"/g, '""')}"`).join(',');
    csvRows.push(headers);

    // Data rows
    rows.forEach((row) => {
      const values = columns.map((col) => {
        let val = row[col.field];
        if (col.formatter) {
          val = col.formatter(val, row);
        } else if (val === null || val === undefined) {
          val = '';
        }
        return `"${String(val).replace(/"/g, '""')}"`;
      });
      csvRows.push(values.join(','));
    });

    const blob = new Blob(['\uFEFF' + csvRows.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
    this.downloadBlob(blob, `${filename}.csv`);
  }

  /**
   * Export dataset as Microsoft Excel XML (.xls spreadsheet)
   */
  exportToExcel(
    filename: string,
    title: string,
    columns: ExportColumn[],
    rows: any[],
    grandTotals?: Record<string, any>
  ): void {
    const tableHtml = `
      <html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40">
      <head>
        <!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>Report</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->
        <meta http-equiv="content-type" content="text/plain; charset=UTF-8"/>
        <style>
          body { font-family: Calibri, Arial, sans-serif; }
          .title { font-size: 16pt; font-weight: bold; color: #1e3a8a; }
          .meta { font-size: 10pt; color: #64748b; margin-bottom: 12px; }
          th { background-color: #1e40af; color: #ffffff; font-weight: bold; text-align: left; padding: 6px 10px; border: 1px solid #cbd5e1; }
          td { padding: 5px 10px; border: 1px solid #e2e8f0; }
          .num { text-align: right; }
          .center { text-align: center; }
          .total-row { background-color: #f1f5f9; font-weight: bold; border-top: 2px solid #0f172a; }
        </style>
      </head>
      <body>
        <div class="title">${title}</div>
        <div class="meta">Campus Services Portal | Generated on: ${new Date().toLocaleString()}</div>
        <table border="1" cellpadding="5" cellspacing="0">
          <thead>
            <tr>
              ${columns.map((c) => `<th>${c.header}</th>`).join('')}
            </tr>
          </thead>
          <tbody>
            ${rows
              .map(
                (row) => `
              <tr>
                ${columns
                  .map((c) => {
                    let val = row[c.field];
                    if (c.formatter) val = c.formatter(val, row);
                    else if (val === null || val === undefined) val = '';
                    const alignClass = c.align === 'right' ? ' class="num"' : c.align === 'center' ? ' class="center"' : '';
                    return `<td${alignClass}>${val}</td>`;
                  })
                  .join('')}
              </tr>`
              )
              .join('')}
          </tbody>
        </table>
      </body>
      </html>
    `;

    const blob = new Blob([tableHtml], { type: 'application/vnd.ms-excel;charset=utf-8' });
    this.downloadBlob(blob, `${filename}.xls`);
  }

  /**
   * Export dataset as Microsoft Word document (.doc)
   */
  exportToWord(filename: string, title: string, columns: ExportColumn[], rows: any[]): void {
    const wordHtml = `
      <html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>
      <head>
        <meta charset="utf-8">
        <title>${title}</title>
        <style>
          body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20mm; }
          .header { text-align: center; border-bottom: 2px solid #2563eb; padding-bottom: 8px; margin-bottom: 16px; }
          .university { font-size: 14pt; font-weight: bold; color: #1e3a8a; }
          .report-title { font-size: 16pt; font-weight: bold; color: #0f172a; margin-top: 4px; }
          .meta { font-size: 9pt; color: #64748b; margin-top: 4px; }
          table { width: 100%; border-collapse: collapse; margin-top: 15px; }
          th { background-color: #2563eb; color: #ffffff; padding: 8px 6px; font-size: 10pt; text-align: left; border: 1px solid #1d4ed8; }
          td { padding: 6px; font-size: 9pt; border: 1px solid #e2e8f0; }
          .num { text-align: right; }
          .center { text-align: center; }
          .footer { margin-top: 30px; font-size: 8pt; color: #94a3b8; text-align: center; border-top: 1px solid #e2e8f0; padding-top: 8px; }
        </style>
      </head>
      <body>
        <div class="header">
          <div class="university">CAMPUS SERVICES PORTAL</div>
          <div class="report-title">${title}</div>
          <div class="meta">Generated: ${new Date().toLocaleString()} | Official University Report</div>
        </div>
        <table>
          <thead>
            <tr>
              ${columns.map((c) => `<th>${c.header}</th>`).join('')}
            </tr>
          </thead>
          <tbody>
            ${rows
              .map(
                (row) => `
              <tr>
                ${columns
                  .map((c) => {
                    let val = row[c.field];
                    if (c.formatter) val = c.formatter(val, row);
                    else if (val === null || val === undefined) val = '';
                    const alignClass = c.align === 'right' ? ' class="num"' : c.align === 'center' ? ' class="center"' : '';
                    return `<td${alignClass}>${val}</td>`;
                  })
                  .join('')}
              </tr>`
              )
              .join('')}
          </tbody>
        </table>
        <div class="footer">
          Confidential • Campus Services Institutional Analytics System • Page 1
        </div>
      </body>
      </html>
    `;

    const blob = new Blob(['\uFEFF' + wordHtml], { type: 'application/msword' });
    this.downloadBlob(blob, `${filename}.doc`);
  }

  /**
   * Print Report Canvas using Angular @media print CSS rules (Zero DOM manipulation)
   */
  printReport(): void {
    if (typeof window !== 'undefined') {
      window.print();
    }
  }

  /**
   * PDF Export via Browser Print / Save to PDF
   */
  exportToPdf(): void {
    this.printReport();
  }

  private downloadBlob(blob: Blob, filename: string): void {
    if (typeof window === 'undefined') return;
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
