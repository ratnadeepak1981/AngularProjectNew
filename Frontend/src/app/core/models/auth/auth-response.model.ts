import { StudentProfile } from './student-profile.model';

export interface AuthResponse {
  token: string;
  refreshToken?: string;
  refreshTokenExpiresAt?: string;
  role: 'Admin' | 'Student' | string;
  profile?: StudentProfile;
  mustChangePassword?: boolean;
  passwordExpired?: boolean;
  forceChangeReason?: string;
}
