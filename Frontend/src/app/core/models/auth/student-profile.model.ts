import { StudentPhoneNumber } from '../student/student-phone-number.model';
import { StudentAddress } from '../student/student-address.model';

export interface StudentProfile {
  id: number;
  indexNumber: string;
  name: string;
  email: string;
  facultyId?: number;
  facultyName?: string;
  contactDetails?: string;
  emailVerified: boolean;
  phoneVerified?: boolean;
  isActive: boolean;
  phoneNumbers?: StudentPhoneNumber[];
  addresses?: StudentAddress[];
}

export interface UpdateStudentProfileRequest {
  fullName: string;
  contactDetails?: string;
  facultyId: number;
  phoneNumbers?: StudentPhoneNumber[];
  addresses?: StudentAddress[];
  mobileOtpCode?: string;
}

