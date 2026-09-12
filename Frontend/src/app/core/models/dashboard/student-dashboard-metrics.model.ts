import { Notification } from '../system/notification.model';

export interface StudentDashboardMetricsSummary {
  hostelStatus: string;
  activeLabBookings: number;
  registeredEvents: number;
  outstandingFees: number;
  certificateStatus: string;
  complaintStatus: string;
  recentNotifications: Notification[];
}
