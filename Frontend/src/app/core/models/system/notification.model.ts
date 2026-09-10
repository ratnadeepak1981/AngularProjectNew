export interface Notification {
  id: number;
  studentId: number;
  indexNumber: string;
  message: string;
  type?: string;
  isRead: boolean;
  createdAt: string;
}
