import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

// Reusable Shared Crystal Report Components
import { MultiSelectDropdownComponent } from '../../../../../shared/components/multi-select-dropdown/multi-select-dropdown.component';
import { MultiSelectOption } from '../../../../../shared/components/multi-select-dropdown/models/multi-select-option.model';
import { ReportViewerComponent } from '../../../../../shared/components/reports/report-viewer/report-viewer.component';
import { ReportHeaderComponent } from '../../../../../shared/components/reports/report-header/report-header.component';
import { ReportGroupHeaderComponent } from '../../../../../shared/components/reports/report-group-header/report-group-header.component';
import { ReportDetailGridComponent } from '../../../../../shared/components/reports/report-detail-grid/report-detail-grid.component';
import { ReportColumn } from '../../../../../shared/components/reports/models/report-column.model';
import { ReportGroupFooterComponent } from '../../../../../shared/components/reports/report-group-footer/report-group-footer.component';
import { GroupSubtotalItem, GrandTotalMetric } from '../../../../../shared/components/reports/models/report-band.model';
import { ReportFooterComponent } from '../../../../../shared/components/reports/report-footer/report-footer.component';
import { ReportPaginationComponent } from '../../../../../shared/components/reports/report-pagination/report-pagination.component';
import { ReportSubreportComponent } from '../../../../../shared/components/reports/report-subreport/report-subreport.component';
import { DashboardCardComponent } from '../../../../../shared/components/cards/dashboard-card/dashboard-card.component';
import { AmountCardComponent } from '../../../../../shared/components/cards/amount-card/amount-card.component';
import { TabComponent, TabItem } from '../../../../../shared/components/tab-component/tab.component';
import { AnalyticsChartComponent } from '../../../../../shared/components/analytics-chart/analytics-chart.component';
import { ChartConfig, ChartDataPoint } from '../../../../../shared/components/analytics-chart/models/chart-data.model';

// Services & Models
import { ReportService } from '../../../../../core/services/report.service';
import { ReportExportService } from '../../../../../core/services/report-export.service';
import { SystemSettingsService } from '../../../../../core/services/system-settings.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { DatePresetUtil } from '../../../../../core/utils/date-preset.util';
import { DropdownOption } from '../../../../../core/models/common/dropdown-option.model';
import { ReportFilter } from '../../../../../core/models/reports/report-filter.model';
import { ExportColumn } from '../../../../../core/models/reports/report-export.model';
import { ReportDomainTab, ReportTabItem } from '../../../../../core/models/reports/report-domain-tab.model';
import { ReportOrientation, ReportPaperSize } from '../../../../../shared/components/reports/models/report-page-setup.model';
import {
  InstitutionalKpiReport,
  FacultyStudentSummary,
  StudentReportItem,
  HostelOccupancySummary,
  UnallocatedStudentItem,
  LabUtilizationSummary,
  LabBookingReportItem,
  FeeTypeSummary,
  BillingLedgerReportItem,
  ComplaintCategorySummary,
  ComplaintReportItem,
  CertificateTypeSummary,
  CertificateReportItem,
  EventSummary,
  EventAttendeeReportItem,
  NotificationTypeSummary,
  NotificationReportItem,
  ReportFilterLookups,
} from '../../../../../core/models/reports/report-domain.models';

@Component({
  selector: 'app-reports-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MultiSelectDropdownComponent,
    ReportViewerComponent,
    ReportHeaderComponent,
    ReportGroupHeaderComponent,
    ReportDetailGridComponent,
    ReportGroupFooterComponent,
    ReportFooterComponent,
    ReportPaginationComponent,
    ReportSubreportComponent,
    DashboardCardComponent,
    AmountCardComponent,
    TabComponent,
    AnalyticsChartComponent,
  ],
  templateUrl: './reports-dashboard.component.html',
  styleUrls: ['./reports-dashboard.component.css'],
})
export class ReportsDashboardComponent implements OnInit {
  private readonly reportService = inject(ReportService);
  private readonly reportExportService = inject(ReportExportService);
  private readonly systemSettingsService = inject(SystemSettingsService);
  private readonly toastService = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  activeTab: ReportDomainTab = 'kpi';
  loading = signal<boolean>(false);
  isReportModalOpen = signal<boolean>(false);
  isSidebarCollapsed = signal<boolean>(false);
  isAnalyticsRoute = signal<boolean>(false);
  activeModuleSubTab = signal<'chart' | 'data'>('chart');
  pageSizeOptions = [5, 10, 25, 50, 100];
  searchCatalogTerm = '';
  reportGeneratedDate = new Date();

  // Page Setup Controls for Left Studio Sidebar
  reportOrientation: ReportOrientation = 'portrait';
  reportPaperSize: ReportPaperSize = 'a4';
  reportZoom = 100;

  // Dynamic System Settings Metadata
  institutionName = 'University of Knowledge (UOK)';
  academicTerm = '2025/2026 - Semester 1';
  systemInfo = 'Campus Services Portal v2.4 • Enterprise Reporting System';
  defaultPageSize = 25;

  // Watermark state
  reportWatermark: 'NONE' | 'CONFIDENTIAL' | 'OFFICIAL COPY' | 'DRAFT' = 'NONE';

  // 1 Master Card per Module Catalog Hub
  moduleCards = [
    {
      id: 'executive',
      moduleName: 'Executive & Strategic Overview',
      category: 'Institutional Strategy',
      icon: '📊',
      description: 'University-wide capacity, occupancy, fee collection totals, and student grievance resolution metrics.',
      badge: 'Executive Hub',
      badgeColor: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300',
      reports: [
        { id: 'kpi' as ReportDomainTab, title: 'Institutional KPI Overview', description: 'High-level capacity, finance & grievance KPIs', icon: '📊' },
      ],
    },
    {
      id: 'academics',
      moduleName: 'Academic Administration & Registry',
      category: 'Academic Administration',
      icon: '👥',
      description: 'Faculty-wise student enrollment distributions, active rosters, and pending profile verification queues.',
      badge: 'Academic Hub',
      badgeColor: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/40 dark:text-indigo-300',
      reports: [
        { id: 'students' as ReportDomainTab, title: 'Student Demographics & Rosters', description: 'Faculty-wise enrollment & active rosters', icon: '👥' },
        { id: 'student-pending' as ReportDomainTab, title: 'Pending Student Registrations', description: 'Unverified admissions & document queues', icon: '📋' },
      ],
    },
    {
      id: 'hostels',
      moduleName: 'Campus Facilities & Hostel Management',
      category: 'Campus Facilities',
      icon: '🏢',
      description: 'Hostel block occupancy rates, floor-by-floor room bed inventories, and pending room allocation applications.',
      badge: 'Facilities Hub',
      badgeColor: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300',
      reports: [
        { id: 'hostels' as ReportDomainTab, title: 'Hostel Occupancy & Capacity Summary', description: 'Block capacities & live occupancy %', icon: '🏢' },
        { id: 'hostel-rooms' as ReportDomainTab, title: 'Hostel Rooms & Bed Inventory', description: 'Floor inventory & vacant bed audit', icon: '🛏️' },
        { id: 'hostel-pending' as ReportDomainTab, title: 'Pending Application Queue', description: 'Student requests awaiting room allocation', icon: '⏳' },
      ],
    },
    {
      id: 'labs',
      moduleName: 'IT Infrastructure & Computer Labs',
      category: 'IT & Facilities',
      icon: '💻',
      description: 'Workstation booking sessions, lab room schedules, workstation seat maps, and daily utilization logs.',
      badge: 'IT Labs Hub',
      badgeColor: 'bg-cyan-100 text-cyan-800 dark:bg-cyan-900/40 dark:text-cyan-300',
      reports: [
        { id: 'labs' as ReportDomainTab, title: 'Lab Workstation Utilization', description: 'Workstation booking sessions & daily logs', icon: '💻' },
        { id: 'lab-directory' as ReportDomainTab, title: 'Lab Directory & Layout Configuration', description: 'Workstation seat maps & hardware directory', icon: '🖥️' },
      ],
    },
    {
      id: 'billing',
      moduleName: 'Finance, Bursar & Student Accounts',
      category: 'Finance & Accounts',
      icon: '💳',
      description: 'Tuition, fees, and fine collection subtotal aggregates and financial transaction ledger audit.',
      badge: 'Financial Hub',
      badgeColor: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300',
      reports: [
        { id: 'billing' as ReportDomainTab, title: 'Billing & Fee Collection Ledger', description: 'Tuition & fee collections (Summary-Detail)', icon: '💳' },
      ],
    },
    {
      id: 'complaints',
      moduleName: 'Student Welfare & Grievance Triage',
      category: 'Student Welfare',
      icon: '📝',
      description: 'Complaint ticket categorizations, priority triage, SLA resolution tracking, and departmental grievance logs.',
      badge: 'Welfare Hub',
      badgeColor: 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300',
      reports: [
        { id: 'complaints' as ReportDomainTab, title: 'Grievance Resolution Triage', description: 'Priority triage & resolution tracking', icon: '📝' },
        { id: 'complaint-categories-sla' as ReportDomainTab, title: 'Complaint Categories & SLA Performance', description: 'Departmental response time compliance', icon: '⏱️' },
      ],
    },
    {
      id: 'certificates',
      moduleName: 'Registry Clearance & Certificates',
      category: 'Registry & Records',
      icon: '📜',
      description: 'Official clearance and certificate issuance tracking, approval workflows, and statutory service catalog.',
      badge: 'Registry Hub',
      badgeColor: 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300',
      reports: [
        { id: 'certificates' as ReportDomainTab, title: 'Certificate Requests & Clearance Logs', description: 'Student certificate requests & verification', icon: '📜' },
        { id: 'certificate-types' as ReportDomainTab, title: 'Certificate Types & Service Catalog', description: 'Statutory fees & turnaround SLAs', icon: '🏷️' },
      ],
    },
    {
      id: 'events',
      moduleName: 'Campus Life & Event Venues',
      category: 'Campus Life',
      icon: '📅',
      description: 'Campus event attendee registrations, participation rates, and auditorium/venue capacity utilization.',
      badge: 'Events Hub',
      badgeColor: 'bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-300',
      reports: [
        { id: 'events' as ReportDomainTab, title: 'Campus Events & Attendee Participation', description: 'Event attendance & registration rosters', icon: '📅' },
        { id: 'venues' as ReportDomainTab, title: 'Event Venues & Facility Utilization', description: 'Auditoriums, halls & seating capacity', icon: '🏛️' },
      ],
    },
    {
      id: 'notifications',
      moduleName: 'System & Security Communication',
      category: 'System Communication',
      icon: '📡',
      description: 'Automated system notification dispatch audit logs, delivery channels, and read rate metrics.',
      badge: 'System Hub',
      badgeColor: 'bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-300',
      reports: [
        { id: 'notifications' as ReportDomainTab, title: 'Notification Dispatch & Audit Trail', description: 'Automated notification dispatch logs', icon: '📡' },
      ],
    },
  ];

  // Grouped Reports for the Left Studio Sidebar (<optgroup>)
  reportGroups = [
    {
      name: '📊 EXECUTIVE & STRATEGY',
      reports: [
        { id: 'kpi' as ReportDomainTab, label: 'Institutional KPI Overview', icon: '📊' },
      ],
    },
    {
      name: '👥 ACADEMIC ADMINISTRATION',
      reports: [
        { id: 'students' as ReportDomainTab, label: 'Student Demographics & Rosters', icon: '👥' },
        { id: 'student-pending' as ReportDomainTab, label: 'Pending Student Registrations', icon: '📋' },
      ],
    },
    {
      name: '🏢 CAMPUS FACILITIES & HOSTELS',
      reports: [
        { id: 'hostels' as ReportDomainTab, label: 'Hostel Occupancy & Capacity Summary', icon: '🏢' },
        { id: 'hostel-rooms' as ReportDomainTab, label: 'Hostel Rooms & Bed Inventory', icon: '🛏️' },
        { id: 'hostel-pending' as ReportDomainTab, label: 'Pending Hostel Application Queue', icon: '⏳' },
      ],
    },
    {
      name: '💻 IT INFRASTRUCTURE & LABS',
      reports: [
        { id: 'labs' as ReportDomainTab, label: 'Lab Workstation Utilization', icon: '💻' },
        { id: 'lab-directory' as ReportDomainTab, label: 'Lab Directory & Layout Configuration', icon: '🖥️' },
      ],
    },
    {
      name: '💳 FINANCE & BURSAR',
      reports: [
        { id: 'billing' as ReportDomainTab, label: 'Billing & Fee Collection Ledger', icon: '💳' },
      ],
    },
    {
      name: '📝 STUDENT WELFARE & GRIEVANCE',
      reports: [
        { id: 'complaints' as ReportDomainTab, label: 'Grievance Resolution Triage', icon: '📝' },
        { id: 'complaint-categories-sla' as ReportDomainTab, label: 'Complaint Categories & SLA Performance', icon: '⏱️' },
      ],
    },
    {
      name: '📜 REGISTRY & RECORDS',
      reports: [
        { id: 'certificates' as ReportDomainTab, label: 'Certificate Requests & Clearance Logs', icon: '📜' },
        { id: 'certificate-types' as ReportDomainTab, label: 'Certificate Types & Service Catalog', icon: '🏷️' },
      ],
    },
    {
      name: '📅 CAMPUS LIFE & EVENTS',
      reports: [
        { id: 'events' as ReportDomainTab, label: 'Campus Events & Attendee Participation', icon: '📅' },
        { id: 'venues' as ReportDomainTab, label: 'Event Venues & Facility Utilization', icon: '🏛️' },
      ],
    },
    {
      name: '📡 SYSTEM & COMMUNICATION',
      reports: [
        { id: 'notifications' as ReportDomainTab, label: 'Notification Dispatch & Audit Trail', icon: '📡' },
      ],
    },
  ];

  tabs: TabItem[] = [
    { id: 'kpi', label: 'Institutional KPI Overview', icon: '📊' },
    { id: 'students', label: 'Student Demographics', icon: '👥' },
    { id: 'student-pending', label: 'Pending Registrations', icon: '📋' },
    { id: 'hostels', label: 'Hostel Occupancy', icon: '🏢' },
    { id: 'hostel-rooms', label: 'Hostel Rooms Inventory', icon: '🛏️' },
    { id: 'hostel-pending', label: 'Pending Applications', icon: '⏳' },
    { id: 'labs', label: 'Lab Utilization', icon: '💻' },
    { id: 'lab-directory', label: 'Lab Directory & Layout', icon: '🖥️' },
    { id: 'billing', label: 'Billing & Fee Ledger', icon: '💳' },
    { id: 'complaints', label: 'Complaints Triage', icon: '📝' },
    { id: 'complaint-categories-sla', label: 'Complaint SLA Performance', icon: '⏱️' },
    { id: 'certificates', label: 'Certificate Requests', icon: '📜' },
    { id: 'certificate-types', label: 'Certificate Types Catalog', icon: '🏷️' },
    { id: 'events', label: 'Events Attendance', icon: '📅' },
    { id: 'venues', label: 'Venues & Facility Utilization', icon: '🏛️' },
    { id: 'notifications', label: 'Notification Dispatch', icon: '📡' },
  ];

  moduleSubTabs: TabItem[] = [
    { id: 'chart', label: 'Visual Charts & Analytics', icon: '📊' },
    { id: 'data', label: 'Data Records & Filter Grid', icon: '📋' },
  ];

  // Filters State
  filter: ReportFilter = {
    pageNumber: 1,
    pageSize: 25,
  };

  // Date Presets (Reusable from DatePresetUtil)
  selectedDatePreset = 'all';
  datePresetOptions: DropdownOption[] = DatePresetUtil.getPresetOptions();

  // Lookups Options
  facultyOptions: MultiSelectOption[] = [];
  hostelOptions: MultiSelectOption[] = [];
  labOptions: MultiSelectOption[] = [];
  feeTypeOptions: MultiSelectOption[] = [];
  complaintCategoryOptions: MultiSelectOption[] = [];
  certificateTypeOptions: MultiSelectOption[] = [];
  eventOptions: MultiSelectOption[] = [];

  // Data Containers
  kpiData?: InstitutionalKpiReport;
  facultySummaries: FacultyStudentSummary[] = [];
  studentItems: StudentReportItem[] = [];
  hostelSummaries: HostelOccupancySummary[] = [];
  unallocatedItems: UnallocatedStudentItem[] = [];
  labSummaries: LabUtilizationSummary[] = [];
  labBookingItems: LabBookingReportItem[] = [];
  feeTypeSummaries: FeeTypeSummary[] = [];
  billingItems: BillingLedgerReportItem[] = [];
  complaintCategorySummaries: ComplaintCategorySummary[] = [];
  complaintItems: ComplaintReportItem[] = [];
  certificateTypeSummaries: CertificateTypeSummary[] = [];
  certificateItems: CertificateReportItem[] = [];
  eventSummaries: EventSummary[] = [];
  eventAttendeeItems: EventAttendeeReportItem[] = [];
  notificationTypeSummaries: NotificationTypeSummary[] = [];
  notificationItems: NotificationReportItem[] = [];

  // New Reports Data Containers
  hostelRoomItems: any[] = [];
  pendingHostelItems: any[] = [];
  labDirectoryItems: any[] = [];
  venueItems: any[] = [];
  pendingStudentItems: any[] = [];
  certificateTypeItems: any[] = [];
  complaintCategorySlaItems: any[] = [];

  // Pagination Metadata
  totalCount = 0;
  totalPages = 1;
  hasPreviousPage = false;
  hasNextPage = false;
  currentGrandTotals: GrandTotalMetric[] = [];
  currentGrandTotalsObj?: Record<string, any>;

  // Subreport Modal State
  isSubreportOpen = false;
  subreportTitle = '';
  subreportParentCategory = '';
  subreportColumns: ReportColumn[] = [];
  subreportItems: any[] = [];
  subreportLoading = false;

  private expandedFacultyIds = new Set<number>();

  // Grid Columns Definitions
  hostelRoomColumns: ReportColumn[] = [
    { header: 'Room #', field: 'roomNumber', align: 'center', sortable: true },
    { header: 'Hostel Name', field: 'hostelName', sortable: true },
    { header: 'Floor', field: 'floorNumber', align: 'center', sortable: true, formatter: (v) => `Floor ${v}` },
    { header: 'Room Type', field: 'roomType', sortable: true },
    { header: 'Capacity', field: 'capacity', align: 'center', sortable: true, formatter: (v) => `${v} Beds` },
    { header: 'Occupied', field: 'occupiedBeds', align: 'center', sortable: true },
    {
      header: 'Available',
      field: 'availableBeds',
      align: 'center',
      sortable: true,
      badgeClass: (v) => (Number(v) > 0 ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'),
    },
    {
      header: 'Status',
      field: 'status',
      align: 'center',
      sortable: true,
      badgeClass: (v) => (v === 'Active' ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'),
    },
    { header: 'Fee / Sem', field: 'feePerSemester', align: 'right', formatter: (v) => `$${Number(v || 0).toFixed(2)}` },
  ];

  pendingHostelColumns: ReportColumn[] = [
    { header: 'App #', field: 'applicationId', align: 'center', sortable: true },
    { header: 'Index #', field: 'indexNumber', sortable: true },
    { header: 'Student Name', field: 'studentName', sortable: true },
    { header: 'Faculty', field: 'facultyName', sortable: true },
    { header: 'Hostel Preference', field: 'preferredHostelName', sortable: true },
    { header: 'Room Type', field: 'requestedRoomType', sortable: true },
    { header: 'Application Date', field: 'applicationDate', align: 'center', sortable: true, formatter: (v) => (v ? v.substring(0, 10) : '') },
    {
      header: 'Payment Status',
      field: 'paymentVerificationStatus',
      align: 'center',
      badgeClass: (v) => (v === 'Verified' ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'),
    },
    {
      header: 'Queue Status',
      field: 'status',
      align: 'center',
      sortable: true,
      badgeClass: () => 'bg-amber-100 text-amber-800',
    },
  ];

  labDirectoryColumns: ReportColumn[] = [
    { header: 'Lab Code', field: 'labCode', align: 'center', sortable: true },
    { header: 'Lab Name', field: 'labName', sortable: true },
    { header: 'Building / Location', field: 'building', sortable: true },
    { header: 'Total Workstations', field: 'totalCapacity', align: 'center', sortable: true },
    { header: 'Operational', field: 'activeOperationalSeats', align: 'center', sortable: true, badgeClass: () => 'bg-emerald-100 text-emerald-800' },
    {
      header: 'Maintenance',
      field: 'maintenanceSeats',
      align: 'center',
      sortable: true,
      badgeClass: (v) => (Number(v) > 0 ? 'bg-rose-100 text-rose-800' : 'bg-slate-100 text-slate-700'),
    },
    { header: 'Supervisor', field: 'supervisorName', sortable: true },
    { header: 'Hours', field: 'operatingHours', align: 'center' },
  ];

  venueColumns: ReportColumn[] = [
    { header: 'Venue Code', field: 'venueCode', align: 'center', sortable: true },
    { header: 'Venue Name', field: 'venueName', sortable: true },
    { header: 'Location', field: 'location', sortable: true },
    { header: 'Venue Type', field: 'venueType', sortable: true },
    { header: 'Capacity', field: 'capacity', align: 'center', sortable: true, formatter: (v) => `${v} Seats` },
    { header: 'Events Hosted', field: 'totalEventsHosted', align: 'center', sortable: true },
    {
      header: 'Status',
      field: 'isActive',
      align: 'center',
      formatter: (v) => (v ? 'Operational' : 'Maintenance'),
      badgeClass: (v) => (v ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'),
    },
  ];

  pendingStudentColumns: ReportColumn[] = [
    { header: 'Index #', field: 'indexNumber', align: 'center', sortable: true },
    { header: 'Full Name', field: 'fullName', sortable: true },
    { header: 'Email', field: 'email', sortable: true },
    { header: 'Faculty', field: 'facultyName', sortable: true },
    { header: 'Admission Date', field: 'admissionDate', align: 'center', sortable: true, formatter: (v) => (v ? v.substring(0, 10) : '') },
    {
      header: 'Verification Status',
      field: 'verificationStatus',
      align: 'center',
      sortable: true,
      badgeClass: (v) => (String(v).includes('Unverified') ? 'bg-amber-100 text-amber-800' : 'bg-rose-100 text-rose-800'),
    },
    { header: 'Pending Requirements', field: 'missingDocuments', sortable: true },
  ];

  certificateTypeColumns: ReportColumn[] = [
    { header: 'Code', field: 'certificateTypeCode', align: 'center', sortable: true },
    { header: 'Certificate Name', field: 'name', sortable: true },
    { header: 'Statutory Fee', field: 'fee', align: 'right', sortable: true, formatter: (v) => `$${Number(v || 0).toFixed(2)}` },
    { header: 'Processing SLA', field: 'processingSlaDays', align: 'center', sortable: true, formatter: (v) => `${v} Days` },
    { header: 'Total Requests', field: 'totalRequestsAllTime', align: 'center', sortable: true },
    { header: 'Approved', field: 'approvedRequestsCount', align: 'center', sortable: true, badgeClass: () => 'bg-emerald-100 text-emerald-800' },
    {
      header: 'Status',
      field: 'isActive',
      align: 'center',
      formatter: (v) => (v ? 'Active' : 'Deprecated'),
      badgeClass: (v) => (v ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'),
    },
  ];

  complaintCategorySlaColumns: ReportColumn[] = [
    { header: 'Code', field: 'categoryCode', align: 'center', sortable: true },
    { header: 'Category Name', field: 'categoryName', sortable: true },
    { header: 'Target SLA', field: 'targetSlaHours', align: 'center', sortable: true, formatter: (v) => `${v} Hours` },
    { header: 'Total Filed', field: 'totalFiled', align: 'center', sortable: true },
    { header: 'Resolved On Time', field: 'resolvedOnTime', align: 'center', sortable: true, badgeClass: () => 'bg-emerald-100 text-emerald-800' },
    {
      header: 'Active Open',
      field: 'activeOpenCount',
      align: 'center',
      sortable: true,
      badgeClass: (v) => (Number(v) > 0 ? 'bg-amber-100 text-amber-800' : 'bg-slate-100 text-slate-700'),
    },
    {
      header: 'SLA Compliance',
      field: 'slaComplianceRate',
      align: 'center',
      sortable: true,
      formatter: (v) => `${Number(v || 0).toFixed(1)}%`,
      badgeClass: (v) => (Number(v) >= 90 ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'),
    },
  ];
  studentColumns: ReportColumn[] = [
    { header: 'Reg No', field: 'registrationNumber', sortable: true },
    { header: 'Full Name', field: 'fullName', sortable: true },
    { header: 'Faculty', field: 'facultyName', sortable: true },
    { header: 'Academic Year', field: 'academicYear', align: 'center', sortable: true, formatter: (v) => `Year ${v}` },
    { header: 'Semester', field: 'semester', align: 'center', sortable: true, formatter: (v) => `Sem ${v}` },
    {
      header: 'Hostel Eligible',
      field: 'isHostelEligible',
      align: 'center',
      formatter: (v) => (v ? 'Eligible' : 'Not Eligible'),
      badgeClass: (v) => (v ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'),
    },
    {
      header: 'Status',
      field: 'isActive',
      align: 'center',
      sortable: true,
      formatter: (v) => (v ? 'Active' : 'Deactivated'),
      badgeClass: (v) => (v ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'),
    },
  ];

  hostelColumns: ReportColumn[] = [
    { header: 'App ID', field: 'applicationId', align: 'center', sortable: true },
    { header: 'Reg / Index #', field: 'indexNumber', sortable: true, formatter: (v, r) => v || r.registrationNumber || '-' },
    { header: 'Student Name', field: 'studentName', sortable: true },
    { header: 'Faculty', field: 'facultyName', sortable: true },
    { header: 'Preferred Hostel', field: 'preferredHostelName', sortable: true, formatter: (v) => v || '-' },
    { header: 'Assigned Room', field: 'assignedRoomNumber', align: 'center', sortable: true, formatter: (v) => v || 'Unassigned' },
    {
      header: 'Status',
      field: 'status',
      align: 'center',
      sortable: true,
      badgeClass: (v) => (v === 'RoomAssigned' || v === 'Approved' ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'),
    },
  ];

  labColumns: ReportColumn[] = [
    { header: 'Booking ID', field: 'bookingId', align: 'center', sortable: true },
    { header: 'Lab Name', field: 'labName', sortable: true },
    { header: 'Seat', field: 'seatNumber', align: 'center', sortable: true },
    { header: 'Student', field: 'studentName', sortable: true },
    { header: 'Date', field: 'sessionDate', align: 'center', sortable: true, formatter: (v) => (v ? v.substring(0, 10) : '') },
    { header: 'Time Slot', field: 'startTime', formatter: (_, r) => `${r.startTime} - ${r.endTime}` },
    {
      header: 'Status',
      field: 'status',
      align: 'center',
      sortable: true,
      badgeClass: (v) => (v === 'Approved' ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'),
    },
  ];

  billingColumns: ReportColumn[] = [
    { header: 'Invoice / Ref', field: 'transactionReference', sortable: true },
    { header: 'Student Name', field: 'studentName', sortable: true },
    { header: 'Fee Type', field: 'feeTypeName', sortable: true },
    { header: 'Amount', field: 'amount', align: 'right', sortable: true, formatter: (v) => `Rs. ${v && !isNaN(Number(v)) ? Number(v).toFixed(2) : '0.00'}` },
    { header: 'Paid', field: 'paidAmount', align: 'right', sortable: true, formatter: (v) => `Rs. ${v && !isNaN(Number(v)) ? Number(v).toFixed(2) : '0.00'}` },
    {
      header: 'Status',
      field: 'paymentStatus',
      align: 'center',
      sortable: true,
      badgeClass: (v) =>
        v === 'Paid'
          ? 'bg-emerald-100 text-emerald-800'
          : v === 'Partial'
          ? 'bg-blue-100 text-blue-800'
          : 'bg-amber-100 text-amber-800',
    },
    { header: 'Due Date', field: 'dueDate', align: 'center', sortable: true, formatter: (v) => (v ? v.substring(0, 10) : '-') },
  ];

  complaintColumns: ReportColumn[] = [
    { header: 'Ticket #', field: 'complaintId', align: 'center', sortable: true },
    { header: 'Category', field: 'categoryName', sortable: true },
    { header: 'Subject', field: 'subject', sortable: true },
    { header: 'Student', field: 'studentName', sortable: true },
    {
      header: 'Priority',
      field: 'priority',
      align: 'center',
      sortable: true,
      badgeClass: (v) =>
        v === 'High' || v === 'Urgent'
          ? 'bg-red-100 text-red-800'
          : v === 'Medium'
          ? 'bg-amber-100 text-amber-800'
          : 'bg-slate-100 text-slate-700',
    },
    {
      header: 'Status',
      field: 'status',
      align: 'center',
      sortable: true,
      badgeClass: (v) =>
        v === 'Resolved'
          ? 'bg-emerald-100 text-emerald-800'
          : v === 'InProgress'
          ? 'bg-blue-100 text-blue-800'
          : 'bg-amber-100 text-amber-800',
    },
    { header: 'Created', field: 'createdAt', align: 'center', sortable: true, formatter: (v) => (v ? v.substring(0, 10) : '') },
  ];

  certificateColumns: ReportColumn[] = [
    { header: 'Req #', field: 'requestId', align: 'center', sortable: true },
    { header: 'Certificate Type', field: 'certificateTypeName', sortable: true },
    { header: 'Student Name', field: 'studentName', sortable: true },
    { header: 'Purpose', field: 'purpose' },
    {
      header: 'Status',
      field: 'status',
      align: 'center',
      sortable: true,
      badgeClass: (v) =>
        v === 'Approved' || v === 'Issued'
          ? 'bg-emerald-100 text-emerald-800'
          : v === 'ReadyForPickup'
          ? 'bg-indigo-100 text-indigo-800'
          : 'bg-amber-100 text-amber-800',
    },
    { header: 'Requested At', field: 'requestedAt', align: 'center', sortable: true, formatter: (v) => (v ? v.substring(0, 10) : '') },
  ];

  eventColumns: ReportColumn[] = [
    { header: 'Reg #', field: 'registrationId', align: 'center', sortable: true },
    { header: 'Event Title', field: 'eventTitle', sortable: true },
    { header: 'Student Name', field: 'studentName', sortable: true },
    { header: 'Registered Date', field: 'registeredAt', align: 'center', sortable: true, formatter: (v) => (v ? v.substring(0, 10) : '') },
    {
      header: 'Attended',
      field: 'attended',
      align: 'center',
      sortable: true,
      formatter: (v) => (v ? 'Attended' : 'Registered'),
      badgeClass: (v) => (v ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-700'),
    },
  ];

  formatNotificationType(type: string): string {
    if (!type) return 'General';
    return type
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/_/g, ' ')
      .trim();
  }

  notificationColumns: ReportColumn[] = [
    { header: 'Log #', field: 'notificationId', align: 'center', sortable: true },
    { header: 'Recipient', field: 'studentName', sortable: true, formatter: (v, r) => v || r.userFullName || r.indexNumber || '-' },
    { header: 'Event Category', field: 'type', sortable: true, formatter: (v) => this.formatNotificationType(v) },
    { header: 'Message / Subject', field: 'message', sortable: true, formatter: (v, r) => v || r.title || '-' },
    {
      header: 'Status',
      field: 'isRead',
      align: 'center',
      sortable: true,
      formatter: (v) => (v ? 'Read' : 'Delivered / Unread'),
      badgeClass: (v) => (v ? 'bg-emerald-100 text-emerald-800' : 'bg-blue-100 text-blue-800'),
    },
    { header: 'Timestamp', field: 'createdAt', align: 'center', sortable: true, formatter: (v) => (v ? v.substring(0, 16).replace('T', ' ') : '') },
  ];

  get activeTabTitle(): string {
    const t = this.tabs.find((x) => x.id === this.activeTab);
    return t ? t.label : 'Institutional Report';
  }

  get activeTabSubtitle(): string {
    switch (this.activeTab) {
      case 'kpi':
        return 'Official institutional key performance indicator metrics and operational overview';
      case 'students':
        return 'Faculty enrollment distribution and student demographics roster';
      case 'student-pending':
        return 'Enrolled students pending document verification and profile clearance';
      case 'hostels':
        return 'Hostel accommodation capacity, occupancy rates, and unallocated students subreport';
      case 'hostel-rooms':
        return 'Room-by-room floor inventory, bed configurations, and occupancy audit';
      case 'hostel-pending':
        return 'Student hostel accommodation requests awaiting room allocation';
      case 'labs':
        return 'Computer laboratory utilization metrics and student workstation booking sessions';
      case 'lab-directory':
        return 'Computer lab workstation configurations, seat layouts, and supervisor directory';
      case 'billing':
        return 'Tuition, fees, and fine collection subtotal aggregates and financial transaction ledger';
      case 'complaints':
        return 'Student grievance triage, category SLA performance, and resolution metrics';
      case 'complaint-categories-sla':
        return 'Student grievance categories, resolution timeframes, and SLA compliance performance';
      case 'certificates':
        return 'Official clearance and certificate issuance tracking and processing logs';
      case 'certificate-types':
        return 'Statutory certificate catalog, fees, and processing turnaround SLAs';
      case 'events':
        return 'Campus event attendee participation and capacity utilization records';
      case 'venues':
        return 'Campus auditoriums, seminar halls, seating capacities, and utilization logs';
      case 'notifications':
        return 'Automated system notification dispatch audit logs and channel delivery metrics';
      default:
        return '';
    }
  }

  get activeFilterChips(): string[] {
    const chips: string[] = [];
    if (this.filter.dateFrom) chips.push(`From: ${this.filter.dateFrom}`);
    if (this.filter.dateTo) chips.push(`To: ${this.filter.dateTo}`);
    if (this.filter.drilldownKey) chips.push(`Category: ${this.formatNotificationType(this.filter.drilldownKey)}`);
    if (this.filter.searchTerm) chips.push(`Search: "${this.filter.searchTerm}"`);
    return chips;
  }

  get currentExportColumns(): ExportColumn[] {
    switch (this.activeTab) {
      case 'students':
        return this.studentColumns;
      case 'student-pending':
        return this.pendingStudentColumns;
      case 'hostels':
        return this.hostelColumns;
      case 'hostel-rooms':
        return this.hostelRoomColumns;
      case 'hostel-pending':
        return this.pendingHostelColumns;
      case 'labs':
        return this.labColumns;
      case 'lab-directory':
        return this.labDirectoryColumns;
      case 'billing':
        return this.billingColumns;
      case 'complaints':
        return this.complaintColumns;
      case 'complaint-categories-sla':
        return this.complaintCategorySlaColumns;
      case 'certificates':
        return this.certificateColumns;
      case 'certificate-types':
        return this.certificateTypeColumns;
      case 'events':
        return this.eventColumns;
      case 'venues':
        return this.venueColumns;
      case 'notifications':
        return this.notificationColumns;
      default:
        return [];
    }
  }

  get currentTableItems(): any[] {
    switch (this.activeTab) {
      case 'students':
        return this.studentItems;
      case 'student-pending':
        return this.pendingStudentItems;
      case 'hostels':
        return this.unallocatedItems;
      case 'hostel-rooms':
        return this.hostelRoomItems;
      case 'hostel-pending':
        return this.pendingHostelItems;
      case 'labs':
        return this.labBookingItems;
      case 'lab-directory':
        return this.labDirectoryItems;
      case 'billing':
        return this.billingItems;
      case 'complaints':
        return this.complaintItems;
      case 'complaint-categories-sla':
        return this.complaintCategorySlaItems;
      case 'certificates':
        return this.certificateItems;
      case 'certificate-types':
        return this.certificateTypeItems;
      case 'events':
        return this.eventAttendeeItems;
      case 'venues':
        return this.venueItems;
      case 'notifications':
        return this.notificationItems;
      default:
        return [];
    }
  }

  get filteredModuleCards() {
    if (!this.searchCatalogTerm.trim()) {
      return this.moduleCards;
    }
    const term = this.searchCatalogTerm.toLowerCase().trim();
    return this.moduleCards.filter(
      (m) =>
        m.moduleName.toLowerCase().includes(term) ||
        m.category.toLowerCase().includes(term) ||
        m.description.toLowerCase().includes(term) ||
        m.reports.some((r) => r.title.toLowerCase().includes(term) || r.description.toLowerCase().includes(term))
    );
  }

  // ==========================================
  // NATIVE ANGULAR SVG CHART ENGINE (ZERO 3RD-PARTY)
  // ==========================================
  readonly donutCircumference = 251.32; // 2 * PI * 40

  // 1. STUDENTS DOMAIN CHARTS
  get facultyAveragePerDepartment(): number {
    if (this.facultySummaries.length === 0) return 0;
    return Math.round(this.totalCount / this.facultySummaries.length);
  }

  get facultyDonutSegments() {
    const total = this.facultySummaries.reduce((sum, f) => sum + (f.totalStudents || 0), 0);
    if (total === 0) return [];
    const colors = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#06b6d4', '#6366f1', '#14b8a6'];
    let accumulatedOffset = 0;
    return this.facultySummaries.map((fac, idx) => {
      const pct = (fac.totalStudents || 0) / total;
      const dash = pct * this.donutCircumference;
      const segment = {
        name: fac.facultyName,
        count: fac.totalStudents,
        active: fac.activeStudents,
        eligible: fac.hostelEligibleCount,
        percentage: Math.round(pct * 100),
        color: colors[idx % colors.length],
        strokeDasharray: `${dash} ${this.donutCircumference}`,
        strokeDashoffset: accumulatedOffset,
      };
      accumulatedOffset -= dash;
      return segment;
    });
  }

  // 2. HOSTELS DOMAIN CHARTS
  get hostelOccupancyStats() {
    const totalBeds = this.hostelSummaries.reduce((s, h) => s + (h.totalBedsCapacity || 0), 0);
    const occupied = this.hostelSummaries.reduce((s, h) => s + (h.occupiedBeds || 0), 0);
    const available = this.hostelSummaries.reduce((s, h) => s + (h.availableBeds || 0), 0);
    const rate = totalBeds > 0 ? Math.round((occupied / totalBeds) * 100) : 0;
    const dash = (rate / 100) * this.donutCircumference;
    return {
      totalBeds,
      occupied,
      available,
      rate,
      dasharray: `${dash} ${this.donutCircumference}`,
      blocks: this.hostelSummaries.map((h) => ({
        ...h,
        id: h.hostelId,
        name: h.hostelName,
        occupied: h.occupiedBeds,
        capacity: h.totalBedsCapacity,
        available: h.availableBeds,
        rate: h.occupancyRate,
        barColor: h.occupancyRate >= 90 ? '#ef4444' : h.occupancyRate >= 70 ? '#f59e0b' : '#10b981',
      })),
    };
  }

  // 3. LABS DOMAIN CHARTS
  get labUtilizationStats() {
    const totalLabs = this.labSummaries.length;
    const totalBookings = this.labSummaries.reduce((s, l) => s + (l.totalBookings || 0), 0);
    const approvedBookings = this.labSummaries.reduce((s, l) => s + (l.approvedBookings || 0), 0);
    const rate = totalBookings > 0 ? Math.round((approvedBookings / totalBookings) * 100) : 0;
    const dash = (rate / 100) * this.donutCircumference;
    return {
      totalLabs,
      totalBookings,
      approvedBookings,
      rate,
      dasharray: `${dash} ${this.donutCircumference}`,
      labs: this.labSummaries.map((l) => ({
        id: l.labId,
        name: l.labName,
        building: l.building,
        floor: l.floor,
        totalBookings: l.totalBookings,
        approvedBookings: l.approvedBookings,
        percentage: totalBookings > 0 ? Math.round((l.totalBookings / totalBookings) * 100) : 0,
      })),
    };
  }

  // 4. BILLING & FINANCE DOMAIN CHARTS
  get billingLedgerDistribution() {
    const billed = this.feeTypeSummaries.reduce((s, f) => s + (Number(f.totalBilledAmount) || 0), 0) ||
      Number(this.currentGrandTotals.find((t) => t.label === 'Total Invoiced')?.value || 0);
    const paid = this.feeTypeSummaries.reduce((s, f) => s + (Number(f.totalPaidAmount) || 0), 0) ||
      Number(this.currentGrandTotals.find((t) => t.label === 'Total Collected')?.value || 0);
    const pending = Math.max(0, billed - paid);
    const total = billed > 0 ? billed : (paid + pending);
    const paidPct = total > 0 ? Math.round((paid / total) * 100) : 0;
    const pendingPct = total > 0 ? (100 - paidPct) : 0;
    const paidDash = (paidPct / 100) * this.donutCircumference;
    const pendingDash = (pendingPct / 100) * this.donutCircumference;
    return {
      total,
      paid,
      pending,
      paidPct,
      pendingPct,
      paidDasharray: `${paidDash} ${this.donutCircumference}`,
      pendingDasharray: `${pendingDash} ${this.donutCircumference}`,
      pendingOffset: -paidDash,
      feeTypes: this.feeTypeSummaries.map((f) => ({
        id: f.feeTypeId,
        name: f.feeTypeName,
        billed: f.totalBilledAmount,
        paid: f.totalPaidAmount,
        collectionRate: f.totalBilledAmount > 0 ? Math.round((f.totalPaidAmount / f.totalBilledAmount) * 100) : 0,
      })),
    };
  }

  // 5. COMPLAINTS & GRIEVANCES DOMAIN CHARTS
  get complaintSlaStats() {
    const total = this.complaintCategorySummaries.reduce((s, c) => s + (c.totalComplaints || 0), 0) || this.totalCount || 0;
    const resolved = this.complaintCategorySummaries.reduce((s, c) => s + (c.resolvedCount || 0), 0) ||
      Number(this.currentGrandTotals.find((t) => t.label === 'Resolved Tickets')?.value || 0);
    const rate = total > 0 ? Math.round((resolved / total) * 100) : 0;
    const dash = (rate / 100) * this.donutCircumference;
    return {
      total,
      resolved,
      pending: Math.max(0, total - resolved),
      rate,
      dasharray: `${dash} ${this.donutCircumference}`,
      categories: this.complaintCategorySummaries.map((c) => ({
        id: c.categoryId,
        name: c.categoryName,
        total: c.totalComplaints,
        resolved: c.resolvedCount,
        resolutionRate: c.resolutionRate,
      })),
    };
  }

  // 6. CERTIFICATES DOMAIN CHARTS
  get certificateSlaStats() {
    const total = this.certificateTypeSummaries.reduce((s, c) => s + (c.totalRequests || 0), 0) || this.totalCount || 0;
    const approved = this.certificateTypeSummaries.reduce((s, c) => s + (c.approvedCount || 0), 0);
    const pending = this.certificateTypeSummaries.reduce((s, c) => s + (c.pendingCount || 0), 0);
    const rate = total > 0 ? Math.round((approved / total) * 100) : 0;
    const dash = (rate / 100) * this.donutCircumference;
    return {
      total,
      approved,
      pending,
      rate,
      dasharray: `${dash} ${this.donutCircumference}`,
      types: this.certificateTypeSummaries.map((c) => ({
        id: c.certificateTypeId,
        name: c.certificateTypeName,
        total: c.totalRequests,
        approved: c.approvedCount,
        pending: c.pendingCount,
        percentage: total > 0 ? Math.round((c.totalRequests / total) * 100) : 0,
      })),
    };
  }

  // 7. EVENTS & VENUES DOMAIN CHARTS
  get eventAttendanceStats() {
    const total = this.eventSummaries.reduce((s, e) => s + (e.maxCapacity || 0), 0);
    const registered = this.eventSummaries.reduce((s, e) => s + (e.registeredAttendees || 0), 0);
    const rate = total > 0 ? Math.round((registered / total) * 100) : 0;
    const dash = (rate / 100) * this.donutCircumference;
    return {
      totalEvents: this.eventSummaries.length,
      maxCapacity: total,
      registeredAttendees: registered,
      rate,
      dasharray: `${dash} ${this.donutCircumference}`,
      events: this.eventSummaries.map((e) => ({
        id: e.eventId,
        title: e.title,
        venue: e.venueName,
        registered: e.registeredAttendees,
        capacity: e.maxCapacity,
        rate: e.capacityUtilizationRate,
      })),
    };
  }

  // 8. NOTIFICATIONS DOMAIN CHARTS
  get notificationEngagementStats() {
    const total = this.notificationTypeSummaries.reduce((s, n) => s + (n.totalSent || 0), 0) || this.totalCount || 0;
    const read = this.notificationTypeSummaries.reduce((s, n) => s + (n.readCount || 0), 0);
    const unread = Math.max(0, total - read);
    const rate = total > 0 ? Math.round((read / total) * 100) : 0;
    const readDash = (rate / 100) * this.donutCircumference;
    return {
      total,
      read,
      unread,
      rate,
      readDasharray: `${readDash} ${this.donutCircumference}`,
      types: this.notificationTypeSummaries.map((n) => ({
        ...n,
        type: n.type || n.notificationType || '',
        notificationType: n.type || n.notificationType || '',
        formattedType: this.formatNotificationType(n.type || n.notificationType || ''),
        totalSent: n.totalSent,
        readCount: n.readCount,
        unreadCount: n.unreadCount !== undefined ? n.unreadCount : Math.max(0, n.totalSent - n.readCount),
        readRate: n.readRate,
      })),
    };
  }

  // 6b. CERTIFICATES BAR & LINE CHARTS
  get certificateBarChart() {
    const maxVal = Math.max(1, ...this.certificateTypeSummaries.map((c) => c.totalRequests || 0));
    const colors = ['#8b5cf6', '#7c3aed', '#6d28d9', '#5b21b6', '#4c1d95', '#a78bfa'];
    return this.certificateTypeSummaries.map((c, idx) => ({
      name: c.certificateTypeName,
      total: c.totalRequests || 0,
      approved: c.approvedCount || 0,
      pending: c.pendingCount || 0,
      totalHeight: Math.round(((c.totalRequests || 0) / maxVal) * 100),
      approvedHeight: Math.round(((c.approvedCount || 0) / maxVal) * 100),
      rate: (c.totalRequests || 0) > 0 ? Math.round(((c.approvedCount || 0) / (c.totalRequests || 0)) * 100) : 0,
      color: colors[idx % colors.length],
    }));
  }

  get certificateLineChart() {
    const items = this.certificateTypeSummaries;
    if (!items || items.length === 0) return { path: '', areaPath: '', points: [] };
    const width = 500; const height = 150; const paddingX = 40; const paddingY = 20;
    const usableW = width - paddingX * 2; const usableH = height - paddingY * 2;
    const maxVal = Math.max(1, ...items.map((c) => c.totalRequests || 0));
    const stepX = items.length > 1 ? usableW / (items.length - 1) : usableW / 2;
    const points = items.map((c, i) => {
      const x = items.length > 1 ? paddingX + i * stepX : width / 2;
      const y = height - paddingY - ((c.totalRequests || 0) / maxVal) * usableH;
      return { x, y, name: c.certificateTypeName, total: c.totalRequests || 0, approved: c.approvedCount || 0 };
    });
    const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    const areaPath = `${path} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingY} L ${points[0].x.toFixed(1)} ${height - paddingY} Z`;
    return { path, areaPath, points };
  }

  // 7b. EVENTS BAR & LINE CHARTS
  get eventBarChart() {
    const maxVal = Math.max(1, ...this.eventSummaries.map((e) => e.maxCapacity || 0));
    return this.eventSummaries.map((e) => ({
      name: e.title ? (e.title.length > 12 ? e.title.substring(0, 12) + '…' : e.title) : 'Event',
      fullName: e.title,
      registered: e.registeredAttendees || 0,
      capacity: e.maxCapacity || 0,
      registeredHeight: Math.round(((e.registeredAttendees || 0) / maxVal) * 100),
      capacityHeight: Math.round(((e.maxCapacity || 0) / maxVal) * 100),
      rate: e.capacityUtilizationRate || 0,
    }));
  }

  get eventLineChart() {
    const items = this.eventSummaries;
    if (!items || items.length === 0) return { path: '', areaPath: '', points: [] };
    const width = 500; const height = 150; const paddingX = 40; const paddingY = 20;
    const usableW = width - paddingX * 2; const usableH = height - paddingY * 2;
    const stepX = items.length > 1 ? usableW / (items.length - 1) : usableW / 2;
    const points = items.map((e, i) => {
      const x = items.length > 1 ? paddingX + i * stepX : width / 2;
      const y = height - paddingY - (Math.min(100, e.capacityUtilizationRate || 0) / 100) * usableH;
      return { x, y, name: e.title, rate: e.capacityUtilizationRate || 0, registered: e.registeredAttendees || 0, capacity: e.maxCapacity || 0 };
    });
    const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    const areaPath = `${path} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingY} L ${points[0].x.toFixed(1)} ${height - paddingY} Z`;
    return { path, areaPath, points };
  }

  // 8b. NOTIFICATIONS BAR & LINE CHARTS
  get notificationBarChart() {
    const maxVal = Math.max(1, ...this.notificationTypeSummaries.map((n) => n.totalSent || 0));
    const colors = ['#3b82f6', '#06b6d4', '#8b5cf6', '#10b981', '#f59e0b', '#ec4899'];
    return this.notificationTypeSummaries.map((n, idx) => ({
      name: this.formatNotificationType(n.type || n.notificationType || ''),
      totalSent: n.totalSent || 0,
      readCount: n.readCount || 0,
      sentHeight: Math.round(((n.totalSent || 0) / maxVal) * 100),
      readHeight: Math.round(((n.readCount || 0) / maxVal) * 100),
      readRate: n.readRate || 0,
      color: colors[idx % colors.length],
    }));
  }

  get notificationLineChart() {
    const items = this.notificationTypeSummaries;
    if (!items || items.length === 0) return { path: '', areaPath: '', points: [] };
    const width = 500; const height = 150; const paddingX = 40; const paddingY = 20;
    const usableW = width - paddingX * 2; const usableH = height - paddingY * 2;
    const stepX = items.length > 1 ? usableW / (items.length - 1) : usableW / 2;
    const points = items.map((n, i) => {
      const rate = n.readRate || 0;
      const x = items.length > 1 ? paddingX + i * stepX : width / 2;
      const y = height - paddingY - (Math.min(100, rate) / 100) * usableH;
      return { x, y, name: this.formatNotificationType(n.type || n.notificationType || ''), rate, totalSent: n.totalSent || 0, readCount: n.readCount || 0 };
    });
    const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    const areaPath = `${path} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingY} L ${points[0].x.toFixed(1)} ${height - paddingY} Z`;
    return { path, areaPath, points };
  }

  // ══════════════════════════════════════════════════════════════════════════
  // TYPED CHART DATA GETTERS — Feed into <app-analytics-chart>
  // Returns ChartDataPoint[] and ChartConfig objects per domain.
  // ══════════════════════════════════════════════════════════════════════════

  // ── 1. STUDENTS ──────────────────────────────────────────────────────────
  get studentDonutData(): ChartDataPoint[] {
    return this.facultySummaries.map((f, idx) => ({
      label: f.facultyName,
      value: f.totalStudents || 0,
    }));
  }
  get studentDonutConfig(): ChartConfig {
    return { type: 'donut', title: 'Faculty Share', icon: '🥧',
      primaryColor: '#3b82f6', centerLabel: `${this.totalCount}`, centerSub: 'Enrolled',
      subtitle: 'Proportional enrollment per faculty' };
  }
  get studentBarData(): ChartDataPoint[] {
    return this.facultySummaries.map(f => ({
      label: f.facultyName,
      value: f.totalStudents || 0,
      value2: f.activeStudents || 0,
    }));
  }
  get studentBarConfig(): ChartConfig {
    return { type: 'bar', title: 'Total vs Active', icon: '📊',
      primaryColor: '#3b82f6', secondaryColor: '#10b981',
      legendItems: [{ label: 'Total Enrolled', color: '#3b82f6' }, { label: 'Active', color: '#10b981' }],
      subtitle: 'Faculty-wise enrolled vs active students' };
  }
  get studentLineData(): ChartDataPoint[] {
    return this.facultySummaries.map(f => ({
      label: f.facultyName, value: f.totalStudents || 0,
    }));
  }
  get studentLineConfig(): ChartConfig {
    return { type: 'line', title: 'Enrollment Trend', icon: '📈',
      primaryColor: '#3b82f6', legendItems: [{ label: 'Students', color: '#3b82f6' }],
      subtitle: 'Distribution across departments' };
  }

  // ── 2. HOSTELS ───────────────────────────────────────────────────────────
  get hostelDonutData(): ChartDataPoint[] {
    return [
      { label: 'Occupied', value: this.hostelOccupancyStats.occupied, color: '#10b981' },
      { label: 'Available', value: this.hostelOccupancyStats.available, color: '#e2e8f0' },
    ];
  }
  get hostelDonutConfig(): ChartConfig {
    return { type: 'donut', title: 'Capacity Gauge', icon: '🥧',
      primaryColor: '#10b981', gaugePercent: this.hostelOccupancyStats.rate,
      centerLabel: `${this.hostelOccupancyStats.rate}%`, centerSub: 'Occupied' };
  }
  get hostelBarData(): ChartDataPoint[] {
    return this.hostelSummaries.map(h => ({
      label: h.hostelName,
      value: h.totalBedsCapacity || 0,
      value2: h.occupiedBeds || 0,
    }));
  }
  get hostelBarConfig(): ChartConfig {
    return { type: 'bar', title: 'Block Capacity', icon: '📊',
      primaryColor: '#94a3b8', secondaryColor: '#10b981', stacked: true,
      legendItems: [{ label: 'Total Beds', color: '#94a3b8' }, { label: 'Occupied', color: '#10b981' }],
      subtitle: 'Occupied beds within total capacity' };
  }
  get hostelLineData(): ChartDataPoint[] {
    return this.hostelSummaries.map(h => ({
      label: h.hostelName, value: h.occupancyRate || 0,
    }));
  }
  get hostelLineConfig(): ChartConfig {
    return { type: 'line', title: 'Occupancy Curve', icon: '📈',
      primaryColor: '#10b981', valueSuffix: '%',
      legendItems: [{ label: 'Occupancy %', color: '#10b981' }],
      subtitle: 'Block-by-block occupancy rate' };
  }

  // ── 3. LABS ───────────────────────────────────────────────────────────────
  get labDonutData(): ChartDataPoint[] {
    return [
      { label: 'Approved', value: this.labUtilizationStats.approvedBookings, color: '#06b6d4' },
      { label: 'Pending', value: this.labUtilizationStats.totalBookings - this.labUtilizationStats.approvedBookings, color: '#e2e8f0' },
    ];
  }
  get labDonutConfig(): ChartConfig {
    return { type: 'donut', title: 'Approval Gauge', icon: '🥧',
      primaryColor: '#06b6d4', gaugePercent: this.labUtilizationStats.rate,
      centerLabel: `${this.labUtilizationStats.rate}%`, centerSub: 'Approved' };
  }
  get labBarData(): ChartDataPoint[] {
    return this.labSummaries.map(l => ({
      label: l.labName, value: l.totalBookings || 0, value2: l.approvedBookings || 0,
    }));
  }
  get labBarConfig(): ChartConfig {
    return { type: 'bar', title: 'Total vs Approved', icon: '📊',
      primaryColor: '#06b6d4', secondaryColor: '#2563eb',
      legendItems: [{ label: 'Total', color: '#06b6d4' }, { label: 'Approved', color: '#2563eb' }],
      subtitle: 'Booking volume per lab' };
  }
  get labLineData(): ChartDataPoint[] {
    return this.labSummaries.map(l => ({ label: l.labName, value: l.totalBookings || 0 }));
  }
  get labLineConfig(): ChartConfig {
    return { type: 'line', title: 'Demand Curve', icon: '📈',
      primaryColor: '#06b6d4',
      legendItems: [{ label: 'Bookings', color: '#06b6d4' }],
      subtitle: 'Workstation demand per lab' };
  }

  // ── 4. BILLING ───────────────────────────────────────────────────────────
  get billingDonutData(): ChartDataPoint[] {
    return [
      { label: 'Paid', value: this.billingLedgerDistribution.paid, color: '#10b981' },
      { label: 'Outstanding', value: this.billingLedgerDistribution.pending, color: '#f59e0b' },
    ];
  }
  get billingDonutConfig(): ChartConfig {
    return { type: 'donut', title: 'Collection Split', icon: '🥧',
      primaryColor: '#10b981', gaugePercent: this.billingLedgerDistribution.paidPct,
      centerLabel: `${this.billingLedgerDistribution.paidPct}%`, centerSub: 'Paid',
      legendItems: [
        { label: 'Paid', color: '#10b981' },
        { label: 'Outstanding', color: '#f59e0b' },
      ] };
  }
  get billingBarData(): ChartDataPoint[] {
    return this.feeTypeSummaries.map(f => ({
      label: f.feeTypeName,
      value: Math.round(Number(f.totalBilledAmount) || 0),
      value2: Math.round(Number(f.totalPaidAmount) || 0),
    }));
  }
  get billingBarConfig(): ChartConfig {
    return { type: 'bar', title: 'Billed vs Paid', icon: '📊',
      primaryColor: '#6366f1', secondaryColor: '#10b981',
      legendItems: [{ label: 'Billed', color: '#6366f1' }, { label: 'Paid', color: '#10b981' }],
      subtitle: 'Fee category revenue comparison' };
  }
  get billingLineData(): ChartDataPoint[] {
    return this.feeTypeSummaries.map(f => ({
      label: f.feeTypeName, value: Math.round(Number(f.totalBilledAmount) || 0),
    }));
  }
  get billingLineConfig(): ChartConfig {
    return { type: 'line', title: 'Revenue Trend', icon: '📈',
      primaryColor: '#4f46e5',
      legendItems: [{ label: 'Billed', color: '#4f46e5' }],
      subtitle: 'Invoiced revenue curve per category' };
  }

  // ── 5. COMPLAINTS ─────────────────────────────────────────────────────────
  get complaintDonutData(): ChartDataPoint[] {
    return [
      { label: 'Resolved', value: this.complaintSlaStats.resolved, color: '#10b981' },
      { label: 'Pending', value: this.complaintSlaStats.pending, color: '#f59e0b' },
    ];
  }
  get complaintDonutConfig(): ChartConfig {
    return { type: 'donut', title: 'SLA Gauge', icon: '🥧',
      primaryColor: '#10b981', gaugePercent: this.complaintSlaStats.rate,
      centerLabel: `${this.complaintSlaStats.rate}%`, centerSub: 'Resolved',
      legendItems: [{ label: 'Resolved', color: '#10b981' }, { label: 'Pending', color: '#f59e0b' }] };
  }
  get complaintBarData(): ChartDataPoint[] {
    return this.complaintCategorySummaries.map(c => ({
      label: c.categoryName, value: c.totalComplaints || 0, value2: c.resolvedCount || 0,
    }));
  }
  get complaintBarConfig(): ChartConfig {
    return { type: 'bar', title: 'By Category', icon: '📊',
      primaryColor: '#94a3b8', secondaryColor: '#f59e0b', stacked: true,
      legendItems: [{ label: 'Total', color: '#94a3b8' }, { label: 'Resolved', color: '#f59e0b' }],
      subtitle: 'Category ticket vs resolution count' };
  }
  get complaintLineData(): ChartDataPoint[] {
    return this.complaintCategorySummaries.map(c => ({
      label: c.categoryName, value: Math.min(100, c.resolutionRate || 0),
    }));
  }
  get complaintLineConfig(): ChartConfig {
    return { type: 'line', title: 'SLA Curve', icon: '📈',
      primaryColor: '#f59e0b', valueSuffix: '%',
      legendItems: [{ label: '% Compliance', color: '#f59e0b' }],
      subtitle: 'Category SLA resolution compliance' };
  }

  // ── 6. CERTIFICATES ───────────────────────────────────────────────────────
  get certDonutData(): ChartDataPoint[] {
    return [
      { label: 'Approved', value: this.certificateSlaStats.approved, color: '#8b5cf6' },
      { label: 'Pending', value: this.certificateSlaStats.pending, color: '#f59e0b' },
    ];
  }
  get certDonutConfig(): ChartConfig {
    return { type: 'donut', title: 'Approval Rate', icon: '🥧',
      primaryColor: '#8b5cf6', gaugePercent: this.certificateSlaStats.rate,
      centerLabel: `${this.certificateSlaStats.rate}%`, centerSub: 'Approved',
      legendItems: [{ label: 'Approved', color: '#8b5cf6' }, { label: 'Pending', color: '#f59e0b' }] };
  }
  get certBarData(): ChartDataPoint[] {
    return this.certificateTypeSummaries.map(c => ({
      label: c.certificateTypeName, value: c.totalRequests || 0, value2: c.approvedCount || 0,
    }));
  }
  get certBarConfig(): ChartConfig {
    return { type: 'bar', title: 'Total vs Approved', icon: '📊',
      primaryColor: '#8b5cf6', secondaryColor: '#10b981',
      legendItems: [{ label: 'Requests', color: '#8b5cf6' }, { label: 'Approved', color: '#10b981' }],
      subtitle: 'Certificate type request vs issuance' };
  }
  get certLineData(): ChartDataPoint[] {
    return this.certificateTypeSummaries.map(c => ({
      label: c.certificateTypeName, value: c.totalRequests || 0,
    }));
  }
  get certLineConfig(): ChartConfig {
    return { type: 'line', title: 'Demand Trend', icon: '📈',
      primaryColor: '#8b5cf6',
      legendItems: [{ label: 'Requests', color: '#8b5cf6' }],
      subtitle: 'Certificate demand by type' };
  }

  // ── 7. EVENTS ────────────────────────────────────────────────────────────
  get eventDonutData(): ChartDataPoint[] {
    return [
      { label: 'Registered', value: this.eventAttendanceStats.registeredAttendees, color: '#ec4899' },
      { label: 'Available', value: Math.max(0, this.eventAttendanceStats.maxCapacity - this.eventAttendanceStats.registeredAttendees), color: '#e2e8f0' },
    ];
  }
  get eventDonutConfig(): ChartConfig {
    return { type: 'donut', title: 'Seat Utilization', icon: '🥧',
      primaryColor: '#ec4899', gaugePercent: this.eventAttendanceStats.rate,
      centerLabel: `${this.eventAttendanceStats.rate}%`, centerSub: 'Booked',
      legendItems: [{ label: 'Registered', color: '#ec4899' }, { label: 'Available', color: '#e2e8f0' }] };
  }
  get eventBarData(): ChartDataPoint[] {
    return this.eventSummaries.map(e => ({
      label: e.title ? (e.title.length > 12 ? e.title.substring(0, 12) + '…' : e.title) : 'Event',
      value: e.maxCapacity || 0,
      value2: e.registeredAttendees || 0,
      tooltip: `${e.title}: ${e.registeredAttendees}/${e.maxCapacity} seats`,
    }));
  }
  get eventBarConfig(): ChartConfig {
    return { type: 'bar', title: 'Registered vs Capacity', icon: '📊',
      primaryColor: '#94a3b8', secondaryColor: '#ec4899', stacked: true,
      legendItems: [{ label: 'Capacity', color: '#94a3b8' }, { label: 'Registered', color: '#ec4899' }],
      subtitle: 'Attendee registration vs venue seats' };
  }
  get eventLineData(): ChartDataPoint[] {
    return this.eventSummaries.map(e => ({
      label: e.title, value: Math.min(100, e.capacityUtilizationRate || 0),
    }));
  }
  get eventLineConfig(): ChartConfig {
    return { type: 'line', title: 'Utilization Curve', icon: '📈',
      primaryColor: '#ec4899', valueSuffix: '%',
      legendItems: [{ label: 'Utilization %', color: '#ec4899' }],
      subtitle: 'Venue utilization rate per event' };
  }

  // ── 8. NOTIFICATIONS ─────────────────────────────────────────────────────
  get notifDonutData(): ChartDataPoint[] {
    return [
      { label: 'Read', value: this.notificationEngagementStats.read, color: '#3b82f6' },
      { label: 'Unread', value: this.notificationEngagementStats.unread, color: '#e2e8f0' },
    ];
  }
  get notifDonutConfig(): ChartConfig {
    return { type: 'donut', title: 'Read Rate', icon: '🥧',
      primaryColor: '#3b82f6', gaugePercent: this.notificationEngagementStats.rate,
      centerLabel: `${this.notificationEngagementStats.rate}%`, centerSub: 'Read Rate',
      legendItems: [{ label: 'Read', color: '#3b82f6' }, { label: 'Unread', color: '#94a3b8' }] };
  }
  get notifBarData(): ChartDataPoint[] {
    return this.notificationTypeSummaries.map(n => ({
      label: this.formatNotificationType(n.type || n.notificationType || ''),
      value: n.totalSent || 0,
      value2: n.readCount || 0,
    }));
  }
  get notifBarConfig(): ChartConfig {
    return { type: 'bar', title: 'Sent vs Read', icon: '📊',
      primaryColor: '#93c5fd', secondaryColor: '#1d4ed8',
      legendItems: [{ label: 'Sent', color: '#93c5fd' }, { label: 'Read', color: '#1d4ed8' }],
      subtitle: 'Channel dispatch vs read count' };
  }
  get notifLineData(): ChartDataPoint[] {
    return this.notificationTypeSummaries.map(n => ({
      label: this.formatNotificationType(n.type || n.notificationType || ''),
      value: Math.min(100, n.readRate || 0),
    }));
  }
  get notifLineConfig(): ChartConfig {
    return { type: 'line', title: 'Engagement Curve', icon: '📈',
      primaryColor: '#3b82f6', valueSuffix: '%',
      legendItems: [{ label: 'Read Rate', color: '#3b82f6' }],
      subtitle: 'Channel engagement read rate' };
  }

  // Chart Display Mode Selector ('all' | 'bar' | 'line' | 'pie')
  chartDisplayMode = signal<'all' | 'bar' | 'line' | 'pie'>('all');


  setChartDisplayMode(mode: 'all' | 'bar' | 'line' | 'pie') {
    this.chartDisplayMode.set(mode);
  }

  // 1. STUDENTS BAR & LINE CHARTS
  get facultyBarChart() {
    const maxVal = Math.max(1, ...this.facultySummaries.map((f) => f.totalStudents || 0));
    const colors = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#06b6d4', '#6366f1', '#14b8a6'];
    return this.facultySummaries.map((fac, idx) => ({
      name: fac.facultyName,
      count: fac.totalStudents || 0,
      active: fac.activeStudents || 0,
      heightPercent: Math.round(((fac.totalStudents || 0) / maxVal) * 100),
      color: colors[idx % colors.length],
    }));
  }

  get facultyLineChart() {
    const items = this.facultySummaries;
    if (!items || items.length === 0) return { path: '', areaPath: '', points: [], maxVal: 0 };
    const maxVal = Math.max(1, ...items.map((f) => f.totalStudents || 0));
    const width = 500;
    const height = 150;
    const paddingX = 40;
    const paddingY = 20;
    const usableW = width - paddingX * 2;
    const usableH = height - paddingY * 2;
    const stepX = items.length > 1 ? usableW / (items.length - 1) : usableW / 2;

    const points = items.map((f, i) => {
      const x = items.length > 1 ? paddingX + i * stepX : width / 2;
      const y = height - paddingY - ((f.totalStudents || 0) / maxVal) * usableH;
      return { x, y, name: f.facultyName, count: f.totalStudents || 0 };
    });

    const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    const areaPath = `${path} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingY} L ${points[0].x.toFixed(1)} ${height - paddingY} Z`;

    return { path, areaPath, points, maxVal };
  }

  // 2. HOSTELS BAR & LINE CHARTS
  get hostelBarChart() {
    const maxVal = Math.max(1, ...this.hostelSummaries.map((h) => h.totalBedsCapacity || 0));
    return this.hostelSummaries.map((h) => ({
      name: h.hostelName,
      occupied: h.occupiedBeds || 0,
      capacity: h.totalBedsCapacity || 0,
      occupiedPercent: Math.round(((h.occupiedBeds || 0) / maxVal) * 100),
      capacityPercent: Math.round(((h.totalBedsCapacity || 0) / maxVal) * 100),
      rate: h.occupancyRate || 0,
    }));
  }

  get hostelLineChart() {
    const items = this.hostelSummaries;
    if (!items || items.length === 0) return { path: '', areaPath: '', points: [] };
    const width = 500;
    const height = 150;
    const paddingX = 40;
    const paddingY = 20;
    const usableW = width - paddingX * 2;
    const usableH = height - paddingY * 2;
    const stepX = items.length > 1 ? usableW / (items.length - 1) : usableW / 2;

    const points = items.map((h, i) => {
      const x = items.length > 1 ? paddingX + i * stepX : width / 2;
      const y = height - paddingY - (Math.min(100, h.occupancyRate || 0) / 100) * usableH;
      return { x, y, name: h.hostelName, rate: h.occupancyRate || 0, occupied: h.occupiedBeds, capacity: h.totalBedsCapacity };
    });

    const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    const areaPath = `${path} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingY} L ${points[0].x.toFixed(1)} ${height - paddingY} Z`;

    return { path, areaPath, points };
  }

  // 3. LABS BAR & LINE CHARTS
  get labBarChart() {
    const maxVal = Math.max(1, ...this.labSummaries.map((l) => l.totalBookings || 0));
    return this.labSummaries.map((l) => ({
      name: l.labName,
      total: l.totalBookings || 0,
      approved: l.approvedBookings || 0,
      totalHeight: Math.round(((l.totalBookings || 0) / maxVal) * 100),
      approvedHeight: Math.round(((l.approvedBookings || 0) / maxVal) * 100),
    }));
  }

  get labLineChart() {
    const items = this.labSummaries;
    if (!items || items.length === 0) return { path: '', areaPath: '', points: [], maxBookings: 0 };
    const width = 500;
    const height = 150;
    const paddingX = 40;
    const paddingY = 20;
    const usableW = width - paddingX * 2;
    const usableH = height - paddingY * 2;
    const stepX = items.length > 1 ? usableW / (items.length - 1) : usableW / 2;
    const maxBookings = Math.max(1, ...items.map((l) => l.totalBookings || 0));

    const points = items.map((l, i) => {
      const x = items.length > 1 ? paddingX + i * stepX : width / 2;
      const y = height - paddingY - ((l.totalBookings || 0) / maxBookings) * usableH;
      return { x, y, name: l.labName, bookings: l.totalBookings || 0, approved: l.approvedBookings || 0 };
    });

    const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    const areaPath = `${path} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingY} L ${points[0].x.toFixed(1)} ${height - paddingY} Z`;

    return { path, areaPath, points, maxBookings };
  }

  // 4. BILLING BAR & LINE CHARTS
  get billingBarChart() {
    const maxVal = Math.max(1, ...this.feeTypeSummaries.map((f) => Number(f.totalBilledAmount) || 0));
    return this.feeTypeSummaries.map((f) => ({
      name: f.feeTypeName,
      billed: Number(f.totalBilledAmount) || 0,
      paid: Number(f.totalPaidAmount) || 0,
      billedHeight: Math.round(((Number(f.totalBilledAmount) || 0) / maxVal) * 100),
      paidHeight: Math.round(((Number(f.totalPaidAmount) || 0) / maxVal) * 100),
      rate: Number(f.totalBilledAmount) > 0 ? Math.round((Number(f.totalPaidAmount) / Number(f.totalBilledAmount)) * 100) : 0,
    }));
  }

  get billingLineChart() {
    const items = this.feeTypeSummaries;
    if (!items || items.length === 0) return { path: '', areaPath: '', points: [], maxBilled: 0 };
    const width = 500;
    const height = 150;
    const paddingX = 40;
    const paddingY = 20;
    const usableW = width - paddingX * 2;
    const usableH = height - paddingY * 2;
    const stepX = items.length > 1 ? usableW / (items.length - 1) : usableW / 2;
    const maxBilled = Math.max(1, ...items.map((f) => Number(f.totalBilledAmount) || 0));

    const points = items.map((f, i) => {
      const billed = Number(f.totalBilledAmount) || 0;
      const x = items.length > 1 ? paddingX + i * stepX : width / 2;
      const y = height - paddingY - (billed / maxBilled) * usableH;
      return { x, y, name: f.feeTypeName, billed, paid: Number(f.totalPaidAmount) || 0 };
    });

    const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    const areaPath = `${path} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingY} L ${points[0].x.toFixed(1)} ${height - paddingY} Z`;

    return { path, areaPath, points, maxBilled };
  }

  // 5. COMPLAINTS BAR & LINE CHARTS
  get complaintBarChart() {
    const maxVal = Math.max(1, ...this.complaintCategorySummaries.map((c) => c.totalComplaints || 0));
    return this.complaintCategorySummaries.map((c) => ({
      name: c.categoryName,
      total: c.totalComplaints || 0,
      resolved: c.resolvedCount || 0,
      totalHeight: Math.round(((c.totalComplaints || 0) / maxVal) * 100),
      resolvedHeight: Math.round(((c.resolvedCount || 0) / maxVal) * 100),
      rate: c.resolutionRate || 0,
    }));
  }

  get complaintLineChart() {
    const items = this.complaintCategorySummaries;
    if (!items || items.length === 0) return { path: '', areaPath: '', points: [] };
    const width = 500;
    const height = 150;
    const paddingX = 40;
    const paddingY = 20;
    const usableW = width - paddingX * 2;
    const usableH = height - paddingY * 2;
    const stepX = items.length > 1 ? usableW / (items.length - 1) : usableW / 2;

    const points = items.map((c, i) => {
      const x = items.length > 1 ? paddingX + i * stepX : width / 2;
      const y = height - paddingY - (Math.min(100, c.resolutionRate || 0) / 100) * usableH;
      return { x, y, name: c.categoryName, rate: c.resolutionRate || 0, total: c.totalComplaints, resolved: c.resolvedCount };
    });

    const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
    const areaPath = `${path} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingY} L ${points[0].x.toFixed(1)} ${height - paddingY} Z`;

    return { path, areaPath, points };
  }

  ngOnInit(): void {
    this.loadSystemSettings();
    this.loadLookups();

    // Check if on /admin/analytics route
    const currentUrl = this.router.url;
    const isAnalytics = currentUrl.includes('/admin/analytics');
    this.isAnalyticsRoute.set(isAnalytics);

    if (isAnalytics) {
      this.isReportModalOpen.set(true);
    }

    this.route.params.subscribe((params) => {
      const tabParam = params['tab'] as ReportDomainTab;
      if (tabParam && this.tabs.some((t) => t.id === tabParam)) {
        this.activeTab = tabParam;
        this.isReportModalOpen.set(true);
      } else if (this.isAnalyticsRoute()) {
        this.activeTab = 'kpi';
        this.isReportModalOpen.set(true);
      }
      this.loadReportData();
    });
  }

  openReportModal(tabId: ReportDomainTab): void {
    this.activeTab = tabId;
    this.filter.pageNumber = 1;
    this.filter.drilldownKey = undefined;
    this.filter.drilldownId = undefined;
    this.isReportModalOpen.set(true);
    const base = this.isAnalyticsRoute() ? '/admin/analytics' : '/admin/reports';
    this.router.navigate([base, tabId]);
    this.loadReportData();
  }

  closeReportModal(): void {
    if (this.isAnalyticsRoute()) {
      this.router.navigate(['/admin/dashboard']);
    } else {
      this.isReportModalOpen.set(false);
      this.router.navigate(['/admin/reports']);
    }
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed.set(!this.isSidebarCollapsed());
  }

  onPrintReport(): void {
    window.print();
  }

  onExportPdfReport(): void {
    window.print();
  }

  onExportExcelReport(): void {
    const filename = `${this.activeTab}_analytics_${new Date().toISOString().substring(0, 10)}`;
    const title = `${this.institutionName} - ${this.activeTabTitle}`;
    this.reportExportService.exportToExcel(
      filename,
      title,
      this.currentExportColumns,
      this.currentTableItems,
      this.currentGrandTotalsObj
    );
    this.toastService.success('Excel analytics dataset generated and downloaded');
  }

  onExportWordReport(): void {
    const filename = `${this.activeTab}_report_${new Date().toISOString().substring(0, 10)}`;
    const title = `${this.institutionName} - ${this.activeTabTitle}`;
    this.reportExportService.exportToWord(
      filename,
      title,
      this.currentExportColumns,
      this.currentTableItems
    );
    this.toastService.success('Word document generated and downloaded');
  }

  onExportCsvReport(): void {
    const filename = `${this.activeTab}_data_${new Date().toISOString().substring(0, 10)}`;
    const title = `${this.institutionName} - ${this.activeTabTitle}`;
    this.reportExportService.exportToCsv(
      filename,
      this.currentExportColumns,
      this.currentTableItems,
      title
    );
    this.toastService.success('CSV dataset exported successfully');
  }

  loadSystemSettings(): void {
    this.systemSettingsService.getAllSettings().subscribe({
      next: (res) => {
        if (res.data) {
          const s = res.data;
          if (s['InstitutionName']) {
            this.institutionName = s['InstitutionName'];
          }
          const yr = s['AcademicYear'] || '2025/2026';
          const sem = s['Semester'] || 'Semester 1';
          this.academicTerm = `${yr} - ${sem}`;

          if (s['SystemVersion']) {
            this.systemInfo = `${s['SystemVersion']} • Institutional Analytics`;
          }
          if (s['DefaultPageSize']) {
            const parsedSize = parseInt(s['DefaultPageSize'], 10);
            if (!isNaN(parsedSize) && parsedSize > 0) {
              this.defaultPageSize = parsedSize;
              this.filter.pageSize = parsedSize;
            }
          }
        }
      },
      error: () => {},
    });
  }

  selectTab(tab: string | ReportDomainTab): void {
    const target = tab as ReportDomainTab;
    if (this.activeTab === target) return;
    this.activeTab = target;
    this.filter.pageNumber = 1;
    this.filter.drilldownKey = undefined;
    this.filter.drilldownId = undefined;
    const base = this.isAnalyticsRoute() ? '/admin/analytics' : '/admin/reports';
    this.router.navigate([base, target]);
    this.loadReportData();
  }

  toggleCategoryFilter(categoryKey?: string): void {
    if (!categoryKey || this.filter.drilldownKey === categoryKey) {
      this.filter.drilldownKey = undefined;
    } else {
      this.filter.drilldownKey = categoryKey;
    }
    this.filter.pageNumber = 1;
    this.loadReportData();
  }

  openNotificationSubreport(n: NotificationTypeSummary): void {
    const rawType = n.type || n.notificationType || '';
    const formattedTitle = this.formatNotificationType(rawType);
    this.subreportTitle = `Notification Audit Roster: ${formattedTitle}`;
    this.subreportParentCategory = formattedTitle;
    this.subreportColumns = this.notificationColumns;
    this.subreportLoading = true;
    this.isSubreportOpen = true;

    this.reportService
      .getNotificationReport({
        drilldownKey: rawType,
        pageSize: 100,
        pageNumber: 1,
      })
      .subscribe({
        next: (res) => {
          this.subreportItems = res.data.items || [];
          this.subreportLoading = false;
        },
        error: () => {
          this.toastService.error('Failed to load category subreport');
          this.subreportLoading = false;
        },
      });
  }

  openHostelSubreport(h: HostelOccupancySummary): void {
    this.subreportTitle = `Hostel Roster & Allocations: ${h.hostelName}`;
    this.subreportParentCategory = h.hostelName;
    this.subreportColumns = this.hostelColumns;
    this.subreportLoading = true;
    this.isSubreportOpen = true;

    this.reportService
      .getHostelReport({
        drilldownId: h.hostelId,
        pageSize: 100,
        pageNumber: 1,
      })
      .subscribe({
        next: (res) => {
          this.subreportItems = res.data.items || [];
          this.subreportLoading = false;
        },
        error: () => {
          this.toastService.error('Failed to load hostel subreport');
          this.subreportLoading = false;
        },
      });
  }

  openLabSubreport(l: LabUtilizationSummary): void {
    this.subreportTitle = `Workstation Booking Roster: ${l.labName}`;
    this.subreportParentCategory = l.labName;
    this.subreportColumns = this.labColumns;
    this.subreportLoading = true;
    this.isSubreportOpen = true;

    this.reportService
      .getLabReport({
        drilldownId: l.labId,
        pageSize: 100,
        pageNumber: 1,
      })
      .subscribe({
        next: (res) => {
          this.subreportItems = res.data.items || [];
          this.subreportLoading = false;
        },
        error: () => {
          this.toastService.error('Failed to load lab subreport');
          this.subreportLoading = false;
        },
      });
  }

  toggleIdFilter(id?: number | string): void {
    if (!id || this.filter.drilldownId == id) {
      this.filter.drilldownId = undefined;
    } else {
      this.filter.drilldownId = id;
    }
    this.filter.pageNumber = 1;
    this.loadReportData();
  }

  loadLookups(): void {
    this.reportService.getFilterLookups().subscribe({
      next: (data: ReportFilterLookups) => {
        this.facultyOptions = data.faculties.map((f) => ({ id: f.id, label: f.name }));
        this.hostelOptions = data.hostels.map((h) => ({ id: h.id, label: h.name }));
        this.labOptions = data.labs.map((l) => ({ id: l.id, label: l.name }));
        this.feeTypeOptions = data.feeTypes.map((ft) => ({ id: ft.id, label: ft.name }));
        this.complaintCategoryOptions = data.complaintCategories.map((c) => ({ id: c.id, label: c.name }));
        this.certificateTypeOptions = data.certificateTypes.map((ct) => ({ id: ct.id, label: ct.name }));
        this.eventOptions = data.events.map((e) => ({ id: e.id, label: e.name }));
      },
      error: () => {},
    });
  }

  loadReportData(): void {
    this.loading.set(true);
    this.reportGeneratedDate = new Date();

    switch (this.activeTab) {
      case 'kpi':
        this.reportService.getKpiSummary(this.filter).subscribe({
          next: (res) => {
            this.kpiData = res.data;
            const occ = this.kpiData?.occupancyRate ?? (this.kpiData as any)?.OccupancyRate ?? 0;
            const totStudents = this.kpiData?.totalStudents ?? (this.kpiData as any)?.TotalStudents ?? 0;
            const paid = this.kpiData?.totalPaidAmount ?? (this.kpiData as any)?.TotalPaidAmount ?? 0;
            const resolved = this.kpiData?.resolvedComplaints ?? (this.kpiData as any)?.ResolvedComplaints ?? 0;
            this.currentGrandTotals = [
              { label: 'Total Students', value: totStudents },
              { label: 'Revenue Collected', value: paid, isCurrency: true },
              { label: 'Occupancy Rate', value: `${Number(occ).toFixed(1)}%` },
              { label: 'Resolved Complaints', value: resolved },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load KPI summary report');
            this.loading.set(false);
          },
        });
        break;

      case 'students':
        this.reportService.getStudentReport(this.filter).subscribe({
          next: (res) => {
            const sumData = res.data.summaryData as any;
            this.facultySummaries = sumData?.facultySummaries || (Array.isArray(sumData) ? sumData : []);
            this.studentItems = res.data.items || [];
            this.setPagination(res.data);
            this.currentGrandTotals = [
              { label: 'Total Students', value: sumData?.grandTotalStudents ?? res.data.grandTotals?.['TotalStudents'] ?? this.totalCount },
              { label: 'Active Students', value: sumData?.grandTotalActive ?? res.data.grandTotals?.['ActiveStudents'] ?? 0 },
              { label: 'Hostel Eligible', value: res.data.grandTotals?.['HostelEligible'] ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load student report');
            this.loading.set(false);
          },
        });
        break;

      case 'student-pending':
        this.reportService.getPendingStudentRegistrationsReport(this.filter).subscribe({
          next: (res) => {
            this.pendingStudentItems = res.data.items || [];
            this.setPagination(res.data);
            const totals = res.data.grandTotals as any;
            this.currentGrandTotals = [
              { label: 'Pending Registrations', value: totals?.grandTotalPendingRegistrations ?? this.totalCount },
              { label: 'Unverified Emails', value: totals?.unverifiedEmailCount ?? 0 },
              { label: 'Missing Documents', value: totals?.missingDocumentsCount ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load pending student registrations');
            this.loading.set(false);
          },
        });
        break;

      case 'hostels':
        this.reportService.getHostelReport(this.filter).subscribe({
          next: (res) => {
            const sumData = res.data.summaryData as any;
            const list = sumData?.hostelSummaries || (Array.isArray(sumData) ? sumData : []);
            this.hostelSummaries = list.map((h: any) => ({
              ...h,
              totalBedsCapacity: h.totalBedCapacity ?? h.totalBedsCapacity ?? 0,
            }));
            this.unallocatedItems = res.data.items || [];
            this.setPagination(res.data);
            this.currentGrandTotals = [
              { label: 'Total Beds Capacity', value: sumData?.grandTotalBeds ?? res.data.grandTotals?.['TotalCapacity'] ?? 0 },
              { label: 'Occupied Beds', value: sumData?.grandTotalOccupied ?? res.data.grandTotals?.['TotalOccupied'] ?? 0 },
              { label: 'Available Beds', value: sumData?.grandTotalAvailable ?? 0 },
              { label: 'Unallocated Applicants', value: this.totalCount },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load hostel report');
            this.loading.set(false);
          },
        });
        break;

      case 'hostel-rooms':
        this.reportService.getHostelRoomsReport(this.filter).subscribe({
          next: (res) => {
            this.hostelRoomItems = res.data.items || [];
            this.setPagination(res.data);
            const totals = res.data.grandTotals as any;
            this.currentGrandTotals = [
              { label: 'Total Rooms', value: totals?.grandTotalRooms ?? this.totalCount },
              { label: 'Total Beds', value: totals?.grandTotalBeds ?? 0 },
              { label: 'Occupied Beds', value: totals?.grandTotalOccupied ?? 0 },
              { label: 'Vacant Beds', value: totals?.grandTotalVacant ?? 0 },
              { label: 'Occupancy Rate', value: `${Number(totals?.overallOccupancyPercentage ?? 0).toFixed(1)}%` },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load hostel rooms report');
            this.loading.set(false);
          },
        });
        break;

      case 'hostel-pending':
        this.reportService.getPendingHostelApplicationsReport(this.filter).subscribe({
          next: (res) => {
            this.pendingHostelItems = res.data.items || [];
            this.setPagination(res.data);
            const totals = res.data.grandTotals as any;
            this.currentGrandTotals = [
              { label: 'Pending Applications', value: totals?.totalPendingApplications ?? this.totalCount },
              { label: 'Allocated This Term', value: totals?.totalAllocatedThisTerm ?? 0 },
              { label: 'Oldest Pending (Days)', value: totals?.oldestPendingDays ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load pending hostel applications');
            this.loading.set(false);
          },
        });
        break;

      case 'labs':
        this.reportService.getLabReport(this.filter).subscribe({
          next: (res) => {
            const sumData = res.data.summaryData as any;
            this.labSummaries = sumData?.labSummaries || (Array.isArray(sumData) ? sumData : []);
            this.labBookingItems = res.data.items || [];
            this.setPagination(res.data);
            this.currentGrandTotals = [
              { label: 'Total Workstations', value: sumData?.grandTotalCapacity ?? res.data.grandTotals?.['TotalSeats'] ?? 0 },
              { label: 'Active Workstations', value: sumData?.grandTotalCapacity ?? res.data.grandTotals?.['ActiveSeats'] ?? 0 },
              { label: 'Total Booking Sessions', value: this.totalCount },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load lab report');
            this.loading.set(false);
          },
        });
        break;

      case 'lab-directory':
        this.reportService.getLabDirectoryReport(this.filter).subscribe({
          next: (res) => {
            this.labDirectoryItems = res.data.items || [];
            this.setPagination(res.data);
            const totals = res.data.grandTotals as any;
            this.currentGrandTotals = [
              { label: 'Total Labs', value: totals?.grandTotalLabs ?? this.totalCount },
              { label: 'Total Workstations', value: totals?.grandTotalWorkstations ?? 0 },
              { label: 'Operational Workstations', value: totals?.operationalWorkstations ?? 0 },
              { label: 'Operational %', value: `${Number(totals?.operationalPercentage ?? 100).toFixed(1)}%` },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load lab directory report');
            this.loading.set(false);
          },
        });
        break;

      case 'billing':
        this.reportService.getBillingReport(this.filter).subscribe({
          next: (res) => {
            const sumData = res.data.summaryData as any;
            this.feeTypeSummaries = sumData?.feeTypeSummaries || (Array.isArray(sumData) ? sumData : []);
            this.billingItems = res.data.items || [];
            this.setPagination(res.data);
            this.currentGrandTotals = [
              { label: 'Total Invoiced', value: sumData?.grandTotalBilled ?? res.data.grandTotals?.['TotalBilled'] ?? 0, isCurrency: true },
              { label: 'Total Collected', value: sumData?.grandTotalPaid ?? res.data.grandTotals?.['TotalPaid'] ?? 0, isCurrency: true },
              { label: 'Pending Collections', value: sumData?.grandTotalPending ?? res.data.grandTotals?.['TotalPending'] ?? 0, isCurrency: true },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load billing report');
            this.loading.set(false);
          },
        });
        break;

      case 'complaints':
        this.reportService.getComplaintReport(this.filter).subscribe({
          next: (res) => {
            const sumData = res.data.summaryData as any;
            this.complaintCategorySummaries = sumData?.categorySummaries || (Array.isArray(sumData) ? sumData : []);
            this.complaintItems = res.data.items || [];
            this.setPagination(res.data);
            this.currentGrandTotals = [
              { label: 'Total Grievance Tickets', value: this.totalCount },
              { label: 'Resolved Tickets', value: sumData?.grandTotalResolved ?? res.data.grandTotals?.['TotalResolved'] ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load complaint report');
            this.loading.set(false);
          },
        });
        break;

      case 'complaint-categories-sla':
        this.reportService.getComplaintCategoriesSlaReport(this.filter).subscribe({
          next: (res) => {
            this.complaintCategorySlaItems = res.data.items || [];
            this.setPagination(res.data);
            const totals = res.data.grandTotals as any;
            this.currentGrandTotals = [
              { label: 'Complaint Categories', value: totals?.grandTotalCategories ?? this.totalCount },
              { label: 'Total Grievances Logged', value: totals?.totalGrievancesLogged ?? 0 },
              { label: 'Overall SLA Compliance', value: `${Number(totals?.overallSlaCompliancePercentage ?? 100).toFixed(1)}%` },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load complaint categories SLA report');
            this.loading.set(false);
          },
        });
        break;

      case 'certificates':
        this.reportService.getCertificateReport(this.filter).subscribe({
          next: (res) => {
            const sumData = res.data.summaryData as any;
            this.certificateTypeSummaries = sumData?.typeSummaries || (Array.isArray(sumData) ? sumData : []);
            this.certificateItems = res.data.items || [];
            this.setPagination(res.data);
            this.currentGrandTotals = [
              { label: 'Total Requests', value: this.totalCount },
              { label: 'Approved & Issued', value: sumData?.grandTotalApproved ?? res.data.grandTotals?.['TotalApproved'] ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load certificate report');
            this.loading.set(false);
          },
        });
        break;

      case 'certificate-types':
        this.reportService.getCertificateTypesCatalogReport(this.filter).subscribe({
          next: (res) => {
            this.certificateTypeItems = res.data.items || [];
            this.setPagination(res.data);
            const totals = res.data.grandTotals as any;
            this.currentGrandTotals = [
              { label: 'Certificate Types', value: totals?.grandTotalCertificateTypes ?? this.totalCount },
              { label: 'Average Fee', value: totals?.averageFee ?? 0, isCurrency: true },
              { label: 'Average SLA', value: `${totals?.averageSlaDays ?? 3} Days` },
              { label: 'Total Requests YTD', value: totals?.totalRequestsProcessed ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load certificate types report');
            this.loading.set(false);
          },
        });
        break;

      case 'events':
        this.reportService.getEventReport(this.filter).subscribe({
          next: (res) => {
            const sumData = res.data.summaryData as any;
            this.eventSummaries = sumData?.eventSummaries || (Array.isArray(sumData) ? sumData : []);
            this.eventAttendeeItems = res.data.items || [];
            this.setPagination(res.data);
            this.currentGrandTotals = [
              { label: 'Total Event Attendees', value: this.totalCount },
              { label: 'Attended Records', value: sumData?.grandTotalAttended ?? res.data.grandTotals?.['TotalAttended'] ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load event report');
            this.loading.set(false);
          },
        });
        break;

      case 'venues':
        this.reportService.getVenueUtilizationReport(this.filter).subscribe({
          next: (res) => {
            this.venueItems = res.data.items || [];
            this.setPagination(res.data);
            const totals = res.data.grandTotals as any;
            this.currentGrandTotals = [
              { label: 'Total Venues', value: totals?.grandTotalVenues ?? this.totalCount },
              { label: 'Total Seating Capacity', value: totals?.grandTotalSeatingCapacity ?? 0 },
              { label: 'Events Hosted YTD', value: totals?.totalEventsHostedYtd ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load venue utilization report');
            this.loading.set(false);
          },
        });
        break;

      case 'notifications':
        this.reportService.getNotificationReport(this.filter).subscribe({
          next: (res) => {
            const sumData = res.data.summaryData as any;
            this.notificationTypeSummaries = sumData?.typeSummaries || (Array.isArray(sumData) ? sumData : []);
            this.notificationItems = res.data.items || [];
            this.setPagination(res.data);
            this.currentGrandTotals = [
              { label: 'Total Dispatched Logs', value: this.totalCount },
              { label: 'Read Messages', value: sumData?.grandTotalRead ?? res.data.grandTotals?.['TotalRead'] ?? 0 },
            ];
            this.loading.set(false);
          },
          error: () => {
            this.toastService.error('Failed to load notification report');
            this.loading.set(false);
          },
        });
        break;
    }
  }

  private setPagination(paged: any): void {
    this.totalCount = paged.totalCount;
    this.totalPages = paged.totalPages || 1;
    this.hasPreviousPage = paged.hasPreviousPage;
    this.hasNextPage = paged.hasNextPage;
    this.currentGrandTotalsObj = paged.grandTotals;
  }

  get availableSortOptions(): { label: string; value: string }[] {
    switch (this.activeTab) {
      case 'students':
        return [
          { label: 'Default Order', value: '' },
          { label: 'Registration Number', value: 'registrationNumber' },
          { label: 'Student Name', value: 'fullName' },
          { label: 'Faculty', value: 'facultyName' },
          { label: 'Academic Year', value: 'academicYear' },
        ];
      case 'hostels':
        return [
          { label: 'Default Order', value: '' },
          { label: 'Application ID', value: 'applicationId' },
          { label: 'Distance from Campus', value: 'distanceFromCampusKm' },
          { label: 'Family Income', value: 'annualFamilyIncome' },
          { label: 'Application Status', value: 'status' },
        ];
      case 'labs':
        return [
          { label: 'Default Order', value: '' },
          { label: 'Booking ID', value: 'bookingId' },
          { label: 'Session Date', value: 'sessionDate' },
          { label: 'Seat Number', value: 'seatNumber' },
          { label: 'Booking Status', value: 'status' },
        ];
      case 'billing':
        return [
          { label: 'Default Order', value: '' },
          { label: 'Invoice / Ref', value: 'transactionReference' },
          { label: 'Student Name', value: 'studentName' },
          { label: 'Total Amount', value: 'amount' },
          { label: 'Paid Amount', value: 'paidAmount' },
          { label: 'Payment Status', value: 'paymentStatus' },
          { label: 'Due Date', value: 'dueDate' },
        ];
      case 'complaints':
        return [
          { label: 'Default Order', value: '' },
          { label: 'Ticket ID', value: 'complaintId' },
          { label: 'Priority Level', value: 'priority' },
          { label: 'Complaint Status', value: 'status' },
          { label: 'Date Logged', value: 'createdAt' },
        ];
      case 'certificates':
        return [
          { label: 'Default Order', value: '' },
          { label: 'Request ID', value: 'requestId' },
          { label: 'Certificate Type', value: 'certificateTypeName' },
          { label: 'Student Name', value: 'studentName' },
          { label: 'Request Status', value: 'status' },
          { label: 'Date Requested', value: 'createdAt' },
        ];
      case 'events':
        return [
          { label: 'Default Order', value: '' },
          { label: 'Registration ID', value: 'registrationId' },
          { label: 'Student Name', value: 'studentName' },
          { label: 'Date Registered', value: 'registeredAt' },
          { label: 'Attendance Status', value: 'attended' },
        ];
      case 'notifications':
        return [
          { label: 'Default Order', value: '' },
          { label: 'Log ID', value: 'notificationId' },
          { label: 'Recipient Name', value: 'userFullName' },
          { label: 'Channel Type', value: 'type' },
          { label: 'Date Dispatched', value: 'createdAt' },
          { label: 'Read Status', value: 'isRead' },
        ];
      default:
        return [
          { label: 'Default Order', value: '' },
          { label: 'Date / Time', value: 'createdAt' },
        ];
    }
  }

  get currentSortDirection(): 'asc' | 'desc' {
    return this.filter.sortDirection === 'desc' ? 'desc' : 'asc';
  }

  setModuleSubTab(tab: 'chart' | 'data'): void {
    this.activeModuleSubTab.set(tab);
  }

  toggleSortDirection(): void {
    this.filter.sortDirection = this.filter.sortDirection === 'desc' ? 'asc' : 'desc';
    this.applyFilters();
  }

  onDatePresetChange(preset: string): void {
    this.selectedDatePreset = preset;
    if (preset === 'all') {
      this.filter.dateFrom = undefined;
      this.filter.dateTo = undefined;
    } else if (preset !== 'custom') {
      const range = DatePresetUtil.calculateDateRange(preset);
      this.filter.dateFrom = range.fromDate;
      this.filter.dateTo = range.toDate;
    }
    this.applyFilters();
  }

  onSortChange(event: { sortBy: string; sortDirection: 'asc' | 'desc' }): void {
    this.filter.sortBy = event.sortBy;
    this.filter.sortDirection = event.sortDirection;
    this.filter.pageNumber = 1;
    this.loadReportData();
  }

  applyFilters(): void {
    this.filter.pageNumber = 1;
    this.loadReportData();
  }

  resetFilters(): void {
    this.selectedDatePreset = 'all';
    this.filter = {
      pageNumber: 1,
      pageSize: this.defaultPageSize || 25,
      sortBy: undefined,
      sortDirection: 'asc',
    };
    this.loadReportData();
  }

  onPageChange(page: number): void {
    this.filter.pageNumber = page;
    this.loadReportData();
  }

  onPageSizeChange(size: number): void {
    this.filter.pageSize = size;
    this.filter.pageNumber = 1;
    this.loadReportData();
  }

  drilldownFaculty(fac: FacultyStudentSummary): void {
    this.subreportTitle = `Enrolled Students - ${fac.facultyName}`;
    this.subreportParentCategory = fac.facultyName;
    this.subreportColumns = this.studentColumns;
    this.subreportLoading = true;
    this.isSubreportOpen = true;

    this.reportService
      .getStudentReport({
        facultyIds: [fac.facultyId],
        pageSize: 100,
        pageNumber: 1,
      })
      .subscribe({
        next: (res) => {
          this.subreportItems = res.data.items || [];
          this.subreportLoading = false;
        },
        error: () => {
          this.toastService.error('Failed to load faculty students subreport');
          this.subreportLoading = false;
        },
      });
  }

  onStudentDrilldown(student: StudentReportItem): void {
    this.subreportTitle = `Student Academic Profile: ${student.fullName} (${student.registrationNumber})`;
    this.subreportParentCategory = student.facultyName;
    this.subreportColumns = [
      { header: 'Attribute', field: 'key' },
      { header: 'Value', field: 'val' },
    ];
    this.subreportItems = [
      { key: 'Registration Number', val: student.registrationNumber },
      { key: 'Full Name', val: student.fullName },
      { key: 'University Email', val: student.email },
      { key: 'Faculty', val: student.facultyName },
      { key: 'Current Academic Year', val: `Year ${student.academicYear}` },
      { key: 'Current Semester', val: `Semester ${student.semester}` },
      { key: 'Hostel Eligibility', val: student.isHostelEligible ? 'Eligible' : 'Not Eligible' },
      { key: 'Account Status', val: student.isActive ? 'Active' : 'Deactivated' },
      { key: 'Enrolled On', val: student.createdAt ? student.createdAt.substring(0, 10) : '-' },
    ];
    this.subreportLoading = false;
    this.isSubreportOpen = true;
  }

  isFacultyExpanded(facultyId: number): boolean {
    if (this.expandedFacultyIds.size === 0) {
      return true;
    }
    return this.expandedFacultyIds.has(facultyId);
  }

  toggleFacultyExpansion(facultyId: number): void {
    if (this.expandedFacultyIds.size === 0) {
      this.facultySummaries.forEach((f) => this.expandedFacultyIds.add(f.facultyId));
    }
    if (this.expandedFacultyIds.has(facultyId)) {
      this.expandedFacultyIds.delete(facultyId);
    } else {
      this.expandedFacultyIds.add(facultyId);
    }
  }

  getStudentsForFaculty(facultyId: number): StudentReportItem[] {
    return this.studentItems.filter((s) => s.facultyId === facultyId);
  }
}
