export type ReportDomainTab =
  | 'kpi'
  | 'students'
  | 'student-pending'
  | 'hostels'
  | 'hostel-rooms'
  | 'hostel-pending'
  | 'labs'
  | 'lab-directory'
  | 'billing'
  | 'complaints'
  | 'complaint-categories-sla'
  | 'certificates'
  | 'certificate-types'
  | 'events'
  | 'venues'
  | 'notifications';

export interface ReportTabItem {
  id: ReportDomainTab;
  label: string;
  icon: string;
  moduleGroup?: string;
}

export interface ReportModuleCard {
  id: string;
  moduleName: string;
  category: string;
  icon: string;
  description: string;
  badge: string;
  badgeColor: string;
  reports: {
    id: ReportDomainTab;
    title: string;
    description: string;
    icon: string;
  }[];
}

