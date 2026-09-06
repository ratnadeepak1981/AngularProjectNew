import { Injectable, inject } from '@angular/core';
import { Observable, map, catchError, of } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { StudentMaster } from '../../../../core/models/student/student-master.model';
import { StudentProfile } from '../../../../core/models/auth/student-profile.model';
import { CsvMasterRow } from '../../../../core/models/student/csv-master-row.model';
import { Faculty } from '../../../../core/models/faculty/faculty.model';
import { ApiResponse } from '../../../../core/models/common/api-response.model';
import { PagedResponse } from '../../../../core/models/common/paged-response.model';

export interface CsvValidationResult {
  rows: CsvMasterRow[];
  validCount: number;
  errorCount: number;
  headerError: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class StudentMasterService {
  private readonly apiService = inject(ApiService);

  loadStudentAccounts(
    pageNumber: number,
    pageSize: number,
    search?: string
  ): Observable<ApiResponse<PagedResponse<StudentProfile>>> {
    const params: Record<string, string | number> = {
      pageNumber,
      pageSize,
    };
    if (search && search.trim()) {
      params['search'] = search.trim();
    }

    return this.apiService.get<ApiResponse<PagedResponse<StudentProfile>>>(
      this.apiService.routes.students.directory,
      params
    );
  }

  loadMasterList(
    pageNumber: number,
    pageSize: number,
    search?: string
  ): Observable<ApiResponse<PagedResponse<StudentMaster>>> {
    const params: Record<string, string | number> = {
      pageNumber,
      pageSize,
    };
    if (search && search.trim()) {
      params['search'] = search.trim();
    }

    return this.apiService.get<ApiResponse<PagedResponse<StudentMaster>>>(
      this.apiService.routes.students.masterList,
      params
    );
  }

  loadFaculties(): Observable<Map<number, string>> {
    return this.apiService.get<ApiResponse<Faculty[]>>(this.apiService.routes.faculties.list).pipe(
      map((res) => {
        const list = res.data || (Array.isArray(res) ? res : []);
        const facultyMap = new Map<number, string>();
        list.forEach((f: Faculty) => {
          facultyMap.set(f.id, f.name);
        });
        return facultyMap;
      }),
      catchError(() => {
        const fallbackMap = new Map<number, string>();
        fallbackMap.set(1, 'Faculty of Computing & Technology');
        fallbackMap.set(2, 'Faculty of Science');
        fallbackMap.set(3, 'Faculty of Commerce & Management');
        fallbackMap.set(4, 'Faculty of Humanities');
        return of(fallbackMap);
      })
    );
  }

  checkDeactivateSafety(studentId: number): Observable<ApiResponse<any>> {
    return this.apiService.get<ApiResponse<any>>(
      this.apiService.routes.account.deactivateCheck(studentId)
    );
  }

  deactivateAccount(studentId: number): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>(
      this.apiService.routes.account.deactivate(studentId),
      {}
    );
  }

  reactivateAccount(studentId: number): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>(
      this.apiService.routes.account.reactivate(studentId),
      {}
    );
  }

  resetStudentPassword(studentId: number): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>(
      this.apiService.routes.students.resetPassword(studentId),
      {}
    );
  }

  uploadMasterCsv(file: File): Observable<ApiResponse<number>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.apiService.upload<ApiResponse<number>>(
      this.apiService.routes.students.masterImport,
      formData
    );
  }

  validateCsvContent(csvText: string): CsvValidationResult {
    const lines = csvText.split(/\r\n|\n/).map((l) => l.trim()).filter((l) => l.length > 0);

    if (lines.length < 2) {
      return {
        rows: [],
        validCount: 0,
        errorCount: 0,
        headerError: 'CSV file is empty or missing data rows.',
      };
    }

    const headerCols = lines[0].split(',').map((c) => c.trim().toLowerCase().replace(/["']/g, ''));
    const expectedHeaders = ['indexnumber', 'fullname', 'facultyid'];
    const hasValidHeader = expectedHeaders.every((h) =>
      headerCols.some((col) => col.replace(/\s+/g, '') === h)
    );

    if (!hasValidHeader && headerCols.length < 3) {
      return {
        rows: [],
        validCount: 0,
        errorCount: 0,
        headerError: 'Invalid CSV Header. Expected 3 columns: IndexNumber, FullName, FacultyId',
      };
    }

    const rows: CsvMasterRow[] = [];
    const seenIndexNumbers = new Set<string>();
    let validCount = 0;
    let errorCount = 0;

    const indexRegex = /^[A-Za-z0-9\/\-_]+$/;
    const nameRegex = /^[A-Za-z\s.'\-]+$/;
    const forbiddenSymbols = /[<>{}[\]!@#$%^&*=+~;`]/;

    for (let i = 1; i < lines.length; i++) {
      const lineNumber = i + 1;
      const line = lines[i];
      const parts = line.split(',').map((p) => p.trim().replace(/^["']|["']$/g, ''));

      const indexNumber = parts[0] || '';
      const fullName = parts[1] || '';
      const facultyStr = parts[2] || '';
      const facultyId = parseInt(facultyStr, 10);

      const rowErrors: string[] = [];

      if (parts.length < 3) {
        rowErrors.push('Missing required column fields');
      }

      if (!indexNumber) {
        rowErrors.push('Index Number is missing');
      } else if (indexNumber.length < 3 || indexNumber.length > 30) {
        rowErrors.push(`Index Number length must be 3-30 chars (got ${indexNumber.length})`);
      } else if (!indexRegex.test(indexNumber) || forbiddenSymbols.test(indexNumber)) {
        rowErrors.push('Index contains unexpected special symbols');
      }

      const normalizedIndex = indexNumber.toUpperCase();
      if (seenIndexNumbers.has(normalizedIndex)) {
        rowErrors.push(`Duplicate Index '${indexNumber}' already present in file`);
      } else if (indexNumber) {
        seenIndexNumbers.add(normalizedIndex);
      }

      if (!fullName) {
        rowErrors.push('Full Name is required');
      } else if (fullName.length < 2 || fullName.length > 100) {
        rowErrors.push(`Full Name length must be 2-100 chars (got ${fullName.length})`);
      } else if (!nameRegex.test(fullName) || forbiddenSymbols.test(fullName)) {
        rowErrors.push('Full Name contains numbers or unexpected symbols');
      }

      if (!facultyStr || isNaN(facultyId) || facultyId <= 0) {
        rowErrors.push('Faculty ID must be a positive integer');
      }

      const isValid = rowErrors.length === 0;
      if (isValid) {
        validCount++;
      } else {
        errorCount++;
      }

      rows.push({
        lineNumber,
        indexNumber,
        fullName,
        facultyId: isNaN(facultyId) ? 0 : facultyId,
        isValid,
        errors: rowErrors,
      });
    }

    return {
      rows,
      validCount,
      errorCount,
      headerError: null,
    };
  }
}
