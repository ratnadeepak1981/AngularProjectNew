// Institutional KPI Overview
export interface InstitutionalKpiReport {
  totalStudents: number;
  activeStudents: number;
  deactivatedStudents: number;
  totalHostels: number;
  totalRooms: number;
  totalBedsCapacity: number;
  allocatedBeds: number;
  occupancyRate: number;
  totalLabs: number;
  totalWorkstations: number;
  activeBookingsToday: number;
  totalBilledAmount: number;
  totalPaidAmount: number;
  collectionRate: number;
  totalComplaints: number;
  pendingComplaints: number;
  inProgressComplaints: number;
  resolvedComplaints: number;
  totalCertificateRequests: number;
  pendingCertificateRequests: number;
  approvedCertificateRequests: number;
  totalEvents: number;
  activeEvents: number;
  totalEventAttendees: number;
  totalNotificationsSent: number;
  unreadNotifications: number;
  generatedAt: string;
}

// Student Demographics
export interface FacultyStudentSummary {
  facultyId: number;
  facultyName: string;
  totalStudents: number;
  activeStudents: number;
  deactivatedStudents: number;
  hostelEligibleCount: number;
}

export interface StudentReportItem {
  studentId: number;
  registrationNumber: string;
  fullName: string;
  email: string;
  facultyId: number;
  facultyName: string;
  academicYear: number;
  semester: number;
  isHostelEligible: boolean;
  isActive: boolean;
  createdAt: string;
}

// Hostel Occupancy
export interface HostelOccupancySummary {
  hostelId: number;
  hostelName: string;
  gender?: string;
  totalRooms: number;
  totalBedCapacity?: number;
  totalBedsCapacity: number;
  occupiedBeds: number;
  availableBeds: number;
  occupancyRate: number;
}

export interface UnallocatedStudentItem {
  applicationId: number;
  studentId: number;
  registrationNumber: string;
  studentName: string;
  email: string;
  facultyName: string;
  preferredHostelName: string;
  distanceFromCampusKm: number;
  annualFamilyIncome: number;
  hasSpecialNeeds: boolean;
  status: string;
  applicationDate: string;
}

// Lab Utilization
export interface LabUtilizationSummary {
  labId: number;
  labName: string;
  building: string;
  floor: number;
  totalSeats: number;
  activeSeats: number;
  brokenSeats: number;
  totalBookings: number;
  approvedBookings: number;
}

export interface LabBookingReportItem {
  bookingId: number;
  studentId: number;
  registrationNumber: string;
  studentName: string;
  labName: string;
  seatNumber: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  purpose: string;
  status: string;
}

// Billing & Financial Ledger
export interface FeeTypeSummary {
  feeTypeId: number;
  feeTypeName: string;
  category: string;
  totalBilledAmount: number;
  totalPaidAmount: number;
  totalPendingAmount: number;
  totalTransactionsCount: number;
}

export interface BillingLedgerReportItem {
  paymentId: number;
  studentId: number;
  registrationNumber: string;
  studentName: string;
  feeTypeName: string;
  feeCategory: string;
  amount: number;
  paidAmount: number;
  paymentStatus: string;
  paymentMethod: string;
  transactionReference: string;
  dueDate: string;
  paidAt?: string;
}

// Complaints
export interface ComplaintCategorySummary {
  categoryId: number;
  categoryName: string;
  totalComplaints: number;
  submittedCount: number;
  inReviewCount: number;
  inProgressCount: number;
  resolvedCount: number;
  rejectedCount: number;
  resolutionRate: number;
}

export interface ComplaintReportItem {
  complaintId: number;
  studentId: number;
  registrationNumber: string;
  studentName: string;
  categoryName: string;
  subject: string;
  priority: string;
  status: string;
  createdAt: string;
  assignedAdminName?: string;
  resolvedAt?: string;
}

// Certificate Requests
export interface CertificateTypeSummary {
  certificateTypeId: number;
  certificateTypeName: string;
  totalRequests: number;
  pendingCount: number;
  approvedCount: number;
  readyForPickupCount: number;
  issuedCount: number;
  rejectedCount: number;
}

export interface CertificateReportItem {
  requestId: number;
  studentId: number;
  registrationNumber: string;
  studentName: string;
  certificateTypeName: string;
  purpose: string;
  status: string;
  requestedAt: string;
  processedAt?: string;
}

// Events
export interface EventSummary {
  eventId: number;
  title: string;
  venueName: string;
  eventDate: string;
  maxCapacity: number;
  registeredAttendees: number;
  attendedCount: number;
  capacityUtilizationRate: number;
  status: string;
}

export interface EventAttendeeReportItem {
  registrationId: number;
  eventId: number;
  eventTitle: string;
  studentId: number;
  registrationNumber: string;
  studentName: string;
  registeredAt: string;
  attended: boolean;
  attendedAt?: string;
}

// Notifications
export interface NotificationTypeSummary {
  type?: string;
  notificationType?: string;
  totalSent: number;
  readCount: number;
  unreadCount: number;
  readRate: number;
}

export interface NotificationReportItem {
  notificationId: number;
  userId?: number;
  studentId?: number;
  studentName?: string;
  indexNumber?: string;
  facultyName?: string;
  userEmail?: string;
  userFullName?: string;
  title?: string;
  message?: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string;
}

// Filter Lookup Options
export interface ReportFilterLookups {
  faculties: { id: number; name: string }[];
  hostels: { id: number; name: string }[];
  labs: { id: number; name: string }[];
  feeTypes: { id: number; name: string }[];
  complaintCategories: { id: number; name: string }[];
  certificateTypes: { id: number; name: string }[];
  events: { id: number; name: string }[];
}
